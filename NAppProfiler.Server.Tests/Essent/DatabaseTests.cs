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
    public class DatabaseTests
    {
        public static implicit operator DatabaseTests(int value)
        {
            return new DatabaseTests();
        }

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
                if (Directory.Exists(db.GetDatabaseDirectory()))
                {
                    Directory.Delete(db.GetDatabaseDirectory(), true);
                }
                db.InitializeDatabase();
                Assert.IsTrue(File.Exists(db.DatabaseFullPath));
                db.InsertLog(DateTime.Now, TimeSpan.FromMilliseconds(300).Ticks, new byte[] { 4, 4, 4 });
            }
        }

        [Test]
        public void InsertOneLogRecord()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var id = db.InsertLog(DateTime.Now, TimeSpan.FromMilliseconds(300).Ticks, new byte[] { 3, 30, 255 });
                Assert.IsNotNull(id);
                Assert.AreNotEqual(0, id);
            }
        }

        [Test]
        public void RetrieveRecordByID()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var log = db.RetrieveLogByIDs(1);
                Assert.IsNotNull(log);
                Assert.AreEqual(1, log.Count);
                Assert.AreEqual(1, log[0].ID);
            }
        }
    }
}
