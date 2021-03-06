﻿using System;
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
using NLog;

namespace NAppProfiler.Server.Index
{
    public class NAppIndexUpdater : IDisposable
    {
        private static Logger nLogger;

        private readonly string indexFullPath;
        private readonly Directory directory;
        private readonly Database currentDb;
        private IndexWriter writer;

        private NumericField fLogID;
        private Field fLogName;
        private Field fSvc;
        private Field fMethod;
        private Field fClientIP;
        private Field fServerIP;
        private NumericField fException;
        private NumericField fCreated;
        private NumericField fElapsed;
        private Field fDetail_Desc;
        private Field fDetail_Parm;
        private NumericField fDetail_Elapsed;

        static NAppIndexUpdater()
        {
            nLogger = LogManager.GetCurrentClassLogger();
        }

        public NAppIndexUpdater(Configuration.ConfigManager config, Database currentDb)
        {
            indexFullPath = config.GetSetting(SettingKeys.Index_Directory, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index"));
            indexFullPath = System.IO.Path.GetFullPath(indexFullPath);
            directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexFullPath));
            this.currentDb = currentDb;
        }

        public int UpdateBatchSize
        {
            get { return 200; }
        }

        private void InitializeDocumentCache()
        {
            fLogID = new NumericField(FieldKeys.LogID, 8, Field.Store.YES, true);
            fLogName = new Field(FieldKeys.LogName, string.Empty, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS);
            fSvc = new Field(FieldKeys.Service, string.Empty, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS);
            fMethod = new Field(FieldKeys.Method, string.Empty, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS);
            fClientIP = new Field(FieldKeys.ClientIP, string.Empty, Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS);
            fServerIP = new Field(FieldKeys.ServerIP, string.Empty, Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS);
            fException = new NumericField(FieldKeys.Exception, 1, Field.Store.NO, true);
            fCreated = new NumericField(FieldKeys.CreatedDT, 8, Field.Store.NO, true);
            fElapsed = new NumericField(FieldKeys.Elapsed, 8, Field.Store.NO, true);
            fDetail_Desc = new Field(FieldKeys.Detail_Desc, string.Empty, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS);
            fDetail_Parm = new Field(FieldKeys.Detail_Parm, string.Empty, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS);
            fDetail_Elapsed = new NumericField(FieldKeys.Detail_Elapsed, 8, Field.Store.NO, true);
        }

        public void Initialize()
        {
            SetIndexWriter();
            InitializeDocumentCache();
        }

        public long UpdateIndex()
        {
            long ret = 0;
            var logIds = currentDb.GetLogsToIndex(this.UpdateBatchSize);
            if (logIds.Count > 0)
            {
                var search = new LogQueryResults(){ IncludeData = true};
                search.LogIDs = new List<LogQueryResultDetail>(logIds.Count);
                var idArray = new long[logIds.Count];
                for (int i = 0; i < logIds.Count; i++)
                {
                    idArray[i] = logIds[i].Item1;
                    search.LogIDs.Add(new LogQueryResultDetail() { ID = logIds[i].Item2 });
                }
                currentDb.RetrieveLogsBySearchResults(search);
                foreach (var log in search.LogIDs)
                {
                    AddDocumentToIndex(log.ID, log.Log);
                    if (nLogger.IsTraceEnabled)
                    {
                        nLogger.Trace("Index updated with log id {0}", log.ID);
                    }
                    ret++;
                }
                currentDb.DeleteIndexRows(idArray);
                writer.Commit();
            }
            return ret;
        }

        public long RebuildIndex()
        {
            long ret = 0;
            SetIndexWriter(true);
            currentDb.AddAllLogsToReindex();
            ret = UpdateIndex();
            return ret;
        }

        void SetIndexWriter(bool reWrite = false)
        {
            if (writer != null)
            {
                writer.Commit();
                writer.Dispose();
            }
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            if (reWrite)
            {
                writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            }
            else
            {
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            }
            writer.SetRAMBufferSizeMB(48);
            writer.SetMergeFactor(100);
        }

        void AddDocumentToIndex(long id, Log log)
        {
            var doc = new Document();
            fLogID.SetLongValue(id);
            doc.Add(fLogID);
            fLogName.SetValue("");  // TODO: Add Log Name
            doc.Add(fLogName);
            fSvc.SetValue(string.IsNullOrWhiteSpace(log.Service) ? string.Empty : log.Service);
            doc.Add(fSvc);
            fMethod.SetValue(string.IsNullOrWhiteSpace(log.Method) ? string.Empty : log.Method);
            doc.Add(fMethod);
            fClientIP.SetValue(ConvertIPToString(log.ClientIP));
            doc.Add(fClientIP);
            fServerIP.SetValue(ConvertIPToString(log.ServerIP));
            doc.Add(fServerIP);
            fException.SetIntValue(Convert.ToInt32(log.IsError));
            doc.Add(fException);
            fCreated.SetLongValue(log.CreatedDateTime.Ticks);
            doc.Add(fCreated);
            fElapsed.SetLongValue(log.Elapsed);
            doc.Add(fElapsed);

            if (log.Details != null && log.Details.Count > 0)
            {
                for (int x = 0; x < log.Details.Count; x++)
                {
                    var curDtl = log.Details[x];
                    if (curDtl.Parameters != null)
                    {
                        for (int y = 0; y < curDtl.Parameters.Count; y++)
                        {
                            fDetail_Parm.SetValue(curDtl.Parameters[y].Value);
                            doc.Add(fDetail_Parm);
                        }
                    }
                    fDetail_Desc.SetValue(curDtl.Description);
                    doc.Add(fDetail_Desc);

                    fDetail_Elapsed.SetLongValue(curDtl.Elapsed);
                    doc.Add(fDetail_Elapsed);
                }
            }
            writer.AddDocument(doc);
        }

        internal static string ConvertIPToString(byte[] data)
        {
            var ret = string.Empty;
            if (data == null)
            {
                return "000" + "000" + "000" + "000";
            }
            if (data.Length == 4)
            {
                ret = string.Concat(
                    data[0].ToString("000"),
                    data[1].ToString("000"),
                    data[2].ToString("000"),
                    data[3].ToString("000"));
            }
            else if (data.Length == 16)
            {
                ret = string.Concat(
                    data[0].ToString("000"),
                    data[1].ToString("000"),
                    data[2].ToString("000"),
                    data[3].ToString("000"),
                    data[4].ToString("000"),
                    data[5].ToString("000"),
                    data[6].ToString("000"),
                    data[7].ToString("000"),
                    data[8].ToString("000"),
                    data[9].ToString("000"),
                    data[10].ToString("000"),
                    data[11].ToString("000"),
                    data[12].ToString("000"),
                    data[13].ToString("000"),
                    data[14].ToString("000"),
                    data[15].ToString("000"));
            }
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("000"));
                }
                ret = sb.ToString();
            }
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
        }
    }
}
