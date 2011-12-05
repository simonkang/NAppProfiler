using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Index
{
    public class Updater
    {
        private readonly Configuration.ConfigManager config;

        public Updater(Configuration.ConfigManager config)
        {
            this.config = config;
        }
    }
}
