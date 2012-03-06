using System;
using System.Net.Sockets;
using NAppProfiler.Client.Sockets;

namespace NAppProfiler.Server.Sockets
{
    public class ReceiveStateObject
    {
        public const int MaxBufferSize = 8192;

        private byte[] buffer;
        private Message msg;

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

        public Guid MessageGuid
        {
            get { return msg.MessageGuid; }
        }

        public ReceiveStatuses Status { get; private set; }

        public ReceiveStateObject()
        {
            this.buffer = new byte[MaxBufferSize];
            this.Status = ReceiveStatuses.Receiving;
            this.msg = new Message();
        }

        public int AppendBuffer(int bufferSize, int startIndex = 0)
        {
            var ret = this.msg.AppendData(buffer, bufferSize, startIndex);
            if (ret >= 0)
            {
                this.Status = ReceiveStatuses.Finished;
            }
            else if (ret == -2)
            {
                this.Status = ReceiveStatuses.InvalidData;
            }
            return ret;
        }

        public void Clear()
        {
            this.Status = ReceiveStatuses.Receiving;
            this.msg = new Message();
        }
    }
}
