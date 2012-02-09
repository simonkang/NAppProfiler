using System;
using System.Net.Sockets;
using System.Threading;
using NAppProfiler.Client.DTO;
using NAppProfiler.Client.Sockets;

namespace NAppProfiler.Client
{
    public class NAppProfilerClient
    {
        private static Socket socket;
        private static object socketLock;

        static NAppProfilerClient()
        {
            socketLock = new object();
        }

        public static void SendLog(Log log)
        {
            var data = Log.SerializeLog(log);
            BeginSend(data);
        }

        private static void BeginSend(byte[] data)
        {
            var local = CurrentSocket();
            if (local != null)
            {
                try
                {
                    var msg = Message.CreateMessageByte(data, MessageTypes.SendLog);
                    local.BeginSend(msg, 0, msg.Length, SocketFlags.None, new AsyncCallback(NAppProfilerClient.EndSend), local);
                }
                catch (SocketException)
                {
                    Close();
                }
            }
        }

        private static void EndSend(IAsyncResult ar)
        {
            try
            {
                var s = (Socket)ar.AsyncState;
                s.EndSend(ar);
            }
            catch { }
        }

        private static Socket CurrentSocket()
        {
            if (socket == null)
            {
                NAppProfilerClient.Connect();
            }
            return socket;
        }

        private static void Connect()
        {
            lock (socketLock)
            {
                if (socket == null)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        socket.Connect("127.0.0.1", 33700);
                        if (!socket.Connected)
                        {
                            Close();
                        }
                    }
                    catch (SocketException)
                    {
                        Close();
                    }
                }
            }
        }

        public static void Close()
        {
            if (socket != null)
            {
                lock (socketLock)
                {
                    if (socket != null)
                    {
                        if (socket.Connected)
                        {
                            socket.Shutdown(SocketShutdown.Both);
                        }
                        socket.Close();
                        socket = null;
                    }
                }
            }
        }
    }
}
