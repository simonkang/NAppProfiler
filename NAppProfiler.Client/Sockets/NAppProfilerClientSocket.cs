using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace NAppProfiler.Client.Sockets
{
    public class NAppProfilerClientSocket : NAppProfilerClientSocketBase
    {
        private Socket socket;
        private object socketLock;
        private DateTime lastFailTime;
        private static ConcurrentDictionary<Guid, MessageTrackingObject> messageTracker;
        private Action<Message> clientMessageArrviedHandler;

        static NAppProfilerClientSocket()
        {
            messageTracker = new ConcurrentDictionary<Guid, MessageTrackingObject>();
        }

        public NAppProfilerClientSocket(string host, int port, Action<Message> onMessageArrived)
            : base(host, port, onMessageArrived)
        {
            this.socketLock = new object();
            this.lastFailTime = DateTime.MinValue;
            if (onMessageArrived != null)
            {
                this.clientMessageArrviedHandler = onMessageArrived;
                this.onMessageArrived = new Action<Message>(OnMessageArrivedSocketHandler);
            }
        }

        private void OnMessageArrivedSocketHandler(Message msg)
        {
            if (messageTracker.ContainsKey(msg.MessageGuid))
            {
                MessageTrackingObject msgTrack = null;
                if (messageTracker.TryRemove(msg.MessageGuid, out msgTrack))
                {
                    msg.MessageBag = msgTrack.MessageBag;
                }
            }
            clientMessageArrviedHandler(msg);
        }

        public override void Send(MessageTypes type, byte[] data, object messageBag)
        {
            var local = CurrentSocket();
            if (local != null)
            {
                try
                {
                    byte[] msg = null;
                    if (messageBag != null)
                    {
                        var msgTrack = new MessageTrackingObject(messageBag);
                        messageTracker.TryAdd(msgTrack.MessageGuid, msgTrack);
                        msg = Message.CreateMessageByte(data, type, msgTrack.MessageGuid);
                    }
                    else
                    {
                        msg = Message.CreateMessageByte(data, type);
                    }
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
