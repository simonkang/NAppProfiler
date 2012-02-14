using System;

namespace NAppProfiler.Client.Sockets
{
    public enum MessageTypes
    {
        Invalid = 0,
        SendLog,
        Empty,
        Query,
        Results,
        GetLogs,
    }
}
