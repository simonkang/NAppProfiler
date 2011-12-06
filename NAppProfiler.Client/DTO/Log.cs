using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace NAppProfiler.Client.DTO
{
    public class Log
    {
        [JsonIgnore]
        public bool CompressDescriptions { get; set; }

        /// <summary>
        /// Service Name
        /// </summary>
        public string Svc { get; set; } 
        /// <summary>
        /// Mehod Name
        /// </summary>
        public string Mtd { get; set; }
        /// <summary>
        /// Client IP Address
        /// </summary>
        public byte[] CIP { get; set; }
        /// <summary>
        /// Server IP Address
        /// </summary>
        public byte[] SIP { get; set; }
        /// <summary>
        /// Exception Occurred
        /// </summary>
        public bool Err { get; set; } 
        /// <summary>
        /// Service Start Time
        /// </summary>
        public DateTime CrDT { get; set; } // Created DateTime
        /// <summary>
        /// Elapsed Time in Timespan Ticks
        /// </summary>
        public long ED { get; set; }
        /// <summary>
        /// Log Detail List
        /// </summary>
        public IList<LogDetail> Dtl { get; set; }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (this.Dtl != null)
            {
                foreach (var d in Dtl)
                {
                    d.ShouldCompressionDescriptions(this.CompressDescriptions);
                }
            }
        }

        public static byte[] SerializeLog(Log item)
        {
            byte[] ret = null;
            using (var ms = new MemoryStream())
            using (var writer = new BsonWriter(ms))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, item);
                ret = ms.ToArray();
            }
            return ret;
        }

        public static Log DeserializeLog(byte[] data)
        {
            Log ret = null;
            using (var ms = new MemoryStream(data))
            using (var reader = new BsonReader(ms))
            {
                var serializer = new JsonSerializer();
                ret = serializer.Deserialize<Log>(reader);
            }
            return ret;
        }
    }
}
