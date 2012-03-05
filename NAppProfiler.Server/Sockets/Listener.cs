using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;
using NAppProfiler.Client.DTO;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Manager;
using NAppProfiler.Client.Sockets;
using NAppProfiler.Server.Essent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NAppProfiler.Server.Sockets
{
    public class Listener : IDisposable
    {
        private static Logger nLogger;
        private static int receiveCount;
        private static int connectionState;

        private readonly JobQueueManager manager;
        private readonly int port;
        private Socket listener;
        private object listenerLock;

        static Listener()
        {
            nLogger = LogManager.GetCurrentClassLogger();
        }

        public Listener(ConfigManager config, JobQueueManager manager)
        {
            listenerLock = new object();
            var defaultPort = 33700;
            var portStr = config.GetSetting(SettingKeys.Socket_PortNo, defaultPort.ToString());
            if (!int.TryParse(portStr, out this.port))
            {
                this.port = defaultPort;
            }
            else if (this.port <= 0)
            {
                this.port = defaultPort;
            }
            this.manager = manager;
        }

        public void Initialize()
        {
            lock (listenerLock)
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var localEp = new IPEndPoint(IPAddress.Any, this.port);
                listener.Bind(localEp);
                listener.Listen(int.MaxValue);
                listener.BeginAccept(new AsyncCallback(EndAccept_Callback), listener);
                Interlocked.Exchange(ref connectionState, 1);
            }
        }

        private void EndAccept_Callback(IAsyncResult ar)
        {
            Socket local = null;
            try
            {
                local = (Socket)ar.AsyncState;
                if (Interlocked.CompareExchange(ref connectionState, 1, 1) == 1)
                {
                    var client = local.EndAccept(ar);
                    var state = new ReceiveStateObject()
                    {
                        ClientSocket = client,
                    };
                    client.BeginReceive(state.Buffer, 0, ReceiveStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
                }
                else
                {
                    local = null;
                }
            }
            finally
            {
                if (local != null)
                {
                    local.BeginAccept(new AsyncCallback(EndAccept_Callback), local);
                }
            }
        }

        private void EndReceive_Callback(IAsyncResult ar)
        {
            var beginRec = true;
            var state = (ReceiveStateObject)ar.AsyncState;
            try
            {
                var bytesReceived = state.ClientSocket.EndReceive(ar);
                if (bytesReceived > 0)
                {
                    var status = state.AppendBuffer(bytesReceived);
                    var doLoop = true;
                    while (doLoop)
                    {
                        doLoop = false;
                        if (state.Status == ReceiveStatuses.Finished)
                        {
                            ProcessItem(state);
                            state.Clear();
                            if (status < bytesReceived)
                            {
                                status = state.AppendBuffer(bytesReceived, status);
                                doLoop = true;
                            }
                        }
                        else if (state.Status == ReceiveStatuses.InvalidData)
                        {
                            state.Clear();
                            state.ClientSocket.Close();
                            beginRec = false;
                        }
                    }
                }
                else
                {
                    beginRec = false;
                }
            }
            // Ignore Socket Exception (Foricbly closed connections)
            catch (SocketException) { }
            catch (Exception ex)
            {
                nLogger.ErrorException("Listener EndReceive", ex);
            }
            finally
            {
                if (beginRec)
                {
                    try
                    {
                        state.ClientSocket.BeginReceive(state.Buffer, 0, ReceiveStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
                    }
                    catch (SocketException) { }
                    catch (Exception ex)
                    {
                        nLogger.ErrorException("Listener EndReceive trying to call BeginReceive", ex);
                    }
                }
            }
        }

        private void ProcessItem(ReceiveStateObject state)
        {
            switch (state.Type)
            {
                case MessageTypes.SendLog:
                    ProcessSendLog(state.Data);
                    break;
                case MessageTypes.Empty:
                    ProcessEmptyItem();
                    break;
                case MessageTypes.Query:
                    ProcessQueryRequest(state.Data, state.ClientSocket);
                    break;
                case MessageTypes.GetLogs:
                    ProcessLogRequest(state.Data, state.ClientSocket);
                    break;
                default:
                    if (nLogger.IsWarnEnabled)
                    {
                        nLogger.Warn("Invalid Message Type Received");
                    }
                    break;
            }
            if (nLogger.IsTraceEnabled)
            {
                Interlocked.Increment(ref receiveCount);
                nLogger.Trace("Message Received - Total Count: {0:#,##0}", receiveCount);
            }
        }

        private void AddJob(JobItem job)
        {
            Task.Factory.StartNew(() => manager.AddJob(job));
        }

        private void ProcessSendLog(byte[] data)
        {
            var item = new JobItem(JobMethods.Database_InsertLogs);
            var log = Log.DeserializeLog(data);
            var entity = new LogEntity(log.CreatedDateTime, TimeSpan.FromTicks(log.Elapsed), log.IsError, data);
            item.LogEntityItems = new List<LogEntity>(1);
            item.LogEntityItems.Add(entity);
            AddJob(item);
        }

        private void ProcessEmptyItem()
        {
            var item = new JobItem(JobMethods.Empty);
            AddJob(item);
        }

        private void ProcessQueryRequest(byte[] data, Socket client)
        {
            var query = LogQuery.DeserializeQuery(data);
            query.ClientSocket = client;
            var item = new JobItem(JobMethods.Index_QueryRequest);
            item.LogQueries = new List<LogQuery>(1);
            item.LogQueries.Add(query);
            AddJob(item);
        }

        private void ProcessLogRequest(byte[] data, Socket client)
        {
            var request = LogQueryResults.DeserializeLog(data);
            request.ClientSocket = client;
            var item = new JobItem(JobMethods.Database_RetrieveLogs);
            item.QueryResults = new List<LogQueryResults>(1);
            item.QueryResults.Add(request);
            AddJob(item);
        }

        public void Dispose()
        {
            lock (listenerLock)
            {
                if (listener != null)
                {
                    Interlocked.Exchange(ref connectionState, 0);
                    listener.Close();
                }
                listener = null;
            }
        }
    }
}
