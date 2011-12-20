using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Essent;

namespace NAppProfiler.Server.Manager
{
    class Job : IDisposable
    {
        private readonly ConfigManager config;
        private readonly Database currentDb;

        private int stopping;
        private bool traceEnabled;

        public Job(ConfigManager config, bool IsDatabaseTask, bool traceEnabled)
        {
            this.config = config;
            if (IsDatabaseTask)
            {
                currentDb = new Database(config);
            }
            this.traceEnabled = traceEnabled;
        }

        public event EventHandler<WaitEventArgs> Waiting;

        public void Start(JobQueue queue, bool alwaysRunning)
        {
            var processCount = 0L;
            var running = true;
            var topBound = 35;
            var jobItems = new JobItem[topBound + 1];
            var itemsToProcess = -1;
            while (running)
            {
                itemsToProcess = -1;
                var curSize = queue.Size();
                while (curSize > 0)
                {
                    var loop = 0;
                    while (loop < curSize)
                    {
                        itemsToProcess++;
                        jobItems[itemsToProcess] = queue.Dequeue();
                        if (itemsToProcess >= topBound)
                        {
                            processCount += ProcessItems(jobItems, itemsToProcess);
                            itemsToProcess = -1;
                        }
                        loop++;
                    }
                    if (itemsToProcess >= 0)
                    {
                        processCount += ProcessItems(jobItems, itemsToProcess);
                        itemsToProcess = -1;
                    }
                    curSize = queue.Size();
                }
                running = Wait(queue, alwaysRunning, processCount);
            }
        }

        long ProcessItems(JobItem[] jobItems, int topBound)
        {
            var processCount = 0L;
            for (int i = 0; i <= topBound; i++)
            {
                jobItems[i].Processed = true;
                jobItems[i] = null;
                processCount++;
            }
            return processCount;
        }

        bool Wait(JobQueue queue, bool alwaysRunning, long processCount)
        {
            var ret = false;
            var exitMethod = false;
            var dtCountdown = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            var sw = new SpinWait();
            if (traceEnabled && Waiting != null)
            {
                Waiting(this, new WaitEventArgs(processCount, queue.ID));
            }

            while (!exitMethod)
            {
                var localStopping = stopping;
                // Exit if Timeout and not always running
                // Or Signaled from Manager to stop job
                if ((!alwaysRunning && dtCountdown > DateTime.UtcNow) || (localStopping == 1))
                {
                    exitMethod = true;
                }
                else
                {
                    // Something in queue to process
                    if (queue.Size() > 0)
                    {
                        ret = true;
                        exitMethod = true;
                    }
                    else
                    {
                        sw.SpinOnce();
                    }
                }
            }
            return ret;
        }

        public void Stop()
        {
            Interlocked.Exchange(ref stopping, 1);
        }

        public void Dispose()
        {
            Stop();
            if (currentDb != null)
            {
                currentDb.Dispose();
            }
        }
    }
}
