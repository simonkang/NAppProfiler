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

        [Test]
        public void VerifyByteAppedingToMessage_AppendByteOneAtATime()
        {
            var msgData = Log.SerializeLog(CreateLog());
            var data = Message.CreateMessageByte(msgData, MessageTypes.SendLog);
            var msg = new Message();
            var status = -3;
            for (int i = 0; i < data.Length; i++)
            {
                status = msg.AppendData(new byte[] { data[i] }, 1, 0);
            }
            Assert.That(msg.Type, Is.EqualTo(MessageTypes.SendLog));
            Assert.That(msg.Data, Is.EquivalentTo(msgData));
            Assert.That(status, Is.EqualTo(1));
        }

        [Test]
        public void VerifyByteAppedingToMessage_AppendByteAtOnce()
        {
            var msgData = Log.SerializeLog(CreateLog());
            var data = Message.CreateMessageByte(msgData, MessageTypes.SendLog);
            var msg = new Message();
            var status = msg.AppendData(data, data.Length, 0);
            Assert.That(msg.Type, Is.EqualTo(MessageTypes.SendLog));
            Assert.That(msg.Data, Is.EquivalentTo(msgData));
            Assert.That(status, Is.EqualTo(data.Length));
        }

        [Test]
        public void VerifyByteAppedingToMessage_AppendByte128BytesAtATime()
        {
            var msgData = Log.SerializeLog(CreateLog());
            var data = Message.CreateMessageByte(msgData, MessageTypes.SendLog);
            var msg = new Message();
            var status = -3;
            var step = 128;
            for (int i = 0; i < data.Length; i += step)
            {
                byte[] buffer = null;
                if ((i + step) < data.Length)
                {
                    buffer = new byte[step];
                    Buffer.BlockCopy(data, i, buffer, 0, step);
                }
                else
                {
                    step = data.Length - i;
                    buffer = new byte[step];
                    Buffer.BlockCopy(data, i, buffer, 0, step);
                }
                status = msg.AppendData(buffer, buffer.Length, 0);
            }
            Assert.That(msg.Type, Is.EqualTo(MessageTypes.SendLog));
            Assert.That(msg.Data, Is.EquivalentTo(msgData));
            Assert.That(status, Is.EqualTo(step));
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
