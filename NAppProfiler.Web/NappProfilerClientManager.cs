using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc.Async;
using NAppProfiler.Client.Sockets;
using NAppProfiler.Client.DTO;

namespace NAppProfiler.Web
{
    public class NappProfilerClientManager
    {
        public static void OnMessageArrived(Message msg)
        {
            if (msg.MessageBag != null)
            {
                var asyncMgr = msg.MessageBag as AsyncManager;
                if (asyncMgr != null)
                {
                    ProcessMessage(msg, asyncMgr);
                }
            }
        }

        private static void ProcessMessage(Message msg, AsyncManager asyncMgr)
        {
            switch (msg.Type)
            {
                case MessageTypes.Results:
                    ProcessResults(msg, asyncMgr);
                    break;
            }
            asyncMgr.OutstandingOperations.Decrement();
        }

        private static void ProcessResults(Message msg, AsyncManager asyncMgr)
        {
            var results = LogQueryResults.DeserializeLog(msg.Data);
            asyncMgr.Parameters["details"] = results.LogIDs;
        }
    }
}