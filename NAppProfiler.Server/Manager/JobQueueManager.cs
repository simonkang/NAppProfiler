﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAppProfiler.Server.Configuration;

namespace NAppProfiler.Server.Manager
{
    public class JobQueueManager : IDisposable
    {
        private static object startJobLock;
        private static object totalProcessCountLock;

        private readonly ConfigManager config;
        private readonly bool traceEnabled;
        
        private Task[] tasks;
        private int[] taskRunning;
        private JobQueue[] queues;
        private Job databaseJob;
        private Job indexJob;
        private int whenToStartNewTask;
        private int curNumberRunningTasks;
        private int queueSize;

        // Used for Trace and Performance Tests
        private long addDBCounter;
        private long addIndexCounter;
        private Dictionary<Guid, long> processCounts;
        private long totalProcessCount;

        public JobQueueManager(ConfigManager config)
        {
            this.config = config;
            if (!bool.TryParse(config.GetSetting(SettingKeys.Trace_Logging, bool.FalseString), out traceEnabled))
            {
                traceEnabled = false;
            }
            startJobLock = new object();
        }

        public event EventHandler EmptyQueue;

        public long TotalProcessCount { get { return totalProcessCount; } }

        public void Initialize()
        {
            int maxTasks = GetMaxNumberOfTasks();
            tasks = new Task[maxTasks];
            queues = new JobQueue[maxTasks];
            taskRunning = new int[maxTasks];

            databaseJob = new Job(config, true, traceEnabled);
            indexJob = new Job(config, false, traceEnabled);

            InitializeQueues();
            InitializeTasks();

            if (traceEnabled)
            {
                totalProcessCountLock = new object();
                processCounts = new Dictionary<Guid, long>();
                for (int i = 0; i < queues.Length; i++)
                {
                    processCounts.Add(queues[i].ID, 0);
                }
                databaseJob.Waiting += new EventHandler<WaitEventArgs>(Job_Waiting);
                indexJob.Waiting += new EventHandler<WaitEventArgs>(Job_Waiting);
            }
        }

        void Job_Waiting(object sender, WaitEventArgs e)
        {
            var curCount = processCounts[e.QueueID];
            if (curCount != e.ProcessCount)
            {
                lock (totalProcessCountLock)
                {
                    processCounts[e.QueueID] = e.ProcessCount;
                    totalProcessCount = processCounts.Aggregate(0L, (total, pc) => total + (long)pc.Value);
                }
                if (EmptyQueue != null)
                {
                    var curQueueSize = CurrentQueueSize();
                    if (curQueueSize == 0)
                    {
                        EmptyQueue(this, EventArgs.Empty);
                    }
                }
            }
        }

        public long CurrentQueueSize()
        {
            var ret = queues.Aggregate(0L, (total, q) => total + q.Size());
            return ret;
        }

        void InitializeTasks()
        {
            var dbQueue = queues[0];
            var dbTask = Task.Factory.StartNew(() => databaseJob.Start(dbQueue, true), TaskCreationOptions.LongRunning);
            tasks[0] = dbTask;
            taskRunning[0] = 1;
            curNumberRunningTasks = 1;
        }

        int GetMaxNumberOfTasks()
        {
            int maxTasks;
            if (!Int32.TryParse(config.GetSetting(SettingKeys.Manager_MaxTasks), out maxTasks))
            {
                maxTasks = Environment.ProcessorCount - 1;
                if (maxTasks <= 0)
                {
                    maxTasks = 1;
                }
            }
            return maxTasks;
        }

        void InitializeQueues()
        {
            if (!Int32.TryParse(config.GetSetting(SettingKeys.Manager_QueueSize), out queueSize))
            {
                queueSize = 1024;
            }
            whenToStartNewTask = Math.Min((int)(queueSize * 0.5), 50);
            for (int i = 0; i < queues.Length; i++)
            {
                queues[i] = new JobQueue(queueSize, traceEnabled);
            }
        }

        public void AddDatabaseJob(JobItem job)
        {
            AddToQueue(job, 0);
            if (traceEnabled)
            {
                Interlocked.Increment(ref addDBCounter);
            }
        }

        public void AddIndexJob(JobItem job)
        {
            var addIndex = 0;
            CheckToStartNewJob();
            if (curNumberRunningTasks > 1)
            {
                // Loop through all Index queues that are running and add to the smallest queue
                var minSize = int.MaxValue;
                for (int i = 1; i < taskRunning.Length; i++)
                {
                    var localTaskRunning = taskRunning[i];
                    if (localTaskRunning == 1 && queues[i].Size() < minSize)
                    {
                        addIndex = i;
                    }
                }
            }
            AddToQueue(job, addIndex);
            if (traceEnabled)
            {
                Interlocked.Increment(ref addIndexCounter);
            }
        }

        void AddToQueue(JobItem job, int index)
        {
            queues[index].Add(job);
        }

        void CheckToStartNewJob()
        {
            if (curNumberRunningTasks < tasks.Length)
            {
                var shouldStartNewJob = false;
                for (int i = 0; i < taskRunning.Length; i++)
                {
                    var localTaskRunning = taskRunning[i];
                    if (localTaskRunning == 1 && queues[i].Size() > whenToStartNewTask)
                    {
                        shouldStartNewJob = true;
                        break;
                    }
                }
                if (shouldStartNewJob)
                {
                    StartNewJob();
                }
            }
        }

        void StartNewJob()
        {
            lock (startJobLock)
            {
                for (int i = 1; i < taskRunning.Length; i++)
                {
                    if (taskRunning[i] == 0)
                    {
                        var localIndex = i;
                        var localQueue = queues[i];
                        var localTask = Task.Factory.StartNew(
                            () => indexJob.Start(localQueue, false))
                            .ContinueWith(t => OnTaskStopping(t, localIndex));
                        taskRunning[localIndex] = 1;
                        tasks[localIndex] = localTask;
                        curNumberRunningTasks++;
                        break;
                    }
                }
            }
        }

        void OnTaskStopping(Task t, int index)
        {
            lock (startJobLock)
            {
                taskRunning[index] = 0;
                curNumberRunningTasks--;
                t.Dispose();
                tasks[index] = null;
                if (traceEnabled)
                {
                    lock (totalProcessCountLock)
                    {
                        var curId = queues[index].ID;
                        var curCount = processCounts[curId];
                        processCounts[curId] = 0;
                        processCounts.Add(Guid.NewGuid(), curCount);
                    }
                }
            }
            // Queue must be empty
            System.Diagnostics.Debug.Assert(queues[index].Size() == 0);
        }

        public void Dispose()
        {
            if (databaseJob != null)
            {
                databaseJob.Stop();
            }
            if (indexJob != null)
            {
                indexJob.Stop();
            }
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null)
                {
                    tasks[i].Wait(5000);
                    tasks[i].Dispose();
                }
            }
            if (databaseJob != null)
            {
                databaseJob.Dispose();
            }
            if (indexJob != null)
            {
                indexJob.Dispose();
            }
        }
    }
}
