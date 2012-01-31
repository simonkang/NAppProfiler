using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ProtoBuf;
using ProtoBuf.Serializers;

namespace NAppProfiler.Client.DTO
{
    [ProtoContract]
    public class LogDetail
    {
        [ProtoMember(1)]
        public string Description { get; set; }

        [ProtoMember(2)]
        public byte[] DescCompressed { get; set; }

        [ProtoMember(3)]
        public IList<LogParm> Parameters { get; set; } // Parameters (SQL)

        [ProtoMember(4)]
        public DateTime CreatedDateTime { get; set; }

        [ProtoMember(5)]
        public long Elapsed { get; set; }

        [ProtoMember(6)]
        public bool IsSql { get; set; }

        internal void ShouldCompressionDescriptions(bool value)
        {
            if (value && this.DescCompressed == null)
            {
                if (this.Description != null && this.Description.Length > 100)
                {
                    var bData = Encoding.UTF8.GetBytes(this.Description);
                    using (var compressMS = new MemoryStream())
                    using (var zipStream = new GZipStream(compressMS, CompressionMode.Compress))
                    {
                        zipStream.Write(bData, 0, bData.Length);
                        zipStream.Close();
                        this.DescCompressed = compressMS.ToArray();
                        this.Description = null;
                    }
                }
                else
                {
                    this.DescCompressed = new byte[0];
                }
            }
        }

        [ProtoAfterDeserialization]
        internal void OnDeserialized()
        {
            if (this.DescCompressed != null && this.DescCompressed.Length > 0)
            {
                using (var origMS = new MemoryStream(this.DescCompressed))
                using (var zipStream = new GZipStream(origMS, CompressionMode.Decompress))
                using (var newMS = new MemoryStream())
                {
                    zipStream.CopyTo(newMS);
                    zipStream.Close();
                    this.Description = Encoding.UTF8.GetString(newMS.ToArray());
                }
                this.DescCompressed = null;
            }
        }
    }
}
