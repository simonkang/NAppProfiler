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
            using (var idxUpdate = new NAppIndexUpdater(new ConfigManager()))
            {
                idxUpdate.Initialize();
                DateTime start;
                DateTime stop;
                TimeSpan ts;
                start = DateTime.UtcNow;
                var count = idxUpdate.UpdateIndex();
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Index Added " + count.ToString("#,##0") + " in " + ts.TotalMilliseconds.ToString("#,##0") + " ms");
            }
        }

        [Test]
        public void ReindexAllTest()
        {
            var config = new ConfigManager();
            using (var idxUpdate = new NAppIndexUpdater(config))
            {
                idxUpdate.Initialize();
                DateTime start;
                DateTime stop;
                TimeSpan ts;
                start = DateTime.UtcNow;
                var count = idxUpdate.RebuildIndex();
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Index Rebuild " + count.ToString("#,##0") + " in " + ts.TotalMilliseconds.ToString("#,##0") + " ms");
            }
        }
    }
}
