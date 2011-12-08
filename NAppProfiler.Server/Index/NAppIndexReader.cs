using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.QueryParsers;
using NAppProfiler.Server.Configuration;
using Lucene.Net.Analysis.Tokenattributes;

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
            searcher = new IndexSearcher(directory, true);

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
