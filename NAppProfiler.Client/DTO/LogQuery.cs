using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Serializers;

namespace NAppProfiler.Client.DTO
{
    [ProtoContract]
    public class LogQuery
    {
        /// <summary>
        /// Required
        /// </summary>
        [ProtoMember(1)]
        public DateTime DateTime_From { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [ProtoMember(2)]
        public DateTime DateTime_To { get; set; }

        [ProtoMember(3)]
        public string Text { get; set; }

        [ProtoMember(4)]
        public byte[] ClientIP { get; set; }

        [ProtoMember(5)]
        public byte[] ServerIP { get; set; }

        [ProtoMember(6)]
        public LogQueryExceptions ShowExceptions { get; set; }

        [ProtoMember(7)]
        public TimeSpan TotalElapsed_From { get; set; }

        [ProtoMember(8)]
        public TimeSpan TotalElapsed_To { get; set; }

        [ProtoMember(9)]
        public TimeSpan DetailElapsed_From { get; set; }

        [ProtoMember(10)]
        public TimeSpan DetailElapsed_To { get; set; }

        public static byte[] SerializeQuery(LogQuery item)
        {
            byte[] ret;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<LogQuery>(ms, item);
                ret = ms.ToArray();
            }
            return ret;
        }

        public static LogQuery DeserializeQuery(byte[] item)
        {
            LogQuery ret = null;
            using (var ms = new MemoryStream(item))
            {
                ret = Serializer.Deserialize<LogQuery>(ms);
            }
            return ret;
        }
    }
}
