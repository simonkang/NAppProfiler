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
    }
}
