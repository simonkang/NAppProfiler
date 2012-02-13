using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
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
        //private readonly IndexSearcher searcher;
        private readonly Analyzer analyzer;
        private readonly Sort logNameIDSort;
        private readonly string[] textQueryFields;

        public NAppIndexReader(Configuration.ConfigManager config)
        {
            // TODO: Use Central place to retrieve default setting of Index Full Path
            indexFullPath = config.GetSetting(SettingKeys.Index_Directory, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index"));
            indexFullPath = System.IO.Path.GetFullPath(indexFullPath);
            directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexFullPath));
            analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);

            var sortFields = new SortField[2];
            sortFields[0] = new SortField(FieldKeys.LogName, SortField.STRING);
            sortFields[1] = new SortField(FieldKeys.LogID, SortField.LONG);
            logNameIDSort = new Sort(sortFields);

            textQueryFields = new string[]{
                FieldKeys.Service,
                FieldKeys.Method,
                FieldKeys.Detail_Desc,
                FieldKeys.Detail_Parm,
            };
        }

        public void Search()
        {
            var idx = IndexReader.Open(directory, true);
            var docs = idx.NumDocs();
            Console.WriteLine(docs.ToString("#,##0"));
            //var clientIP = new TermQuery(new Term(FieldKeys.ClientIP, "010026010142"));
            //var dateFilter = NumericRangeFilter.NewLongRange(FieldKeys.CreatedDT, 8, (new DateTime(2011, 10, 31)).Ticks, (new DateTime(2011, 11, 1, 0, 0, 0).Ticks), true, true);
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            var qp = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, FieldKeys.Detail_Desc, analyzer);
            var qryStr = "1* AND " + FieldKeys.Method + ":Method";
            var q = qp.Parse(qryStr); //10.026.010.142");
            //var q = new TermQuery(new Term(FieldKeys.DetailDesc, "2310"));
            var searcher = new IndexSearcher(directory, true);
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

            var qryHeader = new BooleanQuery();

            if (!string.IsNullOrWhiteSpace(query.Text))
            {
                var textBooleanQuery = new BooleanQuery();
                for (int i = 0; i < textQueryFields.Length; i++)
                {
                    var termQry = new TermQuery(new Term(textQueryFields[i], query.Text));
                    textBooleanQuery.Add(termQry, BooleanClause.Occur.SHOULD);
                }
                qryHeader.Add(textBooleanQuery, BooleanClause.Occur.MUST);
            }

            if (query.ClientIP != null)
            {
                var ipStr = NAppIndexUpdater.ConvertIPToString(query.ClientIP);
                var termQry = new TermQuery(new Term(FieldKeys.ClientIP, ipStr));
                qryHeader.Add(termQry, BooleanClause.Occur.MUST);
            }

            if (query.ServerIP != null)
            {
                var ipStr = NAppIndexUpdater.ConvertIPToString(query.ServerIP);
                var termQry = new TermQuery(new Term(FieldKeys.ServerIP, ipStr));
                qryHeader.Add(termQry, BooleanClause.Occur.MUST);
            }

            if (query.ShowExceptions == LogQueryExceptions.ExceptionsOnly)
            {
                var termQry = new TermQuery(new Term(FieldKeys.Exception, "1"));
                qryHeader.Add(termQry, BooleanClause.Occur.MUST);
            }
            else if (query.ShowExceptions == LogQueryExceptions.SuccessesOnly)
            {
                var termQry = new TermQuery(new Term(FieldKeys.Exception, "1"));
                qryHeader.Add(termQry, BooleanClause.Occur.MUST);
            }

            var elapsedQuery = AddFromToQueryString(FieldKeys.Elapsed, query.TotalElapsed_From.Ticks, query.TotalElapsed_To.Ticks, TimeSpan.Zero.Ticks);
            if (elapsedQuery != null)
            {
                qryHeader.Add(elapsedQuery, BooleanClause.Occur.MUST);
            }

            var dtlQry = AddFromToQueryString(FieldKeys.Detail_Elapsed, query.DetailElapsed_From.Ticks, query.DetailElapsed_To.Ticks, TimeSpan.Zero.Ticks);
            if (dtlQry != null)
            {
                qryHeader.Add(dtlQry, BooleanClause.Occur.MUST);
            }

            var ret = new LogQueryResults()
            {
                DateTime_From = query.DateTime_From,
                DateTime_To = query.DateTime_To,
                LogIDs = new List<LogQueryResultDetail>(),
            };
            if (qryHeader.Clauses().Count > 0)
            {
                GetLogIDsFromMain(qryHeader, ret.DateTime_From, ret.DateTime_To, ret.LogIDs);
            }
            return ret;
        }

        void GetLogIDsFromMain(Query qryHeader, DateTime from, DateTime to, IList<LogQueryResultDetail> results)
        {
            var dateFilter = NumericRangeFilter.NewLongRange(FieldKeys.CreatedDT, 8, DateTime.SpecifyKind(from, DateTimeKind.Utc).Ticks, DateTime.SpecifyKind(to, DateTimeKind.Utc).Ticks, true, true);
            var searcher = new IndexSearcher(directory, true);
            var topDocs = searcher.Search(qryHeader, dateFilter, searcher.MaxDoc());
            for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
            {
                //var curDoc = searcher.Doc(topDocs.ScoreDocs[i].doc, new MapFieldSelector(new string[] { FieldKeys.LogName, FieldKeys.LogID }));
                var curDoc = searcher.Doc(topDocs.ScoreDocs[i].doc);
                var curResult = new LogQueryResultDetail()
                {
                    Database = curDoc.Get(FieldKeys.LogName),
                    ID = Convert.ToInt64(curDoc.Get(FieldKeys.LogID), System.Globalization.CultureInfo.InvariantCulture)
                };
                results.Add(curResult);
            }
        }

        Query AddFromToQueryString(string fieldName, long from, long to, long minValue)
        {
            Query qry = null;
            if (from != minValue)
            {
                if (to != minValue)
                {
                    qry = NumericRangeQuery.NewLongRange(fieldName, from, to, true, true);
                }
                else
                {
                    qry = NumericRangeQuery.NewLongRange(fieldName, from, long.MaxValue, true, true);
                }
            }
            else if (to != minValue)
            {
                qry = NumericRangeQuery.NewLongRange(fieldName, 0, to, true, true);
            }
            return qry;
        }

        void ViewTokenTerms()
        {
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            var value = new System.IO.StringReader("Description2 1");
            var tokenStream = analyzer.TokenStream(FieldKeys.Detail_Desc, value);
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
            //if (searcher != null)
            //{
            //    searcher.Dispose();
            //}
            if (directory != null)
            {
                directory.Dispose();
            }
        }
    }
}
