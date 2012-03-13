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
            AsyncManager.Parameters["canvasWidth"] = request.canvasWidth;
            AsyncManager.Parameters["canvasHeight"] = request.canvasHeight;
            var req = new LogQuery()
            {
                DateTime_From = DateTime.MinValue.AddYears(1),
                DateTime_To = DateTime.MaxValue,
            };
            NAppProfilerClient.SendQuery(req, AsyncManager);
        }

        public ActionResult IndexCompleted(int offSetX, int offSetY, int canvasWidth, int canvasHeight, IList<LogQueryResultDetail> details)
        {
            var list = new List<int[]>();
            //list.Add(new int[] { offSetX, offSetY, offSetX, offSetY + canvasHeight, 255, 0, 0 });
            //list.Add(new int[] { offSetX, offSetY + canvasHeight, offSetX + canvasWidth, offSetY + canvasHeight, 255, 0, 0 });
            //list.Add(new int[] { offSetX + canvasWidth, offSetY + canvasHeight, offSetX + canvasWidth, offSetY, 255, 0, 0 });
            //list.Add(new int[] { offSetX + canvasWidth, offSetY, offSetX, offSetY, 255, 0, 0 });

            var curDT = DateTime.MinValue;
            var curElapsed = -1L;
            foreach (var cur in details.AsParallel().OrderBy(i => i.Elapsed).ThenBy(i => i.CreatedDateTime))
            {
            }
            return Json(new { points = list, errmsg = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}
