using System;
using LcmsNetDataClasses.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LcmsNetDmsTools;
using LcmsNetDataClasses;

namespace Buzzard.Data
{    
    /// <summary>
    /// Manages a list of datasets
    /// </summary>
    public class classDatasetManager
    {
        #region Events
        /// <summary>
        /// Fired when a dataset trigger file is failed to be made.
        /// </summary>
        public event EventHandler<classDatasetQueueEventArgs> DatasetFailed;
        /// <summary>
        /// Fired when a dataset trigger file is made.
        /// </summary>
        public event EventHandler<classDatasetQueueEventArgs> DatasetSent;
        /// <summary>
        /// Fired when a dataset trigger file is pending.
        /// </summary>
        public event EventHandler<classDatasetQueueEventArgs> DatasetPending;
        /// <summary>
        /// Fired when a dataset is cleared.
        /// </summary>
        public event EventHandler<classDatasetQueueEventArgs> DatasetCleared;
        public event EventHandler<classDatasetQueueEventArgs> DatasetFailedCleared; 
        /// <summary>
        /// Fired when datasets are loaded.
        /// </summary>
        public event EventHandler DatasetsLoaded;
        #endregion

        #region Members
        /// <summary>
        /// thread for loading data from DMS.
        /// </summary>
        private Thread mobj_dmsLoadThread;
        /// <summary>
        /// Trie that holds dataset names from DMS.
        /// </summary>        
        private classDatasetTrie mobj_datasetTrie;
        /// <summary>
        /// Flag indicating when true, that the dataset names have been loaded from DMS.
        /// </summary>
        private bool mbool_datasetsReady;
        /// <summary>
        /// Synchs data saving between the DMS database load thread and the rest of the data management.
        /// </summary>
        private object mobj_synchObject;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public classDatasetManager()
        {
            this.Pending        = new List<classDataset>();
            this.Failed         = new List<classDataset>();
            this.Sent           = new List<classDataset>();
            mobj_datasetTrie    = new classDatasetTrie();
            mobj_synchObject    = new object();
            mbool_datasetsReady = false;
        }

        public void ClearCompleted()
        {
            if (DatasetCleared == null)
                return;

            foreach (classDataset dataset in Sent)
            {
                DatasetCleared(this, 
                                    new classDatasetQueueEventArgs(dataset,
                                        DatasetQueueType.Sent, 
                                            DatasetQueueType.Sent));
            }
        }

        public void ClearFailed()
        {
            if (DatasetFailedCleared == null)
                return;

            foreach (classDataset dataset in Sent)
            {
                DatasetFailedCleared(this, 
                                    new classDatasetQueueEventArgs(dataset, 
                                        DatasetQueueType.Failed,
                                            DatasetQueueType.Failed));
            }
        }


        #region Loading Data
        /// <summary>
        /// Loads datasets
        /// </summary>
        /// <param name="path"></param>
        public void LoadDatasets(string path, 
                                 string pattern,
                                 TimeSpan span,
                                 SearchOption option)
        {
            string[] paths = Directory.GetFiles(path, pattern, option);
            foreach (string filePath in paths)
            {
                FileInfo info = new FileInfo(filePath);
                CreateDataset(path, span, info.LastWriteTime);
            }            
        }
        /// <summary>
        /// Abort for the Dms Thread.
        /// </summary>
        private void AbortDmsThread()
        {
            try
            {
                mobj_dmsLoadThread.Abort();
            }
            catch
            {
                // who cares.
            }
            finally
            {
                try
                {
                    mobj_dmsLoadThread.Join(100);
                }
                catch
                {
                }
            }
            mobj_dmsLoadThread = null;
        }
        /// <summary>
        /// Loads the DMS Data Cache
        /// </summary>
        public void LoadDMSCache()
        {
            if (mobj_dmsLoadThread != null)
            {
                AbortDmsThread();
            }

            // Create a new threaded load.
            ThreadStart start  = new ThreadStart(LoadThread);
            mobj_dmsLoadThread = new Thread(start);
            mobj_dmsLoadThread.Start();

            mbool_datasetsReady = false;
        }

