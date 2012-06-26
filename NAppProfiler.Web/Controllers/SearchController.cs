using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NAppProfiler.Client;
using NAppProfiler.Client.DTO;
using NAppProfiler.Web.Models;
using System.Globalization;

namespace NAppProfiler.Web.Controllers
{
    public class SearchController : AsyncController
    {
        //
        // GET: /Search/
        public void IndexAsync(SearchRequest request)
        {
            var validReq = ValidateSearchRequest(request);
            if (string.IsNullOrWhiteSpace(validReq.Item2))
            {
                AsyncManager.OutstandingOperations.Increment();
                AsyncManager.Parameters["offSetX"] = request.offSetX;
                AsyncManager.Parameters["offSetY"] = request.offSetY;
                AsyncManager.Parameters["canvasWidth"] = request.canvasWidth;
                AsyncManager.Parameters["canvasHeight"] = request.canvasHeight;
                NAppProfilerClient.SendQuery(validReq.Item1, AsyncManager);
            }
            else
            {
                AsyncManager.Parameters["errMsg"] = validReq.Item2;
            }
        }

        public ActionResult IndexCompleted(int offSetX, int offSetY, int canvasWidth, int canvasHeight, string errMsg, IList<LogQueryResultDetail> details)
        {
            if (string.IsNullOrWhiteSpace(errMsg))
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
            return Json(new { errMsg = errMsg }, JsonRequestBehavior.AllowGet);
        }

        private Tuple<LogQuery, string> ValidateSearchRequest(SearchRequest request)
        {
            var req = new LogQuery();
            var errMsg = string.Empty;
            DateTime tempTime;
            if (DateTime.TryParseExact(request.fromDate, "yy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out tempTime))
            {
                req.DateTime_From = tempTime;
            }
            else
            {
            }
            return new Tuple<LogQuery, string>(req, errMsg);
        }

        private DateTime? ParseDateTime(string dt)
        {
            DateTime temp;
            if (DateTime.TryParseExact(dt, "yy-MM-dd HH:mm", 
       }
    }
}
