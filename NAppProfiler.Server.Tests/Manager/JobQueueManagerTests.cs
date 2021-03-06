﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Manager;

namespace NAppProfiler.Server.Tests.Manager
{
    [TestFixture]
    class JobQueueManagerTests
    {
        private ManualResetEventSlim mre;
        private JobQueueManager queueMgr;

        [SetUp]
        public void BeforeTest()
        {
            queueMgr = new JobQueueManager(new ConfigManager());
            queueMgr.EmptyQueue += new EventHandler(queueMgr_EmptyQueue);
            queueMgr.Initialize();
            mre = new ManualResetEventSlim();
        }

        [TearDown]
        public void AfterTest()
        {
            queueMgr.Dispose();
            mre.Dispose();
        }

        [Test]
        [Ignore]
        public void RunPerfTest_Sync()
        {
            RunTest(false, CreateEmptyJobItems());
        }

        [Test]
        [Ignore]
        public void RunPerfTest_Parallel()
        {
            RunTest(true, CreateEmptyJobItems());
        }

        [Test]
        [Ignore]
        public void InsertLogsTest_Sync()
        {
            RunTest(false, CreateInsertLogItems());
        }

        [Test]
        [Ignore]
        public void InsertLogsTest_Parallel()
        {
            RunTest(true, CreateInsertLogItems());
        }

        JobItem[] CreateEmptyJobItems()
        {
            var testSize = 10000000; // 10 million
            var items = new JobItem[testSize];
            for (int i = 0; i < testSize; i++)
            {
                items[i] = new JobItem(JobMethods.Empty);
            }
            return items;
        }

        void RunTest(bool parallel, JobItem[] items)
        {
            long testSize = items.Length;
            var dt1 = DateTime.UtcNow;
            if (parallel)
            {
                RunParallel(items);
            }
            else
            {
                RunSync(items);
            }
            var localProcessCount = queueMgr.TotalProcessCount;
            while (localProcessCount != testSize && queueMgr.CurrentQueueSize() > 0)
            {
                mre.Reset();
                mre.Wait(TimeSpan.FromSeconds(5));
                localProcessCount = queueMgr.TotalProcessCount;
            }
            var dt2 = DateTime.UtcNow;
            var ts = dt2 - dt1;
            var unProcessedCount = 0;
            for (int i = 0; i < testSize; i++)
            {
                if (!items[i].Processed)
                {
                    unProcessedCount++;
                }
                items[i] = null;
            }
            Console.WriteLine("Parallel: " + parallel.ToString());
            Console.WriteLine(ts.ToString() + " " + unProcessedCount.ToString());
            var itemsPerSecond = (testSize / ts.TotalMilliseconds) * 1000D;
            Console.WriteLine(itemsPerSecond.ToString("#,##0") + " items per second");
            Assert.That(unProcessedCount, Is.EqualTo(0), "Items not Processed");
            
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.Collect();
            Thread.Sleep(1);
        }

        void RunSync(JobItem[] items)
        {
            var testSize = items.Length;
            for (int i = 0; i < testSize; i++) { queueMgr.AddJob(items[i]); }
        }

        void RunParallel(JobItem[] items)
        {
            var testSize = items.Length;
            System.Threading.Tasks.Parallel.For(0, testSize, i => queueMgr.AddJob(items[i]));
        }

        void queueMgr_EmptyQueue(object sender, EventArgs e)
        {
            if (queueMgr.TotalProcessCount > 0)
            {
                mre.Set();
            }
        }

        JobItem[] CreateInsertLogItems()
        {
            var testSize = 50000;
            var items = new JobItem[testSize];
            var insertStart = new DateTime(2011, 11, 1);
            var interval = (long)((DateTime.Now - insertStart).Ticks / testSize);
            var rndElapsed = new Random();
            for (int i = 0; i < items.Length; i++)
            {
                var createdDT = insertStart.AddTicks(interval * i);
                var elapsed = TimeSpan.FromMilliseconds((long)rndElapsed.Next(1, 30000)).Ticks;
                var log = new NAppProfiler.Client.DTO.Log()
                {
                    ClientIP = new byte[] { 10, 26, 10, 142 },
                    CreatedDateTime = createdDT,
                    Details = new List<NAppProfiler.Client.DTO.LogDetail>(),
                    Elapsed = elapsed,
                    IsError = Convert.ToBoolean(rndElapsed.Next(0, 1)),
                    Method = "Method",
                    Service = "Service",
                };
                log.Details.Add(new Client.DTO.LogDetail()
                {
                    CreatedDateTime = createdDT,
                    Description = "Description " + i.ToString(),
                    Elapsed = 100,
                });
                log.Details.Add(new Client.DTO.LogDetail()
                {
                    CreatedDateTime = createdDT,
                    Description = "Description2 " + i.ToString(),
                    Elapsed = 100,
                });
                items[i] = new JobItem(JobMethods.Database_InsertLogs)
                {
                    LogEntityItems = new List<Server.Essent.LogEntity>(),
                };
                items[i].LogEntityItems.Add(new Server.Essent.LogEntity(createdDT, new TimeSpan(elapsed), log.IsError, Client.DTO.Log.SerializeLog(log)));
            }
            return items;
        }
    }
}
