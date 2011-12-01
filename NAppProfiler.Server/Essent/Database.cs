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
        private JET_INSTANCE instance;
        private Session session;
        private JET_DBID dbid;
        private bool disposed;

        public string DatabaseFullPath { get { return databaseFullPath; } }

        public Database(ConfigManager config)
        {
            this.config = config;
            databaseDirectory = GetDatabaseDirectory();
            databaseFullPath = Path.Combine(databaseDirectory, "NAppProfiler.edb");
            this.disposeLock = new object();
            this.tblSchema = new LogTableSchema();
        }

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
        }

        void CreateDatabase()
        {
            using (var sid = new Session(instance))
            {
                Api.JetCreateDatabase(sid, databaseFullPath, null, out dbid, CreateDatabaseGrbit.None);
                tblSchema.Create(sid, dbid);
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
            var cacheSizeMaxMB = 0;
            if (!int.TryParse(config.GetSetting(SettingKeys.Database_CacheSizeMax), out cacheSizeMaxMB))
            {
                cacheSizeMaxMB = 1024;
            }
            SystemParameters.CacheSizeMax = ((cacheSizeMaxMB * 1024) / SystemParameters.DatabasePageSize) * 1024;
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
            instanceParms.LogFileDirectory = Path.Combine(databaseDirectory, "logs");
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
    }
}
