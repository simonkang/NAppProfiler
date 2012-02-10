using System;
using System.Collections.Generic;
using NUnit.Framework;
using NAppProfiler.Client.DTO;
using NAppProfiler.Client.Sockets;

namespace NAppProfiler.Client.Tests.Sockets
{
    [TestFixture]
    public class MessageTests
    {
        [Test]
        public void VerifyMessageByteCreated()
        {
            var log = CreateLog();
            var data = Log.SerializeLog(log);
            var msg = Message.CreateMessageByte(data, MessageTypes.SendLog);
            Assert.That(msg, Is.Not.Null);
            Assert.That(msg[0], Is.EqualTo((int)MessageTypes.SendLog));
            Assert.That(msg[msg.Length - 1], Is.EqualTo(0xFF));

            var dataSize = BitConverter.ToInt32(new byte[] { msg[1], msg[2], msg[3], msg[4] }, 0);
            Assert.That(dataSize, Is.EqualTo(data.Length));
            Assert.That(data, Is.SubsetOf(msg));
        }

        private Log CreateLog()
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

    }
}
