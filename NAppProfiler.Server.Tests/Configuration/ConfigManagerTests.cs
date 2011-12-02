using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Server.Configuration;

namespace NAppProfiler.Server.Tests.Configuration
{
    [TestFixture]
    public class ConfigManagerTests
    {
        [Test]
        public void GetSettingTest()
        {
            var config = new ConfigManager();
            var ret = config.GetSetting(SettingKeys.Database_CacheSizeMax);
            Assert.AreEqual(ret, "1024");
        }

        [Test]
        public void GetSettingWithDefaultValueTest()
        {
            var config = new ConfigManager();
            var defValue = "DefaultReturn";
            var ret = config.GetSetting("NoKey", defValue);
            Assert.AreEqual(ret, defValue);
        }
    }
}
