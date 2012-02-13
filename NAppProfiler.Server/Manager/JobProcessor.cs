using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NAppProfiler.Server.Configuration;
using NAppProfiler.Server.Essent;
using NAppProfiler.Server.Index;
using System.Threading.Tasks;

namespace NAppProfiler.Server.Manager
{
    public class JobProcessor
    {
        private static Logger nLogger;

        private readonly int processorQueueSize;
        private readonly Database currentDb;
        private readonly JobQueueManager manager;
        private readonly NAppIndexUpdater indexUpdater;
        private readonly NAppIndexReader indexReader;

        private int count;
        private long processCount;
        private int insertLogCount;
        private List<JobItem> insertLogItems;
        private int retrieveLogCount;
        private List<JobItem> retrieveLogItems;
        private List<JobItem> emptyLogItems;
        private bool updateIndex;
        private List<JobItem> queryRequestItems;

        static JobProcessor()
        {
            nLogger = LogManager.GetCurrentClassLogger();
        }

        public JobProcessor(ConfigManager config, Database currentDb, JobQueueManager manager, NAppIndexUpdater indexUpdater, NAppIndexReader indexReader)
        {
            this.currentDb = currentDb;
            this.manager = manager;
            this.indexReader = indexReader;
            this.indexUpdater = indexUpdater;
            if (!int.TryParse(config.GetSetting(SettingKeys.Manager_ProcessorQueueSize), out this.processorQueueSize))
            {
                this.processorQueueSize = 64;
            }
            insertLogItems = new List<JobItem>(processorQueueSize);
            retrieveLogItems = new List<JobItem>(processorQueueSize);
            emptyLogItems = new List<JobItem>(processorQueueSize);
        }

        public long ProcessCount
        {
            get { return processCount; }
        }

        public void Add(JobItem item)
        {
            count++;

            if (item.Method == JobMethods.Database_InsertLogs)
            {
                insertLogItems.Add(item);
                insertLogCount += item.LogEntityItems.Count;
            }
            else if (item.Method == JobMethods.Database_RetrieveLogs)
            {
                retrieveLogItems.Add(item);
                retrieveLogCount += item.LogIDs.Count;
            }
            else if (item.Method == JobMethods.Empty)
            {
                emptyLogItems.Add(item);
            }
            else if (item.Method == JobMethods.Database_UpdateIndex)
            {
                updateIndex = true;
                item.Processed = true;
            }
            else if (item.Method == JobMethods.Index_QueryRequest)
            {
                queryRequestItems.Add(item);
            }

            if (count >= processorQueueSize)
            {
                Flush(false);
            }
        }

        public void Flush(bool finalFlush)
        {
            if (insertLogCount > 0)
            {
                InsertLogs();
                updateIndex = true;
                //Task.Factory.StartNew(() => manager.AddJob(new JobItem(JobMethods.Database_UpdateIndex)));
            }
            if (retrieveLogCount > 0)
            {
                RetrieveLogs();
            }
            if (emptyLogItems.Count > 0)
            {
                for (int i = 0; i < emptyLogItems.Count; i++)
                {
                    processCount++;
                    emptyLogItems[i].Processed = true;
                }
                emptyLogItems.Clear();
            }
            if (updateIndex && finalFlush)
            {
                var ret = indexUpdater.UpdateIndex();
                updateIndex = false;
                if (ret >= indexUpdater.UpdateBatchSize)
                {
                    Task.Factory.StartNew(() => manager.AddJob(new JobItem(JobMethods.Database_UpdateIndex)));
                }
            }
            ProcessQueryRequests();

            count = 0;
        }

        private void InsertLogs()
        {
            var logArray = new List<LogEntity>(insertLogCount);
            for (int ji = 0; ji < insertLogItems.Count; ji++)
            {
                processCount++;
                insertLogItems[ji].Processed = true;
                var cur = insertLogItems[ji].LogEntityItems;
                for (int le = 0; le < cur.Count; le++)
                {
                    logArray.Add(cur[le]);
                }
            }
            try
            {
                currentDb.InsertLogs(logArray);
            }
            catch (Exception ex)
            {
                nLogger.ErrorException("Insert Logs Exception", ex);
            }
            finally
            {
                insertLogCount = 0;
                insertLogItems.Clear();
            }
        }

        private void RetrieveLogs()
        {
            var idArray = new List<long>(retrieveLogCount);
            for (int i = 0; i < retrieveLogItems.Count; i++)
            {
                processCount++;
                retrieveLogItems[i].Processed = true;
                var cur = retrieveLogItems[i].LogIDs;
                for (int lid = 0; lid < cur.Count; lid++)
                {
                    idArray.Add(cur[lid]);
                }
            }
            IList<LogEntity> ret = null;
            try
            {
                ret = currentDb.RetrieveLogByIDs(idArray);
            }
            catch (Exception ex)
            {
                nLogger.ErrorException("Retrieve Logs Exception", ex);
            }
            finally
            {
                retrieveLogCount = 0;
                retrieveLogItems.Clear();
            }
        }

        private void ProcessQueryRequests()
        {
            var processed = false;
            for (int i = 0; i < queryRequestItems.Count; i++)
            {
                var curQueries = queryRequestItems[i].LogQueries;
                for (int j = 0; j < curQueries.Count; j++)
                {
                    indexReader.Search(curQueries[j]);
                }
            }
            if (processed)
            {
                queryRequestItems.Clear();
            }
        }
    }
}
