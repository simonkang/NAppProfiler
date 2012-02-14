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

        [ProtoMember(3)]
        public DateTime CreatedDateTime { get; set; }

        [ProtoMember(4)]
        public long Elapsed { get; set; }

        [ProtoMember(5)]
        public bool IsError { get; set; }

        [ProtoMember(6)]
        public Log Log { get; set; }
    }
}
