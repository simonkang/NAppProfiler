using System;
using System.Net.Sockets;

namespace NAppProfiler.Client.Sockets
{
    public class ClientStateObject
    {
        public const int MaxBufferSize = 8192;
        
        private byte[] buffer;
        private Message msg;
        private Action<Message> onMessageReceived;

        public Socket ClientSocket { get; set; }

        public byte[] Buffer
        {
            get { return buffer; }
        }

        public byte[] Data
        {
            get { return msg.Data; }
        }

        public MessageTypes Type
        {
            get { return msg.Type; }
        }

        public ClientStateObject(Action<Message> onMessageReceived)
        {
            this.buffer = new byte[MaxBufferSize];
            this.msg = new Message();
            this.onMessageReceived = onMessageReceived;
        }

        public void AppendBuffer(int bufferSize)
        {
            var ret = this.msg.AppendData(buffer, bufferSize, 0);
            while (ret >= 0)
            {
                this.onMessageReceived(msg);
                this.Clear();
                if (ret < bufferSize)
                {
                    ret = this.msg.AppendData(buffer, bufferSize, ret);
                }
                else
                {
                    ret = -1;
                }
            }
        }

        public void Clear()
        {
            this.msg = new Message();
        }
    }
}
