using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Client.DTO;

namespace NAppProfiler.Client.Tests.DTO
{
    [TestFixture]
    public class LogTests
    {
        [Test]
        public void SerializeLogTest()
        {
            var item = CreateLog();
            var ret = Log.SerializeLog(item);
            Assert.IsNotNull(ret);
            Assert.IsTrue(ret.Length > 0);
        }

        [Test]
        public void SerializeLogTestWithCompression()
        {
            var item = CreateLog();
            var ret = Log.SerializeLog(item, true);
            Assert.IsNotNull(ret);
            Assert.IsTrue(ret.Length > 0);

            var item2 = CreateLog();
            var ret2 = Log.SerializeLog(item2);
            Assert.That(ret2, Is.Not.Null);
            Assert.That(ret2, Is.Not.EqualTo(ret));
        }

        [Test]
        public void VerifyDeserialize()
        {
            var item = CreateLog();
            var ser = Log.SerializeLog(item, true);
            var deSer = Log.DeserializeLog(ser);
            var item2 = CreateLog();

            Assert.That(deSer, Is.Not.Null);
            Assert.That(deSer.Service, Is.EqualTo(item2.Service));
            Assert.That(deSer.Details.Count, Is.EqualTo(item2.Details.Count));
            Assert.That(deSer.Details[1].Description, Is.EqualTo(item2.Details[1].Description));
        }

        Log CreateLog()
        {
            var details = new List<LogDetail>();
            var parm = new LogParm() { Name = "abc", StringType = true, Value = "def" };
            details.Add(new LogDetail()
            {
                CreatedDateTime = DateTime.Now,
                Description = "Testing",
                Elapsed = TimeSpan.FromMilliseconds(300).Ticks,
                Parameters = new List<LogParm>(new LogParm[] { parm }),
            });
            details.Add(new LogDetail()
            {
                CreatedDateTime = DateTime.Now,
                Description = "testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two ",
                Elapsed = TimeSpan.FromMilliseconds(500).Ticks,
            });
            var ret = new Log()
            {
                ClientIP = new byte[] { 10, 26, 10, 142 },
                CreatedDateTime = DateTime.Now,
                Details = details,
                Elapsed = TimeSpan.FromMilliseconds(600).Ticks,
                IsError = false,
                Method = "mae",
                Service = "vewavea",
            };
            return ret;
        }

        bool CompareLog(Log logA, Log logB)
        {
            return true;
        }
    }
}
