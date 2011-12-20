using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Manager
{
    public class JobItem
    {
        private bool processed;

        public bool Processed
        {
            get { return processed; }
            set { processed = value; }
        }
    }
}
