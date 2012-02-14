using System;
using System.Net.Sockets;

namespace NAppProfiler.Client.Sockets
{
    public class NAppProfilerClientSocket : NAppProfilerClientSocketBase
    {
        private Socket socket;
        private object socketLock;
        private DateTime lastFailTime;

        public NAppProfilerClientSocket(string host, int port, Action<Message> onMessageArrived)
            : base(host, port, onMessageArrived)
        {
            this.socketLock = new object();
            this.lastFailTime = DateTime.MinValue;
        }

        public override void Send(MessageTypes type, byte[] data)
        {
            var local = CurrentSocket();
            if (local != null)
            {
                try
                {
                    var msg = Message.CreateMessageByte(data, type);
                    local.BeginSend(msg, 0, msg.Length, SocketFlags.None, new AsyncCallback(this.EndSend), local);
                }
                catch (SocketException)
                {
                    Close();
                }
            }
        }

        private void EndSend(IAsyncResult ar)
        {
            try
            {
                var s = (Socket)ar.AsyncState;
                s.EndSend(ar);
            }
            catch { }
        }

        public override void Close()
        {
            if (socket != null)
            {
                lock (socketLock)
                {
                    if (socket != null)
                    {
                        if (socket.Connected)
                        {
                            socket.Shutdown(SocketShutdown.Both);
                        }
                        socket.Close();
                        socket = null;
                    }
                }
            }
        }

        private Socket CurrentSocket()
        {
            if (socket == null)
            {
                Connect();
            }
            return socket;
        }

        private void Connect()
        {
            if (socket == null && (DateTime.UtcNow - lastFailTime) > TimeSpan.FromSeconds(10))
            {
                lock (socketLock)
                {
                    if (socket == null && (DateTime.UtcNow - lastFailTime) > TimeSpan.FromSeconds(10))
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            socket.Connect(this.host, this.port);
                            if (!socket.Connected)
                            {
                                Close();
                                lastFailTime = DateTime.UtcNow;
                            }
                            else
                            {
                                var state = new ClientStateObject(this.onMessageArrived);
                                state.ClientSocket = socket;
                                socket.BeginReceive(state.Buffer, 0, ClientStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
                            }
                        }
                        catch (SocketException)
                        {
                            Close();
                            lastFailTime = DateTime.UtcNow;
                        }
                    }
                }
            }
        }

        private void EndReceive_Callback(IAsyncResult ar)
        {
            var state = (ClientStateObject)ar.AsyncState;
            try
            {
                var bytesReceived = state.ClientSocket.EndReceive(ar);
                state.AppendBuffer(bytesReceived);
                state.ClientSocket.BeginReceive(state.Buffer, 0, ClientStateObject.MaxBufferSize, SocketFlags.None, new AsyncCallback(EndReceive_Callback), state);
            }
            catch (ObjectDisposedException) { }
            catch (SocketException)
            {
                Close();
                lastFailTime = DateTime.UtcNow;
            }
        }
    }
}
