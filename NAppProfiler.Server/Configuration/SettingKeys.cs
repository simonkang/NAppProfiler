using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Configuration
{
    public static class SettingKeys
    {
        public static string Database_Directory { get { return "Database_Directory"; } }
        public static string Database_CacheSizeMax { get { return "Database_CacheSizeMax"; } }
        public static string Database_CacheSizeMin { get { return "Database_CacheSizeMin"; } }
        public static string Database_LogDirectory { get { return "Database_LogDirectory"; } }

        public static string Index_Directory { get { return "Index_Directory"; } }

        public static string Manager_MaxTasks { get { return "Manager_MaxTasks"; } }
        public static string Manager_QueueSize { get { return "Manager_QueueSize"; } }

        public static string Trace_Logging { get { return "Trace_Logging"; } }
    }
}
