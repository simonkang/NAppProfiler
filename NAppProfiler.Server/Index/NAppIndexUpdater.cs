using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NAppProfiler.Client.DTO;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Essent;

namespace NAppProfiler.Server.Index
{
    public class NAppIndexUpdater : IDisposable
    {
        private readonly string indexFullPath;
        private readonly Directory directory;
        private readonly Database db;
        private IndexWriter writer;

        private Document doc;
        private NumericField fLogID;
        private Field fSvc;
        private Field fMethod;
        private Field fClientIP;
        private Field fServerIP;
        private NumericField fException;
        private NumericField fCreated;
        private NumericField fElapsed;
        private Field fDetailDescription;
        private Field fParameterValue; 

        public NAppIndexUpdater(Configuration.ConfigManager config)
        {
            indexFullPath = config.GetSetting(SettingKeys.Index_Directory, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index"));
            indexFullPath = System.IO.Path.GetFullPath(indexFullPath);
            directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexFullPath));
            db = new Database(config);
        }

        private void InitializeDocumentCache()
        {
            doc = new Document();
            fLogID = new NumericField(FieldKeys.LogID, 8, Field.Store.YES, false);
            doc.Add(fLogID);
            fSvc = new Field(FieldKeys.Service, string.Empty, Field.Store.NO, Field.Index.ANALYZED);
            doc.Add(fSvc);
            fMethod = new Field(FieldKeys.Method, string.Empty, Field.Store.NO, Field.Index.ANALYZED);
            doc.Add(fMethod);
            fClientIP = new Field(FieldKeys.ClientIP, string.Empty, Field.Store.NO, Field.Index.NOT_ANALYZED);
            doc.Add(fClientIP);
            fServerIP = new Field(FieldKeys.ServerIP, string.Empty, Field.Store.NO, Field.Index.NOT_ANALYZED);
            doc.Add(fServerIP);
            fException = new NumericField(FieldKeys.Exception, 1, Field.Store.NO, true);
            doc.Add(fException);
            fCreated = new NumericField(FieldKeys.CreatedDT, 8, Field.Store.NO, true);
            doc.Add(fCreated);
            fElapsed = new NumericField(FieldKeys.Elapsed, 8, Field.Store.NO, true);
            doc.Add(fElapsed);
            fDetailDescription = new Field(FieldKeys.DetailDesc, string.Empty, Field.Store.NO, Field.Index.ANALYZED);
            doc.Add(fDetailDescription);
            fParameterValue = new Field(FieldKeys.Parms, string.Empty, Field.Store.NO, Field.Index.ANALYZED);
            doc.Add(fParameterValue);
        }

        public void Initialize()
        {
            writer = new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), IndexWriter.MaxFieldLength.UNLIMITED);
            writer.SetRAMBufferSizeMB(48);
            writer.SetMergeFactor(100);
            InitializeDocumentCache();
            db.InitializeDatabase();
        }

        public long UpdateIndex()
        {
            long ret = 0;
            var logIds = db.GetLogsToIndex(50);
            while (logIds.Count > 0)
            {
                var logArray = new long[logIds.Count];
                var idArray = new long[logIds.Count];
                for (int i = 0; i < logIds.Count; i++)
                {
                    idArray[i] = logIds[i].Item1;
                    logArray[i] = logIds[i].Item2;
                }
                var logEntries = db.RetrieveLogByIDs(logArray);
                foreach (var log in logEntries)
                {
                    AddDocumentToIndex(log.ID, log.Data);
                    ret++;
                }
                //writer.Commit();
                db.DeleteIndexRows(idArray);
                logIds = db.GetLogsToIndex(50);
            }
            writer.Commit();
            return ret;
        }

        public long RebuildIndex()
        {
            long ret = 0;
            writer.Dispose();
            writer = new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), true, IndexWriter.MaxFieldLength.UNLIMITED);
            writer.SetRAMBufferSizeMB(48);
            writer.SetMergeFactor(100);
            db.AddAllLogsToReindex();
            ret = UpdateIndex();
            return ret;
        }

        void AddDocumentToIndex(long id, byte[] data)
        {
            var log = Log.DeserializeLog(data);
            fLogID.SetLongValue(id);
            fSvc.SetValue(log.Svc);
            fMethod.SetValue(log.Mtd);
            fClientIP.SetValue(ConvertIPToString(log.CIP));
            fServerIP.SetValue(ConvertIPToString(log.SIP));
            fException.SetIntValue(Convert.ToInt32(log.Err));
            fCreated.SetLongValue(log.CrDT.Ticks);
            fElapsed.SetLongValue(log.ED);

            if (log.Dtl.Count > 0)
            {
                var desc = new StringBuilder();
                var parms = new StringBuilder();
                for (int x = 0; x < log.Dtl.Count; x++)
                {
                    var curDtl = log.Dtl[x];
                    desc.Append(curDtl.Dsc + " ");
                    if (curDtl.Ps != null)
                    {
                        for (int y = 0; y < curDtl.Ps.Count; y++)
                        {
                            parms.Append(curDtl.Ps[y].Val + " ");
                        }
                    }
                }
                fDetailDescription.SetValue(desc.ToString());
                fParameterValue.SetValue(parms.ToString());
            }
            else
            {
                fDetailDescription.SetValue(string.Empty);
                fParameterValue.SetValue(string.Empty);
            }
            writer.AddDocument(doc);
        }

        string ConvertIPToString(byte[] data)
        {
            string ret = string.Concat(
                data[0].ToString("000"),
                data[1].ToString("000"),
                data[2].ToString("000"),
                data[3].ToString("000"));
            return ret;
        }

        public void Dispose()
        {
            if (writer != null)
            {
                writer.Dispose();
            }
            if (directory != null)
            {
                directory.Dispose();
            }
            if (db != null)
            {
                db.Dispose();
            }
        }
    }
}
