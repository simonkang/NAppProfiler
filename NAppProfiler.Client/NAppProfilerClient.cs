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
            Initialize(null, null, 0, null);
        }

        public static void Initialize(NAppProfilerClientSocketBase comm, string host, int port, Action<Message> onMessageArrived)
        {
            if (comm == null)
            {
                NAppProfilerClient.comm = new NAppProfilerClientSocket(host, port, onMessageArrived);
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
            CurrentSocket().Send(MessageTypes.SendLog, data, null);
        }

        public static void SendEmpty()
        {
            CurrentSocket().Send(MessageTypes.Empty, new byte[0], null);
        }

        public static void SendQuery(LogQuery query, object messageBag)
        {
            CurrentSocket().Send(MessageTypes.Query, LogQuery.SerializeQuery(query), messageBag);
        }

        public static void SendLogRequest(LogQueryResults results, object messageBag)
        {
            CurrentSocket().Send(MessageTypes.GetLogs, results.Serialize(), messageBag);
        }

        public static void Close()
        {
            CurrentSocket().Close();
        }
    }
}
