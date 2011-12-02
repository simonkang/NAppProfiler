using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Client.DTO;
using Newtonsoft.Json;

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
            item.CompressDescriptions = true;
            var ret = Log.SerializeLog(item);
            Assert.IsNotNull(ret);
            Assert.IsTrue(ret.Length > 0);
        }

        [Test]
        public void VerifyLogAsJSON()
        {
            var item = CreateLog();
            item.CompressDescriptions = true;
            var ret = JsonConvert.SerializeObject(item);
            Assert.IsNotNullOrEmpty(ret);
        }

        Log CreateLog()
        {
            var details = new List<LogDetail>();
            var parm = new LogParm() { Nm = "abc", sType = true, Val = "def" };
            details.Add(new LogDetail()
            {
                CrDT = DateTime.Now,
                Dsc = "Testing",
                Ed = TimeSpan.FromMilliseconds(300).Ticks,
                Ps = new List<LogParm>(new LogParm[] { parm }),
            });
            details.Add(new LogDetail()
            {
                CrDT = DateTime.Now,
                Dsc = "testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two ",
                Ed = TimeSpan.FromMilliseconds(500).Ticks,
            });
            var ret = new Log()
            {
                CIP = new byte[] { 10, 26, 10, 142 },
                CrDT = DateTime.Now,
                Dtl = details,
                ED = TimeSpan.FromMilliseconds(600).Ticks,
                Err = false,
                Mtd = "mae",
                Svc = "vewavea",
            };
            return ret;
        }

        bool CompareLog(Log logA, Log logB)
        {
            return true;
        }
    }
}
