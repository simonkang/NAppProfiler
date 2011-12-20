using System;
using System.Collections.Generic;

namespace NAppProfiler.Server.Manager
{
    public class WaitEventArgs : EventArgs
    {
        private long processCount;
        private Guid queueID;

        public WaitEventArgs(long processCount, Guid queueID)
        {
            this.processCount = processCount;
            this.queueID = queueID;
        }

        public long ProcessCount { get { return processCount; } }
        public Guid QueueID { get { return queueID; } }
    }
}
