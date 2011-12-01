using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using NUnit.Framework;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Essent;

namespace NAppProfiler.Server.Tests.Essent
{
    [TestFixture]
    class DatabaseTests
    {
        [Test]
        public void GetDatabaseDefaultDirectory()
        {
            var expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB");
            using (var db = new Database(new ConfigManager()))
            {
                var filePath = db.GetDatabaseDirectory();
                Assert.AreEqual(filePath, expected);
            }
        }

        [Test]
        public void InitializeNewDatabase()
        {
            using (var db = new Database(new ConfigManager()))
            {
                var dbDate = new DateTime(2011, 11, 15);
                if (File.Exists(db.DatabaseFullPath))
                {
                    File.Delete(db.DatabaseFullPath);
                }
                db.InitializeDatabase();
                Assert.IsTrue(File.Exists(db.DatabaseFullPath));
            }
        }
    }
}
