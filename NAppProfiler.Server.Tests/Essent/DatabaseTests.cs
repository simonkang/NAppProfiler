using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using NUnit.Framework;
using NUnit.Framework.Constraints;
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
            var expected = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "DB", "current"));
            using (var db = new Database(new ConfigManager()))
            {
                var filePath = db.GetDatabaseDirectory();
                Assert.That(filePath, Is.EqualTo(expected));
                //Assert.AreEqual(filePath, expected);
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
                Assert.That(File.Exists(db.DatabaseFullPath));
                var log = new LogEntity(DateTime.Now, TimeSpan.FromMilliseconds(300), new byte[] { 4, 4, 4 });
                db.InsertLogs(new LogEntity[] { log });
            }
        }

        [Test]
        public void InsertOneLogRecord()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var log = new LogEntity(DateTime.Now, TimeSpan.FromMilliseconds(300), new byte[] { 3, 30, 255 });
                var id = db.InsertLogs(new LogEntity[] { log });
                Assert.That(id, Is.Not.Null);
                Assert.That(id, Is.Not.LessThanOrEqualTo(0));
            }
        }

        [Test]
        public void RetrieveRecordByID()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var ids = new List<long>(1);
                ids.Add(1);
                var log = db.RetrieveLogByIDs(ids);
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
                var numOfRows = 50000;
                var interval = (long)((DateTime.Now - insertStart).Ticks / numOfRows);
                var rndElapsed = new Random();
                for (int i = 0; i < numOfRows; i++)
                {
                    var createdDT = insertStart.AddTicks(interval * i);
                    var elapsed = (long)rndElapsed.Next(1, 30000);
                    var log = new NAppProfiler.Client.DTO.Log()
                    {
                        ClientIP = new byte[] { 10, 26, 10, 142 },
                        CreatedDateTime = createdDT,
                        Details = new List<NAppProfiler.Client.DTO.LogDetail>(),
                        Elapsed = elapsed,
                        IsError = Convert.ToBoolean(rndElapsed.Next(0, 1)),
                        Method = "Method",
                        Service = "Service",
                    };
                    log.Details.Add(new Client.DTO.LogDetail()
                    {
                        CreatedDateTime = createdDT,
                        Description = "Description " + i.ToString(),
                        Elapsed = 100,
                    });
                    log.Details.Add(new Client.DTO.LogDetail()
                    {
                        CreatedDateTime = createdDT,
                        Description = "Description2 " + i.ToString(),
                        Elapsed = 100,
                    });
                    var logEnt = new LogEntity(createdDT, new TimeSpan(elapsed), NAppProfiler.Client.DTO.Log.SerializeLog(log));
                    db.InsertLogs(new LogEntity[] { logEnt });
                }
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Total Milliseconds (Insert " + numOfRows.ToString() + " Rows): " + ts.TotalMilliseconds.ToString("#,##0"));

                start = DateTime.UtcNow;
                for (int i = 1; i < 200001; i++)
                {
                    var log = db.RetrieveLogByIDs(new long[] { (long)i });
                    var logDe = NAppProfiler.Client.DTO.Log.DeserializeLog(log[0].Data);
                }
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Total Milliseconds (Retrieve 200,000 Logs)" + ts.TotalMilliseconds.ToString("#,##0"));

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
