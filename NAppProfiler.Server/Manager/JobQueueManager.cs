using System;
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
        private static object startJobLock = new object();

        private readonly ConfigManager config;
        private readonly bool traceEnabled;
        
        private Thread[] tasks;
        private int[] taskRunning;
        private JobQueue[] queues;
        private Job databaseJob;
        private Job indexJob;
        private int whenToStartNewTask;
        private int curNumberRunningTasks;
        private int queueSize;

        private long addDBCounter;
        private long addIndexCounter;

        public JobQueueManager(ConfigManager config)
        {
            this.config = config;
            if (!bool.TryParse(config.GetSetting(SettingKeys.Trace_Logging, bool.FalseString), out traceEnabled))
            {
                traceEnabled = false;
            }
        }

        public void Initialize()
        {
            int maxTasks = GetMaxNumberOfTasks();
            tasks = new Thread[maxTasks];
            queues = new JobQueue[maxTasks];
            taskRunning = new int[maxTasks];

            databaseJob = new Job(config, true);
            indexJob = new Job(config, false);

            InitializeQueues();
            InitializeTasks();
        }

        void InitializeTasks()
        {
            var dbQueue = queues[0];
            //var dbTask = Task.Factory.StartNew(() => databaseJob.Start(dbQueue, true), TaskCreationOptions.LongRunning);
            var dbStart = new ThreadStart(new Action(() => databaseJob.Start(dbQueue, true)));
            var dbTask = new Thread(dbStart);
            dbTask.Priority = ThreadPriority.Highest;
            dbTask.Start();
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
                queueSize = 256;
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
                        taskRunning[i] = 1;
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
                    tasks[i].Join(5000);
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