        private void LoadThread()
        {
            classSampleQueryData query  = new classSampleQueryData();
            query.UnassignedOnly        = false;
            query.RequestName           = "";
            query.Cart                  = "";
            query.BatchID               = "";
            query.Block                 = "";
            query.MaxRequestNum         = "100000000";
            query.MinRequestNum         = "0";
            query.Wellplate             = "";
            
            string queryString                          = query.BuildSqlString();
            List<classSampleData> samples               = LcmsNetDmsTools.classDBTools.GetSamplesFromDMS(query);            
            mobj_datasetTrie.Clear();

            foreach (classSampleData sample in samples)
            {              
                mobj_datasetTrie.AddData(sample.DmsData);
                if (sample.DmsData.RequestName == "SysVirol_SCL012_icSARS-DORF6_0h_3_Met")
                {
                    int xx = 90;
                    xx++;
                }
            }

            /// Use an interlocked atomic operation for this flag.
            mbool_datasetsReady = true;

            if (DatasetsLoaded != null)
            {
                DatasetsLoaded(this, null);
            }
        }
        #endregion

        #region Trigger Files

        public classInstrumentInfo InstrumentData
        {
            get;
            set;
        }

        private bool CreateTriggerFile(classDataset dataset)
        {
            bool worked = false;

            if (dataset.ShouldIgnore)
            {
                dataset.DatasetStatus = DatasetStatus.Ignored;
                return worked;
            }

            if (dataset.DMSData == null)
            {
                dataset.DatasetStatus = DatasetStatus.FailedNoDMSRequest;
                return worked;
            }
            classSampleData sample      = new classSampleData();
            sample.DmsData              = dataset.DMSData;
            sample.LCMethod             = new LcmsNetDataClasses.Method.classLCMethod();

            FileInfo info               = new FileInfo(dataset.DatasetPath);
            sample.LCMethod.ActualStart = info.CreationTime;
            sample.LCMethod.SetStartTime(info.CreationTime);
            sample.LCMethod.ActualEnd   = info.LastWriteTime;
            sample.DmsData.DatasetName  = dataset.DMSData.DatasetName;                        
            sample.ColumnData           = new LcmsNetDataClasses.Configuration.classColumnData();
            sample.ColumnData.Name      = classLCMSSettings.GetParameter("ColumnData");
            sample.InstrumentData       = InstrumentData;


            try
            {
                classTriggerFileTools.GenerateTriggerFile(sample);
                worked = true;
            }
            catch (DirectoryNotFoundException)
            {
                dataset.DatasetStatus = DatasetStatus.FailedFileError;                
            }
            catch (Exception)
            {
                dataset.DatasetStatus = DatasetStatus.FailedUnknown;                
            }

            return worked;
        }
        #endregion

        #region DMS Resolving
        /// <summary>
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        /// <param name="datasets"></param>
        private void ResolveDMS(List<classDataset> datasets)
        {
            foreach (classDataset dataset in datasets)
            {
                ResolveDMS(dataset);
            }
        }
        /// <summary>
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        /// <param name="datasets"></param>
        private void ResolveDMS(classDataset dataset)
        {
            bool worked = false;
            try
            {
                string path       = Path.GetFileNameWithoutExtension(dataset.DatasetPath);
                classDMSData data = null;
                try
                {
                    data = mobj_datasetTrie.FindData(path);
                }
                catch (KeyNotFoundException)
                {
                    // Now get the path name of the directory, then use that as the "search string for dms"
                    path = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(dataset.DatasetPath));
                    data = mobj_datasetTrie.FindData(path);
                }
                dataset.DMSData   = data;
            }
            catch (KeyNotFoundException ex)
            {
                int x = 90;
                x++;
            }
            catch (Exception ex)
            {
                
            }
        }
        #endregion

