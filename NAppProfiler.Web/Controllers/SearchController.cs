using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NAppProfiler.Web.Controllers
{
    public class SearchController : AsyncController
    {
        //
        // GET: /Search/
        public void IndexAsync(
                int offSetX, int offSetY,
                bool exceptionOnly, 
                string dateOptions, 
                string fromDate, 
                string fromTime, 
                string toDate, 
                string toTime, 
                string textSearch, 
                string clientIP, 
                string serverIP, 
                string totalFrom, 
                string totalTo, 
                string detailedFrom, 
                string detailedTo)
        {
            AsyncManager.OutstandingOperations.Increment();
            AsyncManager.Parameters["offSetX"] = offSetX;
            AsyncManager.Parameters["offSetY"] = offSetY;
            AsyncManager.OutstandingOperations.Decrement();
        }

        public ActionResult IndexCompleted(int offSetX, int offSetY)
        {
            var list = new List<int[]>();
            Enumerable.Range(0, 5).ToList().ForEach(i => list.Add(new int[] { i + offSetX, i + offSetY, i + offSetX, i + offSetY }));
            return Json(new { points = list, errmsg = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}
