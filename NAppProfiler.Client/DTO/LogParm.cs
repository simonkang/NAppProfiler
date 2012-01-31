using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace NAppProfiler.Client.DTO
{
    [ProtoContract]
    public class LogParm
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Value { get; set; }

        [ProtoMember(3)]
        public bool StringType { get; set; }
    }
}