        #region Managing Queues
        /// <summary>
        /// Creates a new datasets based on the file path.
        /// </summary>
        /// <param name="path"></param>
        public void CreateDataset(string path, 
                                  TimeSpan duration,
                                  DateTime lastWrite
                                 )
        {
            classDataset dataset  = new classDataset();
            dataset.DatasetPath   = path;
            dataset.DatasetStatus = DatasetStatus.Pending;
            dataset.DMSData       = null;
            dataset.Duration      = duration;
            dataset.ShouldIgnore  = false;
            dataset.LastWrite     = lastWrite;
            dataset.Name          = Path.GetFileNameWithoutExtension(path);

            Pending.Add(dataset);

            if (DatasetPending != null)
            {
                DatasetPending(this,
                    new classDatasetQueueEventArgs( dataset, 
                                                    DatasetQueueType.Pending, 
                                                    DatasetQueueType.Pending));
            }
        }
        /// <summary>
        /// Updates the queue for creating trigger files etc.
        /// </summary>
        private void UpdatePending()
        {                        
            // Resolve those datasets in DMS
            List<classDataset> datasets  = Pending.FindAll(delegate(classDataset dataset)
            {
                return dataset.DMSData == null;
            }
            );
            ResolveDMS(datasets);

            // Determine which datasets are past due
            datasets = Pending.FindAll(delegate(classDataset dataset)
            {
                return dataset.LastWrite.Add(dataset.Duration).CompareTo(DateTime.Now) <= 0;
            }
            );

            // Now iterate around the datasets that are past due to create trigger files
            // or notify the UI to move to the right spot in the user interface.
            foreach (classDataset dataset in datasets)
            {
                bool worked = false;
                // If the DMS data is not null, then we have a trigger file to create.
                if (dataset.DMSData != null)
                {
                    worked = CreateTriggerFile(dataset);                    
                }
                else
                {
                    // Otherwise the dataset could not be resolved.
                    dataset.DatasetStatus = DatasetStatus.FailedNoDMSRequest;
                }
                
                if (worked)
                {
                    dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                    Sent.Add(dataset);
                    if (DatasetSent != null)
                    {
                        DatasetSent(this,
                            new classDatasetQueueEventArgs(dataset,
                                                            DatasetQueueType.Pending,
                                                            DatasetQueueType.Sent));
                    }
                }
                else
                {
                    Failed.Add(dataset);
                    if (DatasetFailed != null)
                    {
                        DatasetFailed(this,
                            new classDatasetQueueEventArgs(dataset,
                                                            DatasetQueueType.Pending,
                                                            DatasetQueueType.Failed));
                    }
                }
                Pending.Remove(dataset);
            }


            // Update the remaining datasets that were not removed from the pending queue.
            foreach (classDataset dataset in Pending)
            {
                if (File.Exists(dataset.DatasetPath))
                {
                    FileInfo info         = new FileInfo(dataset.DatasetPath);
                    dataset.LastWrite     = info.LastWriteTime;
                    dataset.DatasetStatus = DatasetStatus.Pending;
                }
            }

            if (mbool_datasetsReady)
            {
                // Determine which datasets that need DMS data.
                datasets = Pending.FindAll(delegate(classDataset dataset)
                        {
                            return dataset.DMSData == null;
                        }
                );            
                ResolveDMS(datasets);
            }
        }
        /// <summary>
        /// Updates the failed queue if the datasets could not be resolved by DMS search.
        /// </summary>
        private void UpdateFailed()
        {
            // Determine which datasets are past due
            List<classDataset> datasets = Failed.FindAll(delegate(classDataset dataset)
                        {
                            return dataset.LastWrite.Add(
                                            dataset.Duration).CompareTo(DateTime.Now) <= 0;
                        }
            );

            // Now iterate around the datasets that are past due to create trigger files
            // or notify the UI to move to the right spot in the user interface.
            foreach (classDataset dataset in datasets)
            {
                bool worked = CreateTriggerFile(dataset);
                if (worked)
                {
                    dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                    Sent.Add(dataset);
                    if (DatasetSent != null)
                    {
                        DatasetSent(this,
                            new classDatasetQueueEventArgs(dataset,
                                                            DatasetQueueType.Failed,
                                                            DatasetQueueType.Sent));
                    }
                    Failed.Remove(dataset);
                }                
            }
            if (mbool_datasetsReady)
            {
                // Determine which datasets that need DMS data.
                datasets = Failed.FindAll(delegate(classDataset dataset)
                {
                    return dataset.DMSData == null;
                }
                );
                ResolveDMS(datasets);
            }
        }
        /// <summary>
        /// Updates all of the pending datasets.
        /// </summary>
        public void UpdateAllDatasets()
        {
            UpdatePending();
            UpdateFailed();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the pending queue.
        /// </summary>
        public List<classDataset> Pending
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets or sets the failed datasets.
        /// </summary>
        public List<classDataset> Failed
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets or sets the sent datasets.
        /// </summary>
        public List<classDataset> Sent
        {
            get;
            private set;
        }
        #endregion
    }
}
