﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using NAppProfiler.Client.DTO;

namespace NAppProfiler.Server.Essent
{
    class LogTableSchema : IDisposable
    {
        private const string tableName = "Log";
        private const string colName_ID = "id";
        private const string colName_Created = "created";
        private const string colName_Elapsed = "elapsed";
        private const string colName_Exception = "exception";
        private const string colName_Data = "data";
        private const string idxName_Primary = "idxprimary";
        private const string idxName_Created = "idxcreated";

        private Table logTable;
        private JET_COLUMNID colID_ID;
        private JET_COLUMNID colID_Created;
        private JET_COLUMNID colID_Elapsed;
        private JET_COLUMNID colID_Exception;
        private JET_COLUMNID colID_Data;

        public void Create(JET_SESID session, JET_DBID dbid)
        {
            using (var tran = new Transaction(session))
            {
                JET_TABLEID tblID;
                Api.JetCreateTable(session, dbid, tableName, 1, 80, out tblID);

                JET_COLUMNID c;
                Api.JetAddColumn(session, tblID, colName_ID, new JET_COLUMNDEF()
                {
                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnAutoincrement | ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL,
                }, null, 0, out c);

                Api.JetAddColumn(session, tblID, colName_Created, new JET_COLUMNDEF()
                {
                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL,
                }, null, 0, out c);

                Api.JetAddColumn(session, tblID, colName_Elapsed, new JET_COLUMNDEF()
                {
                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL,
                }, null, 0, out c);

                Api.JetAddColumn(session, tblID, colName_Exception, new JET_COLUMNDEF()
                {
                    coltyp = JET_coltyp.Bit,
                    grbit = ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL,
                }, null, 0, out c);

                Api.JetAddColumn(session, tblID, colName_Data, new JET_COLUMNDEF()
                {
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.ColumnTagged,
                }, null, 0, out c);

                var indexDef = "+" + colName_ID + "\0\0";
                Api.JetCreateIndex(session, tblID, idxName_Primary, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 80);

                indexDef = "+" + colName_Created + "\0\0";
                Api.JetCreateIndex(session, tblID, idxName_Created, CreateIndexGrbit.None, indexDef, indexDef.Length, 80);

                tran.Commit(CommitTransactionGrbit.None);
            }
        }

        public void InitializeColumnIDS(JET_SESID session, JET_DBID dbid)
        {
            logTable = new Table(session, dbid, tableName, OpenTableGrbit.None);
            var columnIDs = Api.GetColumnDictionary(session, logTable);
            colID_ID = columnIDs[colName_ID];
            colID_Created = columnIDs[colName_Created];
            colID_Elapsed = columnIDs[colName_Elapsed];
            colID_Exception = columnIDs[colName_Exception];
            colID_Data = columnIDs[colName_Data];
        }

        public void Dispose()
        {
            if (logTable != null)
            {
                logTable.Dispose();
            }
        }

        public long InsertLog(JET_SESID session, Transaction tran, IndexTableSchema idxSchema, LogEntity log) // DateTime createdDateTime, long elapsed, byte[] data)
        {
            long ret = 0;
            using (var updt = new Update(session, logTable, JET_prep.Insert))
            {
                ret = (long)Api.RetrieveColumnAsInt64(session, logTable, colID_ID, RetrieveColumnGrbit.RetrieveCopy);
                Api.SetColumn(session, logTable, colID_Created, log.CreatedDateTime.Ticks);
                Api.SetColumn(session, logTable, colID_Elapsed, log.Elapsed.Ticks);
                Api.SetColumn(session, logTable, colID_Exception, log.Exception);
                Api.SetColumn(session, logTable, colID_Data, log.Data);
                updt.Save();
            }
            idxSchema.InsertIndexRow(session, tran, ret);
            return ret;
        }

