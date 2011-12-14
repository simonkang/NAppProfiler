using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Manager;

namespace NAppProfiler.Server.Tests.Manager
{
    [TestFixture]
    class JobQueueManagerTests
    {
        [Test]
        public void InitializeJobQueueManagerTest()
        {
            using (var q = new JobQueueManager(new ConfigManager()))
            {
                q.Initialize();

                var testSize = 5000000; // 2million
                var jobItems = new JobItem[testSize];
                for (int i = 0; i < testSize; i++)
                {
                    jobItems[i] = new JobItem();
                }
                System.Threading.Thread.Yield();
                for (int i = 0; i < testSize; i++)
                {
                    q.AddDatabaseJob(jobItems[i]);
                }
                System.Threading.Thread.Sleep(10000);
                System.Threading.Thread.Sleep(60000);
            }
        }
    }
}
