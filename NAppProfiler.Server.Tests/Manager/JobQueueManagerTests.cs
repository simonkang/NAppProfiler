using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
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

                var testSize = 10000000; // 5 million
                var jobItems = new JobItem[testSize];
                for (int i = 0; i < testSize; i++)
                {
                    jobItems[i] = new JobItem();
                }
                for (int i = 0; i < testSize; i++)
                {
                    q.AddDatabaseJob(jobItems[i]);
                }
                System.Threading.Thread.Sleep(10000);
                System.Threading.Thread.Sleep(60000);
            }
        }

        [Test]
        public void TT()
        {
            var testSize = 100000000;
            var jobitems = new JobItem[testSize];

            var dt1 = DateTime.UtcNow;
            for (int i = 0; i < testSize; i++)
            {
                jobitems[i] = null;
            }
            var dt2 = DateTime.UtcNow;
            var ts1 = dt2 - dt1;
            Console.WriteLine(ts1.ToString());

            var testSizeL = (long)testSize;
            dt1 = DateTime.UtcNow;
            for (long i = 0; i < testSizeL; i++)
            {
                jobitems[i] = null;
            }
            dt2 = DateTime.UtcNow;
            var ts2 = dt2 - dt1;
            Console.WriteLine(ts2.ToString());
        }
    }
}
