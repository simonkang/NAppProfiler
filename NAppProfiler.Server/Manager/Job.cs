using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Essent;
using NAppProfiler.Server.Index;
using NLog;

namespace NAppProfiler.Server.Manager
{
    class Job : IDisposable
    {
        private static Logger log;

        private readonly ConfigManager config;
        private readonly Database currentDb;
        private readonly JobQueueManager manager;
        private readonly NAppIndexReader indexReader;
        private readonly NAppIndexUpdater indexUpdater;

        private int stopping;
        private bool traceEnabled;

        static Job()
        {
            log = LogManager.GetCurrentClassLogger();
        }

        public Job(ConfigManager config, JobQueueManager manager, bool IsDatabaseTask, bool traceEnabled)
        {
            this.config = config;
            this.manager = manager;
            if (IsDatabaseTask)
            {
                currentDb = new Database(config);
                currentDb.InitializeDatabase();
                indexUpdater = new NAppIndexUpdater(config, currentDb);
                indexUpdater.Initialize();
            }
            indexReader = new NAppIndexReader(config);

            this.traceEnabled = traceEnabled;
        }

        public event EventHandler<WaitEventArgs> Waiting;

        public void Start(JobQueue queue, bool alwaysRunning)
        {
            var processor = new JobProcessor(config, currentDb, manager, indexUpdater, indexReader);
            var running = true;
            while (running)
            {
                var curSize = queue.Size();
                while (curSize > 0)
                {
                    var loop = 0;
                    while (loop < curSize)
                    {
                        try
                        {
                            processor.Add(queue.Dequeue());
                        }
                        catch (Exception ex)
                        {
                            log.ErrorException("Dequeue Exception", ex);
                        }
                        loop++;
                    }
                    curSize = queue.Size();
                }
                processor.Flush();
                running = Wait(queue, alwaysRunning, processor.ProcessCount);
            }
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
            if (indexUpdater != null)
            {
                indexUpdater.Dispose();
            }
            if (indexReader != null)
            {
                indexReader.Dispose();
            }
        }
    }
}
