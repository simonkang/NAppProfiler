using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Sockets;
using NLog;

namespace NAppProfiler.Server.Manager
{
    public class JobQueueManager : IDisposable
    {
        private static object startJobLock;
        private static object totalProcessCountLock;
        private static Logger nLogger;

        private readonly ConfigManager config;
        private readonly bool traceEnabled;
        private readonly bool fixedNoOfTasks;

        private int maxTasks;
        private volatile int curAddTask;
        private Task[] tasks;
        private int[] taskRunning;
        private JobQueue[] queues;
        private Job databaseJob;
        private Job indexJob;
        private int whenToStartNewTask;
        private int curNumberRunningTasks;
        private Listener listener;

        // Used for Trace and Performance Tests
        private long addDBCounter;
        private long addIndexCounter;
        private Dictionary<Guid, long> processCounts;
        private long totalProcessCount;

        static JobQueueManager()
        {
            startJobLock = new object();
            nLogger = LogManager.GetCurrentClassLogger();
        }

        public JobQueueManager(ConfigManager config)
        {
            this.config = config;
            if (!bool.TryParse(config.GetSetting(SettingKeys.Trace_Logging, bool.FalseString), out traceEnabled))
            {
                traceEnabled = false;
            }
            if (!bool.TryParse(config.GetSetting(SettingKeys.Manager_FixedNoOfTasks, bool.TrueString), out fixedNoOfTasks))
            {
                fixedNoOfTasks = true;
            }
        }

        public event EventHandler EmptyQueue;

        public long TotalProcessCount { get { return totalProcessCount; } }

        public void Initialize()
        {
            GetMaxNumberOfTasks();
            tasks = new Task[maxTasks];
            queues = new JobQueue[maxTasks];
            taskRunning = new int[maxTasks];

            databaseJob = new Job(config, this, true, traceEnabled);
            indexJob = new Job(config, this, false, traceEnabled);
            listener = new Listener(config, this);

            InitializeQueues();
            InitializeTasks();
            listener.Initialize();

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
            if (fixedNoOfTasks)
            {
                for (int i = 1; i < tasks.Length; i++)
                {
                    var localIndex = i;
                    var localQueue = queues[i];
                    var localTask = Task.Factory.StartNew(
                        () => indexJob.Start(localQueue, true), TaskCreationOptions.LongRunning);
                    taskRunning[localIndex] = 1;
                    tasks[localIndex] = localTask;
                    curNumberRunningTasks++;
                }
            }
        }

        int GetMaxNumberOfTasks()
        {
            if (!Int32.TryParse(config.GetSetting(SettingKeys.Manager_MaxTasks), out maxTasks))
            {
                maxTasks = Environment.ProcessorCount - 2;
                if (maxTasks <= 0)
                {
                    maxTasks = 1;
                }
            }
            return maxTasks;
        }

        void InitializeQueues()
        {
            int queueSize;
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

        public void AddJob(JobItem job)
        {
            if (job.Method > 900)
            {
                AddDatabaseJob(job);
            }
            else
            {
                AddIndexJob(job);
            }
        }

        void AddDatabaseJob(JobItem job)
        {
            AddToQueue(job, 0);
            if (traceEnabled || nLogger.IsTraceEnabled)
            {
                Interlocked.Increment(ref addDBCounter);
                if (nLogger.IsTraceEnabled)
                {
                    nLogger.Trace("Database Job Added: {0:#,##0}", addDBCounter);
                }
            }
        }

        void AddIndexJob(JobItem job)
        {
            var addIndex = 0;
            CheckToStartNewJob();
            if (curNumberRunningTasks > 1)
            {
                // Changed to Round Robin Method
                while (addIndex == 0)
                {
                    curAddTask++;
                    var localCurTask = curAddTask;
                    if (localCurTask >= maxTasks)
                    {
                        curAddTask = 1;
                        localCurTask = 1;
                    }
                    if (taskRunning[localCurTask] == 1)
                    {
                        addIndex = localCurTask;
                    }
                }
                // Loop through all Index queues that are running and add to the smallest queue
                //var minSize = int.MaxValue;
                //for (int i = 1; i < taskRunning.Length; i++)
                //{
                //    var localTaskRunning = taskRunning[i];
                //    if (localTaskRunning == 1 && queues[i].Size() < minSize)
                //    {
                //        addIndex = i;
                //    }
                //}
            }
            AddToQueue(job, addIndex);
            if (traceEnabled || nLogger.IsTraceEnabled)
            {
                Interlocked.Increment(ref addIndexCounter);
                if (nLogger.IsTraceEnabled)
                {
                    nLogger.Trace("Non Database job Added: {0:#,##0}", addIndexCounter);
                }
            }
        }

        void AddToQueue(JobItem job, int index)
        {
            queues[index].Add(job);
        }

        void CheckToStartNewJob()
        {
            if (curNumberRunningTasks < maxTasks)
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
            if (tasks != null)
            {
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (tasks[i] != null)
                    {
                        try
                        {
                            tasks[i].Wait(5000);
                        }
                        catch (AggregateException ex)
                        {
                            nLogger.FatalException("Dispose", ex);
                        }
                        tasks[i].Dispose();
                    }
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
            if (listener != null)
            {
                listener.Dispose();
            }
        }
    }
}
