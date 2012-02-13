using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAppProfiler.Client.DTO;
using NAppProfiler.Server.Essent;

namespace NAppProfiler.Server.Manager
{
    public class JobMethods
    {
        // All Database Methods must be greater than 900
        public const int Empty = 0;
        public const int Index_QueryRequest = 101;
        public const int Database_InsertLogs = 901;
        public const int Database_RetrieveLogs = 902;
        public const int Database_UpdateIndex = 903;
    }

    public class JobItem
    {
        private readonly int method;

        private bool processed;
        private IList<LogEntity> logEntityItems;
        private IList<long> logIDs;
        private IList<LogQuery> querys;

        public JobItem(int method)
        {
            this.method = method;
        }

        public bool Processed
        {
            get { return processed; }
            set { processed = value; }
        }

        public int Method
        {
            get { return method; }
        }

        public IList<LogEntity> LogEntityItems
        {
            get { return logEntityItems; }
            set { logEntityItems = value; }
        }

        public IList<long> LogIDs
        {
            get { return logIDs; }
            set { logIDs = value; }
        }

        public IList<LogQuery> LogQueries
        {
            get { return querys; }
            set { querys = value; }
        }
    }
}
