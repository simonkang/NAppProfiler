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
        private volatile int[] itemStates;
        private long curIn;
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
            var index = curOut % maxSize;
            JobItem ret = null;
            while (itemStates[index] != 1)
            {
                var sw = new SpinWait();
                sw.SpinOnce();
            }
            itemStates[index] = 2;
            ret = items[index];
            if (ret == null)
            {
                var sw = new SpinWait();
                while (ret == null)
                {
                    if (sw.Count > 100)
                    {
                        break;
                    }
                    sw.SpinOnce();
                    ret = items[index];
                }
                if (ret == null)
                {
                    throw new Exception("null dequeue");
                }
            }
            items[index] = null;
            itemStates[index] = 0;
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
            var localCurIn = Interlocked.Increment(ref curIn);
            var index = localCurIn % maxSize;
            if (itemStates[index] != 0)
            {
                var sw = new SpinWait();
                while (itemStates[index] != 0)
                {
                    sw.SpinOnce();
                }
            }
            itemStates[index] = 1;
            items[index] = item;
            if (traceEnabled)
            {
                Interlocked.Increment(ref addCounter);
            }
        }
    }
}
