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

        private int curIn;
        private int curOut;

        private long addCounter;
        private long dequeueCounter;

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
            if (curOut >= maxSize)
            {
                curOut = 0;
            }
            var ret = items[curOut];
            items[curOut] = null;
            if (traceEnabled)
            {
                Interlocked.Increment(ref dequeueCounter);
            }
            return ret;
        }

        // Single Threaded in Retrieve
        public int Size()
        {
            var localCurIn = curIn;
            var localCurOut = curOut;
            if (localCurIn < localCurOut)
            {
                return (localCurIn + maxSize - localCurOut);
            }
            else
            {
                return localCurIn - localCurOut;
            }
        }

        public void Add(JobItem item)
        {
            var index = NextInsertIndex();
            if (index == curOut)
            {
                Thread.Yield();
                SpinWait.SpinUntil(() => false, 50);
            }
            items[index] = item;
            if (traceEnabled)
            {
                Interlocked.Increment(ref addCounter);
            }
        }

        int NextInsertIndex()
        {
            var newIn = Interlocked.Increment(ref curIn);
            if (newIn >= maxSize)
            {
                // Try to reset index to 0;
                if (Interlocked.CompareExchange(ref curIn, 0, newIn) == newIn)
                {
                    newIn = 0;
                }
                else
                {
                    // If already reset by another thread, then just increment
                    newIn = Interlocked.Increment(ref curIn);
                }
            }
            return newIn;
        }
    }
}
