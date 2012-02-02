using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.QueryParsers;
using NAppProfiler.Client.DTO;
using NAppProfiler.Server.Configuration;

namespace NAppProfiler.Server.Index
{
    public class NAppIndexReader : IDisposable
    {
        private readonly string indexFullPath;
        private readonly Directory directory;
        private readonly IndexSearcher searcher;
        private readonly Sort logNameIDSort;

        public NAppIndexReader(Configuration.ConfigManager config)
        {
            // TODO: Use Central place to retrieve default setting of Index Full Path
            indexFullPath = config.GetSetting(SettingKeys.Index_Directory, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index"));
            indexFullPath = System.IO.Path.GetFullPath(indexFullPath);
            directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexFullPath));
            var dt = DateTime.UtcNow;
            searcher = new IndexSearcher(directory, true);
            var dt2 = DateTime.UtcNow;
            var ts = dt2 - dt;

            var sortFields = new SortField[2];
            sortFields[0] = new SortField(FieldKeys.LogName, SortField.STRING);
            sortFields[1] = new SortField(FieldKeys.LogID, SortField.LONG);
            logNameIDSort = new Sort(sortFields);
        }

        public void Search()
        {
            var idx = IndexReader.Open(directory, true);
            var docs = idx.NumDocs();
            Console.WriteLine(docs.ToString("#,##0"));
            //var clientIP = new TermQuery(new Term(FieldKeys.ClientIP, "010026010142"));
            //var dateFilter = NumericRangeFilter.NewLongRange(FieldKeys.CreatedDT, 8, (new DateTime(2011, 10, 31)).Ticks, (new DateTime(2011, 11, 1, 0, 0, 0).Ticks), true, true);
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            var qp = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, FieldKeys.DetailDesc, analyzer);
            var qryStr = "1* AND " + FieldKeys.Method + ":Method";
            var q = qp.Parse(qryStr); //10.026.010.142");
            //var q = new TermQuery(new Term(FieldKeys.DetailDesc, "2310"));
            var topDocs = searcher.Search(q, null, 200);
            for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
            {
                var curDoc = searcher.Doc(topDocs.ScoreDocs[i].doc);
            }
        }

        public LogQueryResults Search(LogQuery query)
        {
            if (query.DateTime_From == DateTime.MinValue || query.DateTime_To == DateTime.MinValue)
            {
                throw new ArgumentException("DateTime_From or DateTime_To Not Set");
            }

            var qryStr = string.Empty;
            if (query.ClientIP != null)
            {
                var ipStr = NAppIndexUpdater.ConvertIPToString(query.ClientIP);
                qryStr = FieldKeys.ClientIP + ":" + ipStr;
            }

            if (query.ServerIP != null)
            {
                var ipStr = NAppIndexUpdater.ConvertIPToString(query.ServerIP);
                qryStr += " " + FieldKeys.ServerIP + ":" + ipStr;
            }

            if (query.ShowExceptions == LogQueryExceptions.ExceptionsOnly)
            {
                qryStr += " " + FieldKeys.Exception + ":1";
            }
            else if (query.ShowExceptions == LogQueryExceptions.SuccessesOnly)
            {
                qryStr += " " + FieldKeys.Exception + ":0";
            }

            qryStr += AddFromToQueryString(FieldKeys.Elapsed, query.TotalElapsed_From.Ticks, query.TotalElapsed_To.Ticks, TimeSpan.Zero.Ticks);
            qryStr += AddFromToQueryString(FieldKeys.DetailElapsed, query.DetailElapsed_From.Ticks, query.DetailElapsed_To.Ticks, TimeSpan.Zero.Ticks);

            if (string.IsNullOrWhiteSpace(qryStr))
            {
                return new LogQueryResults();
            }
            var ret = new LogQueryResults();
            return ret;
        }

        string AddFromToQueryString(string fieldName, long from, long to, long minValue)
        {
            var qryStr = string.Empty;
            if (from != minValue)
            {
                if (to != minValue)
                {
                    qryStr += string.Format(" {0}:[{1} TO {2}]", fieldName, from.ToString(), to.ToString());
                }
                else
                {
                    qryStr += string.Format(" {0}:[{1} TO 9223372036854775807", fieldName, from.ToString());
                }
            }
            else if (to != minValue)
            {
                qryStr += string.Format(" {0}:[0 to {1}]", fieldName, to.ToString());
            }
            return qryStr;
        }

        void ViewTokenTerms()
        {
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            var value = new System.IO.StringReader("Description2 1");
            var tokenStream = analyzer.TokenStream(FieldKeys.DetailDesc, value);
            OffsetAttribute offSet = tokenStream.GetAttribute(typeof(OffsetAttribute)) as OffsetAttribute;
            TermAttribute termAttr = tokenStream.GetAttribute(typeof(TermAttribute)) as TermAttribute;
            while (tokenStream.IncrementToken())
            {
                offSet.StartOffset();
                offSet.EndOffset();
                var term = termAttr.Term();
            }

        }

        public void Dispose()
        {
            if (searcher != null)
            {
                searcher.Dispose();
            }
            if (directory != null)
            {
                directory.Dispose();
            }
        }
    }
}
