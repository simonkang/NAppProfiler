using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Configuration
{
    public class ConfigManager
    {
        public string GetSetting(string key, string defaultValue = "")
        {
            var ret = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(ret))
            {
                return defaultValue;
            }
            return ret;
        }
    }
}
