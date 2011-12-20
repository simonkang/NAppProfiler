using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.ConstrainedExecution;
using Microsoft.Isam.Esent.Interop;
using NAppProfiler.Server.Configuration;

namespace NAppProfiler.Server.Essent
{
    public class Database : CriticalFinalizerObject, IDisposable
    {
        private readonly Configuration.ConfigManager config;
        private readonly string databaseFullPath;
        private readonly string databaseDirectory;
        private readonly object disposeLock;
        private readonly LogTableSchema tblSchema;
        private readonly IndexTableSchema idxSchema;
        private JET_INSTANCE instance;
        private Session session;
        private JET_DBID dbid;
        private bool disposed;

        public Database(ConfigManager config, string directory = "")
        {
            this.config = config;
            databaseDirectory = Path.GetFullPath(GetDatabaseDirectory());
            if (string.IsNullOrWhiteSpace(directory))
            {
                databaseDirectory = Path.Combine(databaseDirectory, "current");
            }
            else
            {
                databaseDirectory = Path.Combine(databaseDirectory, directory);
            }
            databaseFullPath = Path.Combine(databaseDirectory, "NAppProfiler.edb");
            this.disposeLock = new object();
            this.tblSchema = new LogTableSchema();
            this.idxSchema = new IndexTableSchema();
        }

        public string DatabaseFullPath { get { return databaseFullPath; } }

        public void InitializeDatabase()
        {
            InitializeInstance();
            if (!File.Exists(databaseFullPath))
            {
                CreateDatabase();
            }
            session = new Session(instance);
            var ret = Api.JetAttachDatabase(session, databaseFullPath, AttachDatabaseGrbit.None);
            ret = Api.JetOpenDatabase(session, databaseFullPath, null, out dbid, OpenDatabaseGrbit.None);
            tblSchema.InitializeColumnIDS(session, dbid);
            idxSchema.InitializeColumnIDS(session, dbid);
        }

        void CreateDatabase()
        {
            using (var sid = new Session(instance))
            {
                Api.JetCreateDatabase(sid, databaseFullPath, null, out dbid, CreateDatabaseGrbit.None);
                tblSchema.Create(sid, dbid);
                idxSchema.Create(sid, dbid);
                Api.JetCloseDatabase(sid, dbid, CloseDatabaseGrbit.None);
            }
        }

        void InitializeInstance()
        {
            SetSystemParameters();
            Api.JetCreateInstance2(out instance, Path.GetFileName(databaseFullPath), Path.GetFileName(databaseFullPath), CreateInstanceGrbit.None);
            if (instance == JET_INSTANCE.Nil)
            {
                throw new ApplicationException("JetCreateInstance2 Failed");
            }
            SetInstanceParameters();
            Api.JetInit(ref instance);
        }

        void SetSystemParameters()
        {
            var cacheSizeMB = 0;
            if (!int.TryParse(config.GetSetting(SettingKeys.Database_CacheSizeMax), out cacheSizeMB))
            {
                cacheSizeMB = 1024;
            }
            SystemParameters.CacheSizeMax = ((cacheSizeMB * 1024) / SystemParameters.DatabasePageSize) * 1024;

            if (!int.TryParse(config.GetSetting(SettingKeys.Database_CacheSizeMin), out cacheSizeMB))
            {
                cacheSizeMB = 512;
            }
            SystemParameters.CacheSizeMin = ((cacheSizeMB * 1024) / SystemParameters.DatabasePageSize) * 1024;
        }

        void SetInstanceParameters()
        {
            var instanceParms = new InstanceParameters(instance);
            instanceParms.CircularLog = true;
            instanceParms.Recovery = true;
            instanceParms.NoInformationEvent = false;
            instanceParms.CreatePathIfNotExist = true;
            instanceParms.TempDirectory = Path.Combine(databaseDirectory, "temp");
            instanceParms.SystemDirectory = Path.Combine(databaseDirectory, "system");
            instanceParms.LogFileDirectory = Path.Combine(databaseDirectory, config.GetSetting(SettingKeys.Database_LogDirectory, "logs"));
            instanceParms.CheckpointDepthMax = 100 * 1024 * 1024;
        }

        public void Dispose()
        {
            lock (disposeLock)
            {
                if (disposed)
                    return;
                disposed = true;
                GC.SuppressFinalize(this);
                if (tblSchema != null)
                {
                    tblSchema.Dispose();
                }
                if (idxSchema != null)
                {
                    idxSchema.Dispose();
                }
                if (session != null)
                {
                    session.Dispose();
                }
                Api.JetTerm2(instance, TermGrbit.Complete);
            }
        }

        public string GetDatabaseDirectory()
        {
            if (string.IsNullOrWhiteSpace(databaseDirectory))
            {
                var dbDirectory = config.GetSetting(SettingKeys.Database_Directory);
                if (string.IsNullOrWhiteSpace(dbDirectory))
                {
                    dbDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB");
                }
                return dbDirectory;
            }
            else
            {
                return databaseDirectory;
            }
        }

        //TODO: Use LogEntity for Testing - Need to change to BSON object in Client Library
        public long? InsertLog(DateTime createdDateTime, long elapsed, byte[] data)
        {
            return tblSchema.InsertLog(session, idxSchema, createdDateTime, elapsed, data);
        }

        public IList<LogEntity> RetrieveLogByIDs(params long[] ids)
        {
            return tblSchema.RetrieveLogByIDs(session, ids);
        }

        public long Count(DateTime from, DateTime to)
        {
            return tblSchema.Count(session, from, to);
        }

        public long Count()
        {
            return tblSchema.CountAll(session);
        }

        public long Size()
        {
            int sizePages;
            Api.JetGetDatabaseInfo(session, dbid, out sizePages, JET_DbInfo.SpaceOwned);
            long sizeBytes = ((long)sizePages) * SystemParameters.DatabasePageSize;
            return sizeBytes;
        }

        public IList<Tuple<long, long>> GetLogsToIndex(int count)
        {
            return idxSchema.GetLogsToIndex(session, count);
        }

        public void DeleteIndexRows(params long[] ids)
        {
            using (var tran = new Transaction(session))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    idxSchema.DeleteIndexRow(session, tran, ids[i]);
                }
                tran.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }

        public void AddAllLogsToReindex()
        {
            var curData = GetLogsToIndex(50);
            while (curData.Count > 0)
            {
                var deleteIdx = curData.Select(i => i.Item1).ToArray();
                DeleteIndexRows(deleteIdx);
                curData = GetLogsToIndex(50);
            }
            tblSchema.ReAddAllLogsToIndex(session, idxSchema);
        }
    }
}
