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
            var client = new NAppProfilerClient();
            var log = CreateLog();
            client.SendLog(log);
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
    }
}
