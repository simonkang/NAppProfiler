using System;
using NAppProfiler.Client.Sockets;
using NAppProfiler.Client.DTO;

namespace NAppProfiler.Client
{
    public class NAppProfilerClient
    {
        private static NAppProfilerClientSocketBase comm;

        public static void Initialize()
        {
            Initialize(null, null, 0);
        }

        public static void Initialize(NAppProfilerClientSocketBase comm, string host, int port)
        {
            if (comm == null)
            {
                NAppProfilerClient.comm = new NAppProfilerClientSocket(host, port);
            }
            else
            {
                NAppProfilerClient.comm = comm;
            }
}

        private static NAppProfilerClientSocketBase CurrentSocket()
        {
            if (comm == null)
            {
                NAppProfilerClient.Initialize();
            }
            return comm;
        }

        public static void SendLog(Log log)
        {
            var data = Log.SerializeLog(log);
            CurrentSocket().Send(data);
        }

        public static void Close()
        {
            CurrentSocket().Close();
        }
    }
}
