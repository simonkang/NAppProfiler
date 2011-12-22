using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Manager
{
    public enum JobTypes
    {
        Empty,
        Database,
        Index,
    }

    public class JobItem
    {
        private readonly JobTypes type;

        private bool processed;

        public JobItem(JobTypes type)
        {
            this.type = type;
        }

        public bool Processed
        {
            get { return processed; }
            set { processed = value; }
        }

        public JobTypes Type
        {
            get { return type; }
        }
    }
}
