using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NAppProfiler.Web.Models
{
    public class SearchRequest
    {
        public int offSetX { get; set; }
        public int offSetY { get; set; }
        public bool exceptionOnly { get; set; }
        public string dateOptions { get; set; }
        public string fromDate { get; set; }
        public string fromTime { get; set; }
        public string toDate { get; set; }
        public string toTime { get; set; }
        public string textSearch { get; set; }
        public string clientIP { get; set; }
        public string serverIP { get; set; }
        public string totalFrom { get; set; }
        public string totalTo { get; set; }
        public string detailedFrom { get; set; }
        public string detailedTo { get; set; }
    }
}