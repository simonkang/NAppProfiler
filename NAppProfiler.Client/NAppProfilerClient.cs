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
        private static int isSending;

        static NAppProfilerClient()
        {
            socketLock = new object();
            isSending = 0;
        }

        public void SendLog(Log log)
        {
            var data = Log.SerializeLog(log);
            BeginSend(data);
        }

        private void BeginSend(byte[] data)
        {
            var local = CurrentSocket();
            if (local != null)
            {
                try
                {
                    while (Interlocked.CompareExchange(ref isSending, 1, 0) == 1) ;

                    var msg = Message.CreateMessageByte(data, MessageTypes.SendLog);
                    local.BeginSend(msg, 0, msg.Length, SocketFlags.None, new AsyncCallback(NAppProfilerClient.EndSend), local);
                }
                catch (SocketException)
                {
                    Close();
                    Interlocked.Exchange(ref isSending, 0);
                }
            }
        }

        private static void EndSend(IAsyncResult ar)
        {
            var s = (Socket)ar.AsyncState;
            s.EndSend(ar);
            Interlocked.Exchange(ref isSending, 0);
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
