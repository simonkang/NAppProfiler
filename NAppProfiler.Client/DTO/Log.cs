using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ProtoBuf;
using ProtoBuf.Serializers;

namespace NAppProfiler.Client.DTO
{
    [ProtoContract]
    public class Log
    {
        /// <summary>
        /// Service Name
        /// </summary>
        [ProtoMember(1)]
        public string Service { get; set; } 
        /// <summary>
        /// Mehod Name
        /// </summary>
        [ProtoMember(2)]
        public string Method { get; set; }
        /// <summary>
        /// Client IP Address
        /// </summary>
        [ProtoMember(3)]
        public byte[] ClientIP { get; set; }
        /// <summary>
        /// Server IP Address
        /// </summary>
        [ProtoMember(4)]
        public byte[] ServerIP { get; set; }
        /// <summary>
        /// Exception Occurred
        /// </summary>
        [ProtoMember(5)]
        public bool IsError { get; set; } 
        /// <summary>
        /// Service Start Time
        /// </summary>
        [ProtoMember(6)]
        public DateTime CreatedDateTime { get; set; } // Created DateTime
        /// <summary>
        /// Elapsed Time in Timespan Ticks
        /// </summary>
        [ProtoMember(7)]
        public long Elapsed { get; set; }
        /// <summary>
        /// Log Detail List
        /// </summary>
        [ProtoMember(8)]
        public IList<LogDetail> Details { get; set; }

        public Log()
        {
            this.ClientIP = new byte[4];
            this.ServerIP = new byte[4];
        }

        public static byte[] SerializeLog(Log item, bool compressDescriptions = false)
        {
            var dtl = item.Details;
            if (dtl != null)
            {
                for (int i = 0; i < dtl.Count; i++)
                {
                    dtl[i].ShouldCompressionDescriptions(compressDescriptions);
                }
            }
            byte[] ret = null;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<Log>(ms, item);
                ret = ms.ToArray();
            }
            return ret;
        }

        public static Log DeserializeLog(byte[] data)
        {
            Log ret = null;
            using (var ms = new MemoryStream(data))
            {
                ret = Serializer.Deserialize<Log>(ms);
            }
            return ret;
        }
    }
}
