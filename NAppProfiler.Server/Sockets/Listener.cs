using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;
using NAppProfiler.Client.DTO;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Manager;

namespace NAppProfiler.Server.Sockets
{
    public class Listener : IDisposable
    {
        private static Logger nLogger;
        private static int receiveCount;

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
            }
        }

        private static void EndAccept_Callback(IAsyncResult ar)
        {
            Socket local = null;
            try
            {
                local = (Socket)ar.AsyncState;
                var client = local.EndAccept(ar);
                var state = new ReceiveStateObject()
                {
                    ClientSocket = client,
                };
                client.BeginReceive(state.Buffer, 0, ReceiveStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
            }
            finally
            {
                if (local != null)
                {
                    local.BeginAccept(new AsyncCallback(EndAccept_Callback), local);
                }
            }
        }

        private static void EndReceive_Callback(IAsyncResult ar)
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
                            var log = Log.DeserializeLog(state.Data);
                            if (nLogger.IsTraceEnabled)
                            {
                                Interlocked.Increment(ref receiveCount);
                                nLogger.Trace("Message Received - Total Count: {0}", receiveCount.ToString("#,##0"));
                            }
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
                    state.ClientSocket.BeginReceive(state.Buffer, 0, ReceiveStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
                }
            }
        }

        public void Dispose()
        {
            lock (listenerLock)
            {
                if (listener != null)
                {
                    listener.Close();
                }
                listener = null;
            }
        }
    }
}
