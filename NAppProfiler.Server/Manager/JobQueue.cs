using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NAppProfiler.Server.Manager
{
    class JobQueue
    {
        private readonly long maxSize;
        private readonly bool traceEnabled;

        private volatile JobItem[] items;
        private volatile int[] itemStates; // 0-Empty, 1-About to Add, 2-Added, 3-About to Remove
        private long curIn;
        private long pendingCurIn;
        private long curOut;
        private Guid id;

        private long addCounter;
        private long dequeueCounter;

        public JobQueue(int size, bool traceEnabled)
        {
            this.items = new JobItem[size];
            this.itemStates = new int[size];
            this.maxSize = (long)size;
            curOut = -1;
            curIn = -1;
            pendingCurIn = -1;
            this.traceEnabled = traceEnabled;
            this.id = Guid.NewGuid();
        }

        public long AddCounter { get { return addCounter; } }
        public long DequeueCounter { get { return dequeueCounter; } }
        public Guid ID { get { return id; } }

        // Single Threaded in Retrieve
        public JobItem Dequeue()
        {
            curOut++;
            var longIndex = curOut % maxSize;
            var index = Convert.ToInt32(longIndex);
            if (Interlocked.CompareExchange(ref itemStates[index], 3, 2) != 2)
            {
                var sw = new SpinWait();
                while (Interlocked.CompareExchange(ref itemStates[index], 3, 2) != 2 && sw.Count < 1000)
                {
                    sw.SpinOnce();
                }
            }
            JobItem ret = items[index];
            if (ret == null)
            {
                var sw = new SpinWait();
                do
                {
                    sw.SpinOnce();
                    ret = items[index];
                } while (ret == null && sw.Count <= 100);
                if (ret == null)
                {
                    //throw new Exception("null dequeue");
                }
            }
            items[index] = null;
            var beforeEx = Interlocked.CompareExchange(ref itemStates[index], 0, 3);
            if (beforeEx != 3)
            {
                throw new Exception("unknown value");
            }
            if (traceEnabled)
            {
                dequeueCounter++;
            }
            return ret;
        }

        public long Size()
        {
            var ret = curIn - curOut;
            return ret;
        }

        // Multithread in Add
        public void Add(JobItem item)
        {
            var localPending = Interlocked.Increment(ref pendingCurIn);
            var wrapping = curIn + maxSize - 2;
            // Number of pending Add's > then maxSize of array
            if (localPending >= wrapping)
            {
                var sw = new SpinWait();
                do
                {
                    sw.SpinOnce();
                    wrapping = curIn + maxSize - 2;
                } while (localPending >= wrapping);
            }
            var localCurIn = Interlocked.Increment(ref curIn);
            var longIndex = localCurIn % maxSize;
            var index = Convert.ToInt32(longIndex);
            if (Interlocked.CompareExchange(ref itemStates[index], 1, 0) != 0)
            {
                var sw = new SpinWait();
                //var wrapPoint = curOut + maxSize;
                while (Interlocked.CompareExchange(ref itemStates[index], 1, 0) != 0)
                {
                    sw.SpinOnce();
                }
            }
            items[index] = item;
            var beforeEx = Interlocked.CompareExchange(ref itemStates[index], 2, 1);
            if (beforeEx != 1)
            {
                throw new Exception();
            }
            if (traceEnabled)
            {
                Interlocked.Increment(ref addCounter);
            }
        }
    }
}
