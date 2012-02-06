using System;
using System.Collections.Generic;
using ProtoBuf;
using ProtoBuf.Serializers;

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
    }
}
