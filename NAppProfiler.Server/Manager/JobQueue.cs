using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NAppProfiler.Server.Manager
{
    class JobQueue
    {
        private readonly JobItem[] items;
        private readonly int maxSize;
        private readonly bool traceEnabled;

        private long curIn;
        private long curOut;

        private long addCounter;
        private long dequeueCounter;
        private long spinAddCounter;

        public long AddCounter { get { return addCounter; } }
        public long DequeueCounter { get { return dequeueCounter; } }

        public JobQueue(int size, bool traceEnabled)
        {
            this.items = new JobItem[size];
            this.maxSize = size;
            curOut = -1;
            curIn = -1;
            this.traceEnabled = traceEnabled;
        }

        // Single Threaded in Retrieve
        public JobItem Dequeue()
        {
            curOut++;
            var localCurOut = curOut;
            var index = localCurOut % maxSize;
            var ret = items[index];
            items[index] = null;
            if (traceEnabled)
            {
                Interlocked.Increment(ref dequeueCounter);
            }
            return ret;
        }

        // Single Threaded in Retrieve
        public long Size()
        {
            var localCurIn = curIn;
            var localCurOut = curOut;
            return localCurIn - localCurOut;
        }

        public void Add(JobItem item)
        {
            var localCurIn = curIn;
            var localCurOut = curOut;
            var localSize = localCurIn - localCurOut + 1;
            if (localSize > maxSize)
            {
                SpinWait.SpinUntil(() => Size() < maxSize - 1);
                if (traceEnabled)
                {
                    Interlocked.Increment(ref spinAddCounter);
                }
            }
            localCurIn = Interlocked.Increment(ref curIn);
            var index = localCurIn % maxSize;
            items[index] = item;
            if (traceEnabled)
            {
                Interlocked.Increment(ref addCounter);
            }
        }
    }
}
