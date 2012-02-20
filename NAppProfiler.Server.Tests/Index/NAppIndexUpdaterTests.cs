using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Server.Index;
using NAppProfiler.Server.Configuration;

namespace NAppProfiler.Server.Tests.Index
{
    [TestFixture]
    class NAppIndexUpdaterTests
    {
        [Test]
        public void IndexUpdaterTest()
        {
            var config = new ConfigManager();
            using (var currentDB = new NAppProfiler.Server.Essent.Database(config))
            {
                currentDB.InitializeDatabase();
                using (var idxUpdate = new NAppIndexUpdater(config, currentDB))
                {
                    idxUpdate.Initialize();
                    DateTime start;
                    DateTime stop;
                    TimeSpan ts;
                    start = DateTime.UtcNow;

                    var totalCount = 0L;
                    var curCount = 1L;
                    while (curCount > 0)
                    {
                        curCount = idxUpdate.UpdateIndex();
                        totalCount += curCount;
                    }
                    stop = DateTime.UtcNow;
                    ts = stop - start;
                    Console.WriteLine("Index Added " + totalCount.ToString("#,##0") + " in " + ts.TotalMilliseconds.ToString("#,##0") + " ms");
                }
            }
        }

        [Test]
        public void ReindexAllTest()
        {
            var config = new ConfigManager();
            using (var currentDB = new NAppProfiler.Server.Essent.Database(config))
            {
                currentDB.InitializeDatabase();
                using (var idxUpdate = new NAppIndexUpdater(config, currentDB))
                {
                    idxUpdate.Initialize();
                    DateTime start;
                    DateTime stop;
                    TimeSpan ts;
                    start = DateTime.UtcNow;
                    var count = idxUpdate.RebuildIndex();

                    //var totalCount = 0L;
                    //var curCount = 1L;
                    //while (curCount > 0)
                    //{
                    //    curCount = idxUpdate.UpdateIndex();
                    //    totalCount += curCount;
                    //}

                    stop = DateTime.UtcNow;
                    ts = stop - start;
                    Console.WriteLine("Index Rebuild " + count.ToString("#,##0") + " in " + ts.TotalMilliseconds.ToString("#,##0") + " ms");
                }
            }
        }
    }
}
