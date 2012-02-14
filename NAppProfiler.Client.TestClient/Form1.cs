using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAppProfiler.Client;
using NAppProfiler.Client.DTO;
using System.Diagnostics;

namespace NAppProfiler.Client.TestClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            int times;
            if (!int.TryParse(ucTimes.Text, out times))
            {
                times = 1;
            }
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < times; i++)
            {
                var log = CreateLog();
                NAppProfilerClient.SendLog(log);
            }
            sw.Stop();
            MessageBox.Show(string.Format("Elapsed ms: {0}\r\nElapsed ticks: {1}", sw.ElapsedMilliseconds.ToString("#,##0"), sw.ElapsedTicks.ToString("#,##0")));
        }

        Log CreateLog()
        {
            var details = new List<LogDetail>();
            var parm = new LogParm() { Name = "abc", StringType = true, Value = "def" };
            details.Add(new LogDetail()
            {
                CreatedDateTime = DateTime.Now,
                Description = "Testing",
                Elapsed = TimeSpan.FromMilliseconds(300).Ticks,
                Parameters = new List<LogParm>(new LogParm[] { parm }),
            });
            details.Add(new LogDetail()
            {
                CreatedDateTime = DateTime.Now,
                Description = "testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two testsing two ",
                Elapsed = TimeSpan.FromMilliseconds(500).Ticks,
            });
            var ret = new Log()
            {
                ClientIP = new byte[] { 10, 26, 10, 142 },
                CreatedDateTime = DateTime.Now,
                Details = details,
                Elapsed = TimeSpan.FromMilliseconds(600).Ticks,
                IsError = false,
                Method = "mae",
                Service = "vewavea",
            };
            return ret;
        }

        private void btnInit_Click(object sender, EventArgs e)
        {
            NAppProfilerClient.Initialize(null, txtIP.Text, 0);
        }

        private void btnEmpty_Click(object sender, EventArgs e)
        {
            int times;
            if (!int.TryParse(ucTimes.Text, out times))
            {
                times = 1;
            }
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < times; i++)
            {
                NAppProfilerClient.SendEmpty();
            }
            sw.Stop();
            MessageBox.Show(string.Format("Elapsed ms: {0}\r\nElapsed ticks: {1}", sw.ElapsedMilliseconds.ToString("#,##0"), sw.ElapsedTicks.ToString("#,##0")));
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            var qry = new LogQuery();
            qry.DateTime_From = new DateTime(2012, 02, 14);
            qry.DateTime_To = new DateTime(2012, 02, 16);
            NAppProfilerClient.SendQuery(qry);
        }
    }
}
