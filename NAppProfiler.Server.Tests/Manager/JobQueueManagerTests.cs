using System;
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
        public void RunPerfTest_Sync()
        {
            RunTest(false);            
        }

        [Test]
        public void RunPerfTest_Parallel()
        {
            RunTest(true);
        }

        void RunTest(bool parallel)
        {
            var testSize = 10000000; // 10 million
            var items = new JobItem[testSize];
            for (int i = 0; i < testSize; i++)
            {
                items[i] = new JobItem();
            }
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
            Console.WriteLine(itemsPerSecond.ToString("#,##0") + " items per seoncd");
            Assert.That(unProcessedCount, Is.EqualTo(0), "Items not Processed");
        }

        void RunSync(JobItem[] items)
        {
            var testSize = items.Length;
            for (int i = 0; i < testSize; i++) { queueMgr.AddDatabaseJob(items[i]); }
        }

        void RunParallel(JobItem[] items)
        {
            var testSize = items.Length;
            System.Threading.Tasks.Parallel.For(0, testSize, i => queueMgr.AddDatabaseJob(items[i]));
        }

        void queueMgr_EmptyQueue(object sender, EventArgs e)
        {
            if (queueMgr.TotalProcessCount > 0)
            {
                mre.Set();
            }
        }
    }
}
