using System;

namespace NAppProfiler.Client.Sockets
{
    public abstract class NAppProfilerClientSocketBase
    {
        protected string host;
        protected int port;
        protected Action<Message> onMessageArrived;

        public NAppProfilerClientSocketBase(string host, int port, Action<Message> onMessageArrived)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                this.host = "127.0.0.1";
            }
            else
            {
                this.host = host;
            }
            if (this.port <= 0)
            {
                this.port = 33700;
            }
            else
            {
                this.port = port;
            }
            if (onMessageArrived == null)
            {
                this.onMessageArrived = new Action<Message>(OnMessageArrivedNull);
            }
        }

        private void OnMessageArrivedNull(Message msg)
        {
            //Empty Method to Disregard Messages if onMessageArrived is Not set.
        }

        public abstract void Send(MessageTypes type, byte[] data, dynamic messageBag);
        public abstract void Close();
    }
}