        public void RetrieveLogByIDs(JET_SESID session, params LogQueryResults[] results)
        {
            Api.JetSetCurrentIndex(session, logTable, idxName_Primary);
            for (int i = 0; i < results.Length; i++)
            {
                var curResult = results[i];
                if (curResult.LogIDs != null && curResult.LogIDs.Count > 0)
                {
                    for (int j = 0; j < curResult.LogIDs.Count; j++)
                    {
                        var curDetail = curResult.LogIDs[j];
                        Api.MakeKey(session, logTable, curDetail.ID, MakeKeyGrbit.NewKey);
                        if (Api.TrySeek(session, logTable, SeekGrbit.SeekEQ))
                        {
                            curDetail.CreatedDateTime = new DateTime((long)Api.RetrieveColumnAsInt64(session, logTable, colID_Created), DateTimeKind.Utc);
                            curDetail.Elapsed = (long)Api.RetrieveColumnAsInt64(session, logTable, colID_Elapsed);
                            curDetail.IsError = (bool)Api.RetrieveColumnAsBoolean(session, logTable, colID_Exception);
                            if (curResult.IncludeData)
                            {
                                curDetail.Log = Log.DeserializeLog(Api.RetrieveColumn(session, logTable, colID_Data));
                            }
                        }
                    }
                }
                else
                {
                    curResult.LogIDs = RetrieveLogByDate(session, curResult.DateTime_From, curResult.DateTime_To, curResult.IncludeData);
                }
            }
        }

        public IList<LogQueryResultDetail> RetrieveLogByDate(JET_SESID session, DateTime from, DateTime to, bool includeData)
        {
            var ret = new List<LogQueryResultDetail>();
            Api.JetSetCurrentIndex(session, logTable, idxName_Created);
            Api.MakeKey(session, logTable, from.Ticks, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(session, logTable, SeekGrbit.SeekGE))
            {
                Api.MakeKey(session, logTable, to.Ticks, MakeKeyGrbit.NewKey);
                if (Api.TrySetIndexRange(session, logTable, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit))
                {
                    do
                    {
                        var cur = new LogQueryResultDetail();
                        cur.ID = (long)Api.RetrieveColumnAsInt64(session, logTable, colID_ID);
                        cur.CreatedDateTime = new DateTime((long)Api.RetrieveColumnAsInt64(session, logTable, colID_Created), DateTimeKind.Utc);
                        cur.Elapsed = (long)Api.RetrieveColumnAsInt64(session, logTable, colID_Elapsed);
                        cur.IsError = (bool)Api.RetrieveColumnAsBoolean(session, logTable, colID_Exception);
                        if (includeData)
                        {
                            cur.Log = Log.DeserializeLog(Api.RetrieveColumn(session, logTable, colID_Data));
                        }
                        ret.Add(cur);
                    }
                    while (Api.TryMoveNext(session, logTable));
                }
            }
            return ret;
        }

        public long Count(JET_SESID session, DateTime from, DateTime to)
        {
            var ret = 0L;
            Api.JetSetCurrentIndex(session, logTable, idxName_Created);
            Api.MakeKey(session, logTable, from.Ticks, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(session, logTable, SeekGrbit.SeekGE))
            {
                Api.MakeKey(session, logTable, to.Ticks, MakeKeyGrbit.NewKey);
                if (Api.TrySetIndexRange(session, logTable, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit))
                {
                    do
                    {
                        ret++;
                    }
                    while (Api.TryMoveNext(session, logTable));
                }
            }
            return ret;
        }

        public long CountAll(JET_SESID session)
        {
            var ret = 0L;
            Api.MoveBeforeFirst(session, logTable);
            while (Api.TryMoveNext(session, logTable))
            {
                ret++;
            }
            return ret;
        }

        public long ReAddAllLogsToIndex(JET_SESID session, IndexTableSchema idxSchema)
        {
            var ret = 0L;
            Transaction tran = new Transaction(session);
            try
            {
                var tranCount = 0;
                Api.MoveBeforeFirst(session, logTable);
                while (Api.TryMoveNext(session, logTable))
                {
                    var curLogId = (long)Api.RetrieveColumnAsInt64(session, logTable, colID_ID);
                    idxSchema.InsertIndexRow(session, tran, curLogId);
                    tranCount++;
                    ret++;
                    if (tranCount >= 50)
                    {
                        tran.Commit(CommitTransactionGrbit.LazyFlush);
                        tran.Dispose();
                        tran = new Transaction(session);
                    }
                }
                tran.Commit(CommitTransactionGrbit.LazyFlush);
            }
            finally
            {
                tran.Dispose();
            }
            return ret;
        }
    }
}
