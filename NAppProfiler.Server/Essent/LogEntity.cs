using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Essent
{
    public class LogEntity
    {
        private readonly long id;
        private readonly DateTime createdDateTime;
        private readonly TimeSpan elapsed;
        private readonly bool exception;
        private readonly byte[] data;

        public LogEntity(DateTime createdDateTime, TimeSpan elapsed, bool exception, byte[] data)
        {
            this.createdDateTime = createdDateTime;
            this.elapsed = elapsed;
            this.exception = exception;
            this.data = data;
        }

        public LogEntity(long id, DateTime createdDateTime, TimeSpan elapsed, bool exception, byte[] data)
        {
            this.id = id;
            this.createdDateTime = createdDateTime;
            this.elapsed = elapsed;
            this.exception = exception;
            this.data = data;
        }

        public long ID { get { return id; } }
        public DateTime CreatedDateTime { get { return createdDateTime; } }
        public TimeSpan Elapsed { get { return elapsed; } }
        public bool Exception { get { return exception; } }
        public byte[] Data { get { return data; } }
    }
}
