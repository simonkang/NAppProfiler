using System;

namespace NAppProfiler.Client.Sockets
{
    public class Message
    {
        private byte[] data;
        private MessageTypes type;
        private int dataSize;

        public byte[] Data { get { return data; } }

        public MessageTypes Type { get { return type; } }

        public Message()
        {
            this.dataSize = -1;
        }

        public bool AppendData(byte[] buffer, int bufferSize, int startIndex = 0)
        {
            if (this.dataSize == -1)
            {
                // First Byte Data received, set header info
                this.dataSize = BitConverter.ToInt32(buffer, 1);
                this.type = (MessageTypes)Convert.ToInt32(buffer[0]);
                return AppendData(buffer, bufferSize - 5, 5);
            }
            else
            {
                var ret = false;
                var curLen = data == null ? 0 : data.Length;

                if ((curLen + bufferSize - 1) == dataSize)
                {
                    // Matched Data Size
                    ret = true;
                    if (buffer[bufferSize + startIndex - 1] != 0xFF)
                    {
                        // Invalid Message Delimter - Just Exit
                        data = null;
                    }
                    else
                    {
                        AppendBytes(buffer, bufferSize - 1, startIndex, curLen);
                    }
                }
                else if ((curLen + bufferSize - 5) > dataSize)
                {
                    // Data Larger than expected Data Size - Just Exit
                    ret = true;
                    data = null;
                }
                else
                {
                    AppendBytes(buffer, bufferSize, startIndex, curLen);
                }
                return ret;
            }
        }

        private void AppendBytes(byte[] buffer, int bufferSize, int startIndex, int curLen)
        {
            if (data == null)
            {
                data = new byte[bufferSize];
                System.Buffer.BlockCopy(buffer, startIndex, data, 0, bufferSize);
            }
            else
            {
                var combined = new byte[curLen + bufferSize];
                System.Buffer.BlockCopy(data, 0, combined, 0, curLen);
                System.Buffer.BlockCopy(buffer, startIndex, combined, curLen, bufferSize);
                data = combined;
            }
        }

        public static byte[] CreateMessageByte(byte[] data, MessageTypes type)
        {
            var ret = new byte[data.Length + 6];
            ret[0] = Convert.ToByte((int)type);
            var dataSize = BitConverter.GetBytes(data.Length);
            ret[ret.Length - 1] = 0xFF;
            Buffer.BlockCopy(dataSize, 0, ret, 1, 4);
            Buffer.BlockCopy(data, 0, ret, 5, data.Length);
            return ret;
        }
    }
}
