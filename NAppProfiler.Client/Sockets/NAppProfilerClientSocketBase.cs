using System;

namespace NAppProfiler.Client.Sockets
{
    public abstract class NAppProfilerClientSocketBase
    {
        protected string host;
        protected int port;

        public NAppProfilerClientSocketBase(string host, int port)
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
        }

        public abstract void Send(byte[] data);
        public abstract void Close();
    }
}
