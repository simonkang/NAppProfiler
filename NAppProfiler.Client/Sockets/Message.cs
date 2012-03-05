using System;
using System.Dynamic;

namespace NAppProfiler.Client.Sockets
{
    public class Message
    {
        private byte[] data;
        private MessageTypes type;
        private int dataSize;
        private int curPosition;
        private byte[] hdr;
        private int hdrIndex;
        private Guid messageGuid;
        private object messageBag;

        public byte[] Data { get { return data; } }

        public MessageTypes Type { get { return type; } }

        public Guid MessageGuid { get { return messageGuid; } set { messageGuid = value; } }

        public object MessageBag { get { return messageBag; } set { messageBag = value; } }

        public Message()
        {
            this.dataSize = -1;
            this.hdrIndex = 0;
            this.type = MessageTypes.Invalid;
        }

        private void AppendBytes(byte[] buffer, int bufferSize, int startIndex)
        {
            if (data == null)
            {
                curPosition = bufferSize;
                data = new byte[this.dataSize];
                System.Buffer.BlockCopy(buffer, startIndex, data, 0, bufferSize);
            }
            else
            {
                System.Buffer.BlockCopy(buffer, startIndex, data, curPosition, bufferSize);
                curPosition += bufferSize;
            }
        }

        public static byte[] CreateMessageByte(byte[] data, MessageTypes type)
        {
            return CreateMessageByte(data, type, Guid.Empty);
        }

        public static byte[] CreateMessageByte(byte[] data, MessageTypes type, Guid messageGuid)
        {
            var ret = new byte[data.Length + 22];
            ret[0] = Convert.ToByte((int)type);
            var dataSize = BitConverter.GetBytes(data.Length);
            ret[ret.Length - 1] = 0xFF;
            Buffer.BlockCopy(dataSize, 0, ret, 1, 4);
            Buffer.BlockCopy(messageGuid.ToByteArray(), 0, ret, 5, 16);
            Buffer.BlockCopy(data, 0, ret, 21, data.Length);
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="startIndex"></param>
        /// <returns>-2:Invalid Data, -1:More Data, >=0:Done with Start Index</returns>
        public int AppendData(byte[] buffer, int bufferSize, int startIndex)
        {
            if (this.dataSize == -1)
            {
                return SetHeader(buffer, bufferSize, startIndex);
            }
            else
            {
                var ret = -1; // more data
                var msgEndIndex = dataSize - curPosition + startIndex;
                if (msgEndIndex < bufferSize + startIndex)
                {
                    // Data is Completed
                    if (buffer[msgEndIndex] == 0xFF)
                    {
                        //Valid Delimter
                        AppendBytes(buffer, msgEndIndex - startIndex, startIndex);
                        ret = msgEndIndex + 1;
                    }
                    else
                    {
                        //Invalid Delimter
                        ret = -2;
                    }
                }
                else
                {
                    AppendBytes(buffer, bufferSize, startIndex);
                }
                return ret;
            }
        }

        private int SetHeader(byte[] buffer, int bufferSize, int startIndex)
        {
            // First Byte Data received, set header info
            if (startIndex + 21 - hdrIndex >= bufferSize)
            {
                // Header continues to next buffer
                if (hdr == null)
                {
                    this.hdr = new byte[21];
                }
                for (int i = startIndex; i < bufferSize; i++)
                {
                    this.hdr[hdrIndex] = buffer[i];
                    hdrIndex++;
                }
                return -1;
            }
            else
            {
                if (hdr == null)
                {
                    this.dataSize = BitConverter.ToInt32(buffer, startIndex + 1);
                    this.type = (MessageTypes)Convert.ToInt32(buffer[startIndex]);
                    var bGuid = new byte[16];
                    Buffer.BlockCopy(buffer, 5, bGuid, 0, 16);
                    this.messageGuid = new Guid(bGuid);
                    return AppendData(buffer, bufferSize - startIndex - 21, startIndex + 21);
                }
                else
                {
                    // Append Header Info from Previous buffer
                    int j = 0;
                    for (int i = hdrIndex; i < 5; i++)
                    {
                        this.hdr[i] = buffer[j];
                        j++;
                    }
                    this.dataSize = BitConverter.ToInt32(hdr, 1);
                    this.type = (MessageTypes)Convert.ToInt32(hdr[0]);
                    hdr = null;
                    return AppendData(buffer, bufferSize - j, startIndex + j);
                }
            }
        }
    }
}
