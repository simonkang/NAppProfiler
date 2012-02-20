using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Client.DTO;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Index;

namespace NAppProfiler.Server.Tests.Index
{
    [TestFixture]
    class NAppIndexReaderTests
    {
        [Test]
        [Ignore]
        public void Search()
        {
            using (var reader = new NAppIndexReader(new ConfigManager()))
            {
                reader.Search();
            }
        }

        [Test]
        public void ExceptionWhenFromOrToNotSet()
        {
            using (var reader = new NAppIndexReader(new ConfigManager()))
            {
                var qry = new LogQuery();
                Assert.Throws<ArgumentException>(() => reader.Search(qry));
            }
        }

        [Test]
        public void SearchInTimeRange()
        {
            using (var reader = new NAppIndexReader(new ConfigManager()))
            {
                var qry = new LogQuery()
                {
                    DateTime_From = new DateTime(2011, 11, 03),
                    DateTime_To = new DateTime(2011, 11, 06),
                };
                var ret = reader.Search(qry);
                Assert.That(ret, Is.Not.Null);
                Assert.That(ret.DateTime_From, Is.EqualTo(qry.DateTime_From));
                Assert.That(ret.DateTime_To, Is.EqualTo(qry.DateTime_To));
            }
        }

        [Test]
        public void SearchForElapsedGreaterThan500()
        {
            using (var reader = new NAppIndexReader(new ConfigManager()))
            {
                var qry = new LogQuery()
                {
                    DateTime_From = new DateTime(2011, 11, 03),
                    DateTime_To = new DateTime(2011, 11, 06),
                    TotalElapsed_From = TimeSpan.FromTicks(500),
                };
                var ret = reader.Search(qry);
                Assert.That(ret, Is.Not.Null);
            }
        }
    }
}
