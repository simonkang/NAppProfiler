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
using NAppProfiler.Client.DTO;

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
            var expected = string.Empty;
            var baseD = AppDomain.CurrentDomain.BaseDirectory;
            if (baseD.IndexOf("bin\\Release", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                baseD.IndexOf("bin\\Debug", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                expected = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "DB", "current"));
            }
            else
            {
                expected = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB", "current"));
            }
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
                var log = new LogEntity(DateTime.Now, TimeSpan.FromMilliseconds(300), false, new byte[] { 4, 4, 4 });
                db.InsertLogs(new LogEntity[] { log });
            }
        }

        [Test]
        public void InsertOneLogRecord()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var log = new LogEntity(DateTime.Now, TimeSpan.FromMilliseconds(300), false, new byte[] { 3, 30, 255 });
                var id = db.InsertLogs(new LogEntity[] { log });
                Assert.That(id, Is.Not.Null);
                Assert.That(id, Has.Count.GreaterThan(0));
                Assert.That(id.First(), Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void RetrieveRecordByID()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var search = new LogQueryResults() { IncludeData = true };
                search.LogIDs = new List<LogQueryResultDetail>();
                search.LogIDs.Add(new LogQueryResultDetail() { ID = 1 });
                db.RetrieveLogsBySearchResults(search);
                Assert.AreEqual(1, search.LogIDs[0].ID);
                Assert.That(search.LogIDs[0].Log, Is.Not.Null);
            }
        }

        [Test]
        public void RetrieveRecordsByDate()
        {
            using (var db = new Database(new ConfigManager()))
            {
                db.InitializeDatabase();
                var log = new LogEntity(new DateTime(2011, 11, 9, 0, 0, 0), TimeSpan.FromMilliseconds(300), false, new byte[] { 3, 30, 25 });
                db.InsertLogs(new LogEntity[] { log });
                var search = new LogQueryResults() { IncludeData = true };
                search.DateTime_From = new DateTime(2011, 11, 5, 0, 0, 0, DateTimeKind.Utc);
                search.DateTime_To = new DateTime(2011, 11, 10, 0, 0, 0, DateTimeKind.Utc);
                search.IncludeData = true;
                db.RetrieveLogsBySearchResults(search);
                Assert.That(search.LogIDs, Is.Not.Null);
                Assert.That(search.LogIDs.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        [Ignore]
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
                    var elapsed = TimeSpan.FromMilliseconds((long)rndElapsed.Next(1, 30000)).Ticks;
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
                    var logEnt = new LogEntity(createdDT, new TimeSpan(elapsed), log.IsError, NAppProfiler.Client.DTO.Log.SerializeLog(log));
                    db.InsertLogs(new LogEntity[] { logEnt });
                }
                stop = DateTime.UtcNow;
                ts = stop - start;
                Console.WriteLine("Total Milliseconds (Insert " + numOfRows.ToString() + " Rows): " + ts.TotalMilliseconds.ToString("#,##0"));

                start = DateTime.UtcNow;
                for (int i = 1; i < 200001; i++)
                {
                    var search = new LogQueryResults(){IncludeData = true};
                    search.LogIDs = new List<LogQueryResultDetail>();
                    search.LogIDs.Add(new LogQueryResultDetail(){ID = i});
                    db.RetrieveLogsBySearchResults(search);
                    var logDe = search.LogIDs[0].Log;
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
