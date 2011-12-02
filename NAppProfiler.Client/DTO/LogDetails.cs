using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace NAppProfiler.Client.DTO
{
    public class LogDetail
    {
        [JsonIgnore]
        private bool compressDescriptions = false;

        public string Dsc { get; set; }        // Description
        public byte[] DscC { get; set; }       // Description Compressed
        public IList<LogParm> Ps { get; set; } // Parameters (SQL)
        public DateTime CrDT { get; set; }     // Created DateTime
        public long Ed { get; set; }           // Elapsed

        internal void ShouldCompressionDescriptions(bool value)
        {
            compressDescriptions = value;
        }

        public bool ShouldSerializeDsc()
        {
            return !compressDescriptions;
        }

        public bool ShouldSerializeDscC()
        {
            return compressDescriptions;
        }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (compressDescriptions)
            {
                if (this.Dsc.Length > 100)
                {
                    var bData = Encoding.UTF8.GetBytes(this.Dsc);
                    using (var compressMS = new MemoryStream())
                    using (var zipStream = new GZipStream(compressMS, CompressionMode.Compress))
                    {
                        zipStream.Write(bData, 0, bData.Length);
                        zipStream.Close();
                        this.DscC = compressMS.ToArray();
                    }
                }
                else
                {
                    compressDescriptions = false;
                }
            }
        }

        [OnSerialized]
        internal void OnSerializedMethod(StreamingContext context)
        {
            if (compressDescriptions)
            {
                this.DscC = null;
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (this.DscC != null && this.DscC.Length > 0)
            {
                using (var origMS = new MemoryStream(this.DscC))
                using (var zipStream = new GZipStream(origMS, CompressionMode.Decompress))
                using (var newMS = new MemoryStream())
                {
                    zipStream.CopyTo(newMS);
                    zipStream.Close();
                    this.Dsc = Encoding.UTF8.GetString(newMS.ToArray());
                }
                this.DscC = null;
            }
        }
    }
}
