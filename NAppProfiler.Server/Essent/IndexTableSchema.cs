using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace NAppProfiler.Server.Essent
{
    class IndexTableSchema : IDisposable
    {
        private const string tableName = "Index";
        private const string colName_ID = "id";
        private const string colName_LogID = "logid";
        private const string idxName_Primary = "idxprimary";

        private Table indexTable;
        private JET_COLUMNID colID_ID;
        private JET_COLUMNID colID_LogID;

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

                Api.JetAddColumn(session, tblID, colName_LogID, new JET_COLUMNDEF()
                {
                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL,
                }, null, 0, out c);

                var indexDef = "+" + colName_ID + "\0\0";
                Api.JetCreateIndex(session, tblID, idxName_Primary, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 80);

                tran.Commit(CommitTransactionGrbit.None);
            }
        }

        public void InitializeColumnIDS(JET_SESID session, JET_DBID dbid)
        {
            indexTable = new Table(session, dbid, tableName, OpenTableGrbit.None);
            var columnsIDs = Api.GetColumnDictionary(session, indexTable);
            colID_ID = columnsIDs[colName_ID];
            colID_LogID = columnsIDs[colName_LogID];
        }

        public void Dispose()
        {
            if (indexTable != null)
            {
                indexTable.Dispose();
            }
        }

        public void InsertIndexRow(JET_SESID session, Transaction tran, long logID)
        {
            using (var updt = new Update(session, indexTable, JET_prep.Insert))
            {
                Api.SetColumn(session, indexTable, colID_LogID, logID);
                updt.Save();
            }
        }
    }
}
