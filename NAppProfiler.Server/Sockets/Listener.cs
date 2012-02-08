using System;
using System.Net;
using System.Net.Sockets;
using NAppProfiler.Client.DTO;
using NLog;

namespace NAppProfiler.Server.Sockets
{
    public class Listener : IDisposable
    {
        private static Logger nLogger;

        private Socket listener;
        private object listenerLock;

        static Listener()
        {
            nLogger = LogManager.GetCurrentClassLogger();
        }

        public Listener()
        {
            listenerLock = new object();
        }

        public void Initialize()
        {
            lock (listenerLock)
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var localEp = new IPEndPoint(IPAddress.Any, 33700);
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
            try
            {
                var state = (ReceiveStateObject)ar.AsyncState;
                var bytesReceived = state.ClientSocket.EndReceive(ar);
                if (bytesReceived > 0)
                {
                    if (state.AppendBuffer(bytesReceived))
                    {
                        if (state.Status == ReceiveStatuses.Finished)
                        {
                            var log = Log.DeserializeLog(state.Data);
                        }
                        state.Clear();
                    }
                    state.ClientSocket.BeginReceive(state.Buffer, 0, ReceiveStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
                }
            }
            // Ignore Socket Exception (Foricbly closed connections)
            catch (SocketException) { }
            catch (Exception ex)
            {
                nLogger.ErrorException("Listener EndReceive", ex);
            }
        }

        public void Dispose()
        {
            lock (listenerLock)
            {
                listener.Close();
                listener = null;
            }
        }
    }
}
