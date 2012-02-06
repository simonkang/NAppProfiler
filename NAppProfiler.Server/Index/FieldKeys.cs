using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Server.Index
{
    class FieldKeys
    {
        public const string LogID = "LogID";
        public const string LogName = "LogName";
        public const string Service = "Service";
        public const string Method = "Method";
        public const string ClientIP = "ClientIP";
        public const string ServerIP = "ServerIP";
        public const string Exception = "Exception";
        public const string CreatedDT = "CreatedDT";
        public const string Elapsed = "Elapsed";
        public const string Detail_LogID = "DetailLogID";
        public const string Detail_LogName = "DetailLogName";
        public const string Detail_CreatedDT = "DetailCreatedDT"; 
        public const string Detail_Desc = "DetailDesc";
        public const string Detail_Elapsed = "DetailElapsed";
        public const string Detail_Parm = "DetailParm";
    }
}
