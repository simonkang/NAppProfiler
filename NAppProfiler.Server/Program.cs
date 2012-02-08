using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAppProfiler.Server.Manager;
using NAppProfiler.Server.Configuration;

namespace NAppProfiler.Server
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            using (var listener = new NAppProfiler.Server.Sockets.Listener())
            using (var queueMgr = new JobQueueManager(new ConfigManager()))
            {
                queueMgr.Initialize();
                listener.Initialize();
                Console.WriteLine("Started and Listening");
                var line = string.Empty;
                while (!line.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    line = Console.ReadLine();
                }
                Console.WriteLine("Exiting...");
            }
        }
    }
}
