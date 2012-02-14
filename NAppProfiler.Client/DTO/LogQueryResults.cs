using System;
using System.Collections.Generic;
using ProtoBuf;
using ProtoBuf.Serializers;
using System.Net.Sockets;
using System.IO;

namespace NAppProfiler.Client.DTO
{
    [ProtoContract]
    public class LogQueryResults
    {
        [ProtoMember(1)]
        public DateTime DateTime_From { get; set; }

        [ProtoMember(2)]
        public DateTime DateTime_To { get; set; }

        [ProtoMember(3)]
        public IList<LogQueryResultDetail> LogIDs { get; set; }

        [ProtoMember(4)]
        public Guid RequestID { get; set; }

        [ProtoMember(5)]
        public bool IncludeData { get; set; }

        public Socket ClientSocket { get; set; }

        public LogQueryResults()
        {
            this.RequestID = Guid.NewGuid();
        }

        public byte[] Serialize()
        {
            byte[] ret = null;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<LogQueryResults>(ms, this);
                ret = ms.ToArray();
            }
            return ret;
        }

        public static LogQueryResults DeserializeLog(byte[] data)
        {
            LogQueryResults ret = null;
            using (var ms = new MemoryStream(data))
            {
                ret = Serializer.Deserialize<LogQueryResults>(ms);
            }
            return ret;
        }
    }
}
