using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace NAppProfiler.Server.Essent
{
    class LogTableSchema : IDisposable
    {
        private const string tableName = "Log";
        private const string colName_ID = "id";
        private const string colName_Created = "created";
        private const string colName_Data = "data";
        private const string idxName_Primary = "by_primary";
        private const string idxName_Created = "by_created";

        private Table logTable;
        private JET_COLUMNID colID_ID;
        private JET_COLUMNID colID_Created;
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
            colID_Data = columnIDs[colName_Data];
        }

        public void Dispose()
        {
            if (logTable != null)
            {
                logTable.Dispose();
            }
        }
    }
}
