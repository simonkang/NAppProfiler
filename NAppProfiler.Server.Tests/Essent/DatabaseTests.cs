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

        [Test]
        public void TestInsertPerformance()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var size = db.Size();
                var start = DateTime.UtcNow;
                DateTime stop;
                TimeSpan ts;
                var ts1 = TimeSpan.FromMilliseconds(300).Ticks;
                var insertStart = new DateTime(2011, 11, 1);
                var numOfRows = 500000;
                var interval = (long)((DateTime.Now - insertStart).Ticks / numOfRows);
                var rndElapsed = new Random();
                for (int i = 0; i < numOfRows; i++)
                {
                    var createdDT = insertStart.AddTicks(interval * i);
                    var elapsed = (long)rndElapsed.Next(1, 30000);
                    var log = new NAppProfiler.Client.DTO.Log()
                    {
                        CIP = new byte[] { 10, 26, 10, 142 },
                        CrDT = createdDT,
                        Dtl = new List<NAppProfiler.Client.DTO.LogDetail>(),
                        ED = elapsed,
                        Err = Convert.ToBoolean(rndElapsed.Next(0, 1)),
                        Mtd = "Method",
                        Svc = "Service",
                    };
                    log.Dtl.Add(new Client.DTO.LogDetail()
                    {
                        CrDT = createdDT,
                        Dsc = "Description " + i.ToString(),
                        Ed = 100,
                    });
                    log.Dtl.Add(new Client.DTO.LogDetail()
                    {
                        CrDT = createdDT,
                        Dsc = "Description2 " + i.ToString(),
                        Ed = 100,
                    });
                    db.InsertLog(createdDT, elapsed, NAppProfiler.Client.DTO.Log.SerializeLog(log));
                }
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Total Milliseconds (Insert " + numOfRows.ToString() + " Rows): " + ts.TotalMilliseconds.ToString("#,##0"));

                //start = DateTime.UtcNow;
                //for (int i = 1; i < 200001; i++)
                //{
                //    db.RetrieveLogByIDs((long)i);
                //}
                //stop = DateTime.UtcNow;
                //ts = stop - start;
                //Console.WriteLine("Total Milliseconds (Retrieve 200,000 Logs)" + ts.TotalMilliseconds.ToString("#,##0"));

                start = DateTime.UtcNow;
                var count = db.Count(new DateTime(2011, 11, 24), new DateTime(2011, 11, 26));
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Total Milliseconds " + ts.TotalMilliseconds.ToString("#,##0"));
                Console.WriteLine("Total Date Range Count " + count.ToString("#,##0"));

                start = DateTime.UtcNow;
                count = db.Count();
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Total Milliseconds " + ts.TotalMilliseconds.ToString("#,##0"));
                Console.WriteLine("Total All Count " + count.ToString("#,##0"));
            }
        }
    }
}
