using System;
using System.Net.Sockets;
using NAppProfiler.Client.Sockets;

namespace NAppProfiler.Server.Sockets
{
    class ReceiveStateObject
    {
        public const int MaxBufferSize = 128;

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

        public ReceiveStatuses Status { get; private set; }

        public ReceiveStateObject()
        {
            this.buffer = new byte[MaxBufferSize];
            this.Status = ReceiveStatuses.Receiving;
            this.msg = new Message();
        }

        public bool AppendBuffer(int bufferSize)
        {
            var ret = this.msg.AppendData(buffer, bufferSize);
            if (ret)
            {
                this.Status = this.msg.Data == null ? ReceiveStatuses.InvalidData : ReceiveStatuses.Finished;
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
