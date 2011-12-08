using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Index;

namespace NAppProfiler.Server.Tests.Index
{
    [TestFixture]
    class NAppIndexReaderTests
    {
        [Test]
        public void Search()
        {
            using (var reader = new NAppIndexReader(new ConfigManager()))
            {
                reader.Search();
            }
        }
    }
}
