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

        public Job(ConfigManager config, bool IsDatabaseTask)
        {
            this.config = config;
            if (IsDatabaseTask)
            {
                currentDb = new Database(config);
            }
        }

        public void Start(JobQueue queue, bool alwaysRunning)
        {
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
                            ProcessItems(jobItems, itemsToProcess);
                            itemsToProcess = -1;
                        }
                        loop++;
                    }
                    curSize = queue.Size();
                }
                if (itemsToProcess >= 0)
                {
                    ProcessItems(jobItems, itemsToProcess);
                }
                running = Wait(queue, alwaysRunning);
            }
        }

        void ProcessItems(JobItem[] jobItems, int topBound)
        {
            for (int i = 0; i <= topBound; i++)
            {
                jobItems[i] = null;
            }
        }

        bool Wait(JobQueue queue, bool alwaysRunning)
        {
            var ret = false;
            var exitMethod = false;
            var dtCountdown = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            var sw = new SpinWait();

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
                        sw.Reset();
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
