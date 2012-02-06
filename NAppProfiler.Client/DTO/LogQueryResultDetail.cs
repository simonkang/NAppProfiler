using System;
using ProtoBuf;

namespace NAppProfiler.Client.DTO
{
    [ProtoContract]
    public class LogQueryResultDetail
    {
        [ProtoMember(1)]
        public string Database { get; set; }

        [ProtoMember(2)]
        public long ID { get; set; }
    }
}
