using System;
using System.Collections.Generic;
using NUnit.Framework;
using NAppProfiler.Client.DTO;
using NAppProfiler.Client.Sockets;
using NAppProfiler.Server.Sockets;

namespace NAppProfiler.Server.Tests.Sockets
{
    [TestFixture]
    public class ReceiveStateObjectTests
    {
        [Test]
        public void VerifyMessageIsCreated_OneMessage()
        {
            var msgData = Log.SerializeLog(CreateLog());
            var data = Message.CreateMessageByte(msgData, MessageTypes.SendLog);
            var target = new ReceiveStateObject();
            Buffer.BlockCopy(data, 0, target.Buffer, 0, data.Length);
            var ret = target.AppendBuffer(data.Length);
            Assert.That(ret, Is.GreaterThanOrEqualTo(0));
            Assert.That(target.Status, Is.EqualTo(ReceiveStatuses.Finished));
            Assert.That(msgData, Is.EquivalentTo(target.Data));
        }

        [Test]
        public void VerifyMessageIsCreate_TwoMessages_OneBuffer()
        {
            var msgData1 = Log.SerializeLog(CreateLog());
            var data1 = Message.CreateMessageByte(msgData1, MessageTypes.SendLog);
            var msgData2 = Log.SerializeLog(CreateLog());
            var data2 = Message.CreateMessageByte(msgData2, MessageTypes.SendLog);
            var target = new ReceiveStateObject();
            Buffer.BlockCopy(data1, 0, target.Buffer, 0, data1.Length);
            Buffer.BlockCopy(data2, 0, target.Buffer, data1.Length, data2.Length);
            var ret = target.AppendBuffer(data1.Length + data2.Length);
            Assert.That(ret, Is.GreaterThanOrEqualTo(0));
            Assert.That(target.Status, Is.EqualTo(ReceiveStatuses.Finished));
            Assert.That(target.Data, Is.EquivalentTo(msgData1));

            target.Clear();
            ret = target.AppendBuffer(data1.Length + data2.Length, ret);
            Assert.That(ret, Is.GreaterThanOrEqualTo(0));
            Assert.That(target.Status, Is.EqualTo(ReceiveStatuses.Finished));
            Assert.That(msgData2, Is.EquivalentTo(target.Data));
        }

        [Test]
        public void VerifyMessageIsCleared()
        {
            var msgData = Log.SerializeLog(CreateLog());
            var data = Message.CreateMessageByte(msgData, MessageTypes.SendLog);
            var target = new ReceiveStateObject();
            Buffer.BlockCopy(data, 0, target.Buffer, 0, data.Length);
            var ret = target.AppendBuffer(data.Length);
            Assert.That(ret, Is.GreaterThanOrEqualTo(0));
            Assert.That(target.Status, Is.EqualTo(ReceiveStatuses.Finished));
            Assert.That(data, Is.SubsetOf(target.Buffer));

            target.Clear();
            Assert.That(target.Status, Is.EqualTo(ReceiveStatuses.Receiving));
            Assert.That(target.Data, Is.Null);
        }

        [Test]
        public void VerifyMessageThatExceeedsBuffer()
        {
            var msgData = Log.SerializeLog(CreateLog(true));
            var data = Message.CreateMessageByte(msgData, MessageTypes.SendLog);
            var target = new ReceiveStateObject();
            var curIndex = 0;
            var ret = -3;
            while (curIndex < data.Length)
            {
                var len = target.Buffer.Length;
                if (curIndex + target.Buffer.Length > data.Length)
                {
                    len = data.Length - curIndex;
                }
                Buffer.BlockCopy(data, curIndex, target.Buffer, 0, len);
                ret = target.AppendBuffer(len);
                curIndex += target.Buffer.Length;
            }

            Assert.That(ret, Is.GreaterThanOrEqualTo(0));
            Assert.That(target.Status, Is.EqualTo(ReceiveStatuses.Finished));
            Assert.That(msgData, Is.EquivalentTo(target.Data));
        }

        private Log CreateLog(bool large = false)
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
            for (int i = 0; i < 100; i++)
            {
                details.Add(new LogDetail()
                {
                    CreatedDateTime = DateTime.Now,
                    Description = "testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two ",
                    Elapsed = TimeSpan.FromMilliseconds(500).Ticks,
                });
                if (!large)
                {
                    break;
                }
            }
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
