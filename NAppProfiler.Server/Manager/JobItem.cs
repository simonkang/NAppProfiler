using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAppProfiler.Server.Essent;

namespace NAppProfiler.Server.Manager
{
    public class JobMethods
    {
        // All Database Methods must be greater than 900
        public const int Empty = 0;
        public const int Index = 101;
        public const int Database_InsertLogs = 901;
    }

    public class JobItem
    {
        private readonly int method;

        private bool processed;
        private LogEntity logEntityItem;

        public JobItem(int method)
        {
            this.method = method;
        }

        public bool Processed
        {
            get { return processed; }
            set { processed = value; }
        }

        public int Method
        {
            get { return method; }
        }

        public LogEntity LogEntityItem
        {
            get { return logEntityItem; }
            set { logEntityItem = value; }
        }
    }
}
