using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NAppProfiler.Client;
using NAppProfiler.Client.DTO;

namespace NAppProfiler.Web.Controllers
{
    public class SearchController : AsyncController
    {
        //
        // GET: /Search/
        public void IndexAsync(NAppProfiler.Web.Models.SearchRequest request)
        {
            AsyncManager.OutstandingOperations.Increment();
            AsyncManager.Parameters["offSetX"] = request.offSetX;
            AsyncManager.Parameters["offSetY"] = request.offSetY;
            NAppProfilerClient.SendQuery(new LogQuery(), AsyncManager);
        }

        public ActionResult IndexCompleted(int offSetX, int offSetY, IList<LogQueryResultDetail> details)
        {
            var list = new List<int[]>();
            Enumerable.Range(0, 5).ToList().ForEach(i => list.Add(new int[] { i + offSetX, i + offSetY, i + offSetX, i + offSetY }));
            return Json(new { points = list, errmsg = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}
