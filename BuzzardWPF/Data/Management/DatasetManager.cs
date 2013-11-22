using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

using BuzzardWPF.LcmsNetTemp;

using LcmsNetDataClasses;
using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;

using LcmsNetDmsTools;
using BuzzardWPF.IO;


namespace BuzzardWPF.Data
{    
    /// <summary>
    /// Manages a list of datasets
    /// </summary>
    public class DatasetManager
    {
        #region Events
        /// <summary>
        /// Fired when datasets are loaded.
        /// </summary>
        public event EventHandler DatasetsLoaded;
        #endregion

        private const int CONST_KB = 1024;

        #region Attributes
        /// <summary>
        /// thread for loading data from DMS.
        /// </summary>
        private Thread m_dmsLoadThread;
        /// <summary>
        /// Trie that holds dataset names from DMS.
        /// </summary>        
        private DatasetTrie m_datasetTrie;
        /// <summary>
        /// Flag indicating when true, that the dataset names have been loaded from DMS.
        /// </summary>
        private bool m_datasetsReady;
        /// <summary>
        /// Synchs data saving between the DMS database load thread and the rest of the data management.
        /// </summary>
        private object m_synchObject;

		private string[] m_triggerFolderContents;


		private DispatcherTimer m_scannedDatasetTimer;


		private FileSystemWatcher m_fileSystemWatcher;

		#region Static
		private static readonly string[] INTEREST_RATING_ARRAY = { "Unreviewed", "Not Released", "Released", "Rerun (Good Data)", "Rerun (Superseded)" };
		public static readonly ObservableCollection<string>	INTEREST_RATINGS_COLLECTION;
		#endregion
		#endregion


		/// <summary>
        /// Constructor.
        /// </summary>
        private DatasetManager()
        {
            m_datasetTrie	= new DatasetTrie();
            m_synchObject   = new object();
            m_datasetsReady = false;
			Datasets		= new ObservableCollection<BuzzardDataset>();
			m_ticks			= 0;


			WatcherConfigSelectedCartName		= null;
			WatcherConfigSelectedColumnType		= null;
			WatcherConfigSelectedInstrument		= null;
			WatcherConfigSelectedOperator		= null;
			WatcherConfigSelectedSeparationType = null;
			TriggerFileCreationWaitTime			= 5;
            MinimumFileSize                     = 100;

			m_fileSystemWatcher = null;
        }

		static DatasetManager()
		{
			Manager = new DatasetManager();
			INTEREST_RATINGS_COLLECTION = new ObservableCollection<string>(INTEREST_RATING_ARRAY);
		}


        #region Loading Data
        /// <summary>
        /// Abort for the Dms Thread.
        /// </summary>
        private void AbortDmsThread()
        {
            try
            {
                m_dmsLoadThread.Abort();
            }
            catch
            {
                // who cares.
            }
            finally
            {
                try
                {
                    m_dmsLoadThread.Join(100);
                }
                catch
                {
                }
            }
            m_dmsLoadThread = null;
        }
        /// <summary>
        /// Loads the DMS Data Cache
        /// </summary>
        public void LoadDMSCache()
        {
            if (m_dmsLoadThread != null)
            {
                AbortDmsThread();
            }

            // Create a new threaded load.
            ThreadStart start  = new ThreadStart(LoadThread);
            m_dmsLoadThread = new Thread(start);
            m_dmsLoadThread.Start();

            m_datasetsReady = false;
        }

        private void LoadThread()
        {
            classSampleQueryData query  = new classSampleQueryData();
            query.UnassignedOnly        = true;
            query.RequestName           = "";
            query.Cart                  = "";
            query.BatchID               = "";
            query.Block                 = "";
            query.MaxRequestNum         = "1000000";
            query.MinRequestNum         = "0";
            query.Wellplate             = "";
            
            string queryString                          = query.BuildSqlString();
            List<classSampleData> samples               = LcmsNetDmsTools.classDBTools.GetSamplesFromDMS(query);
            SortedDictionary<string, classDMSData> data = new SortedDictionary<string, classDMSData>();

			lock (m_datasetTrie)
			{
				m_datasetTrie.Clear();

				foreach (classSampleData sample in samples)
				{
					m_datasetTrie.AddData(sample.DmsData);
				}
			}

			// We can use this to get an idea is any datasets already have
			// trigger files that were sent.
			string triggerFileDestination = classLCMSSettings.GetParameter("TriggerFileFolder");
			if (Directory.Exists(triggerFileDestination))
			{
				try
				{
					m_triggerFolderContents = Directory.GetFiles(triggerFileDestination);
					
					if (m_triggerFolderContents == null)
						m_triggerFolderContents = new string[0];
				}
				catch
				{
					m_triggerFolderContents = new string[0];
				}
			}
			else
			{
				m_triggerFolderContents = new string[0];
			}

            /// Use an interlocked atomic operation for this flag.
            m_datasetsReady = true;

            if (DatasetsLoaded != null)
            {
                DatasetsLoaded(this, null);
            }
        }
        #endregion


        #region Trigger Files
        public static void CreateTriggerFile(BuzzardDataset dataset, string folderPath)
        {
            if (dataset.ShouldIgnore)
            {
                dataset.DatasetStatus = DatasetStatus.Ignored;
                return;
            }

            classSampleData sample      = new classSampleData();
            sample.LCMethod             = new LcmsNetDataClasses.Method.classLCMethod();

            FileInfo info               = new FileInfo(dataset.FilePath);
            sample.LCMethod.ActualStart = info.CreationTime;
            sample.LCMethod.SetStartTime(info.CreationTime);
            sample.LCMethod.ActualEnd   = info.LastWriteTime;
            sample.DmsData.DatasetName  = dataset.DMSData.DatasetName;

            try
            {
                //TODO: Is there a force create another trigger file just in case....
                if (dataset.DatasetStatus != DatasetStatus.TriggerFileSent)
                {
                    classApplicationLogger.LogMessage(0, string.Format("Creating Trigger File: {0} for {1}", DateTime.Now, dataset.Name));
                    TriggerFileTools.GenerateTriggerFile(sample, dataset, dataset.DMSData, DatasetManager.Manager.TriggerFileLocation);
                    classApplicationLogger.LogMessage(0, string.Format("Saved Trigger File: {0} for {1}", DateTime.Now, dataset.Name));
                    dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                }
            }
            catch (DirectoryNotFoundException)
            {
                dataset.DatasetStatus = DatasetStatus.FailedFileError;                
            }
            catch (Exception)
            {
                dataset.DatasetStatus = DatasetStatus.FailedUnknown;                
            }
        }

		public string[] TriggerDirectoryContents
		{
			get { return m_triggerFolderContents; }
		}
        #endregion


        #region DMS Resolving
        /// <summary>
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        /// <param name="datasets"></param>
        public void ResolveDMS(IEnumerable<BuzzardDataset> datasets)
        {
			if (datasets == null)
				return;

            foreach (BuzzardDataset dataset in datasets)
            {
                ResolveDMS(dataset);
            }
        }

        /// <summary>
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        /// <param name="datasets"></param>
        public void ResolveDMS(BuzzardDataset dataset)
        {
			if (dataset == null)
				return;

            try
            {
                string path       = Path.GetFileNameWithoutExtension(dataset.FilePath);
                classDMSData data = null;
                try
                {
                    data = m_datasetTrie.FindData(path);
                }
                catch (KeyNotFoundException)
                {
                    // Now get the path name of the directory, then use that as the "search string for dms"
                    path = Path.GetFileName(Path.GetDirectoryName(dataset.FilePath));
                    data = m_datasetTrie.FindData(path);
                }
				
				dataset.DMSData = new DMSData(data, dataset.FilePath);
            }
            catch (KeyNotFoundException ex)
            {
				dataset.DatasetStatus = DatasetStatus.FailedNoDMSRequest;
            }
            catch (Exception ex)
            {
				dataset.DatasetStatus = DatasetStatus.FailedUnknown;
            }
        }
        #endregion


        #region Properties
		public string FileWatchRoot
		{
			get { return m_fileWatchRoot; }
			set
			{
				

				m_fileWatchRoot = value;

                
			}
		}
		private string m_fileWatchRoot;

		private Main m_mainWindow;
		public Main MainWindow
		{
			get { return m_mainWindow; }
			set
			{
				if (m_mainWindow != value)
				{
					m_mainWindow = value;
					SetupTimers();
				}
			}
		}

		private void SetupTimers()
		{
			if (MainWindow == null)
				return;

			m_scannedDatasetTimer = new DispatcherTimer(DispatcherPriority.Normal, MainWindow.Dispatcher);
			m_scannedDatasetTimer.Interval = new TimeSpan(0, 0, 0, 0, 333); // 0.333 seconds (about 1/3 of a second)
			m_scannedDatasetTimer.Tick += new EventHandler(ScannedDatasetTimer_Tick);
			m_scannedDatasetTimer.Start();
		}

		private long m_ticks;

		/// <summary>
		/// This will keep the UI components of the Datasets that are found by the scanner upto date.
		/// </summary>
		void ScannedDatasetTimer_Tick(object sender, EventArgs e)
		{
			// Find the datasets that have source data found by the
			// file watcher.
			var x = from BuzzardDataset ds in Datasets
					where ds.DatasetSource == DatasetSource.Watcher
					select ds;

			List<BuzzardDataset> datasets = new List<BuzzardDataset>(x);

			// If there aren't any, then we're done.
			if (datasets.Count == 0)
				return;

			DateTime now = DateTime.Now;
			TimeSpan timeToWait = new TimeSpan(0, TriggerFileCreationWaitTime, 0);

			int totalSecondsToWait = TriggerFileCreationWaitTime * 60;
			bool checkForFileMods = (m_ticks % 5 == 0);
			m_ticks++;

			foreach (var dataset in datasets)
			{
                
				if (dataset.DatasetStatus == DatasetStatus.TriggerFileSent)
					continue;
                
                if ((dataset.FileSize / CONST_KB) < MinimumFileSize)
                {
                    dataset.DatasetStatus = DatasetStatus.PendingFileSize;
                    dataset.UpdateFileProperties();
                    continue;
                }
                
				//if (checkForFileMods)
				//    ds.UpdateFileProperties();

				TimeSpan timeWaited = now - dataset.RunFinish;
				if (timeWaited >= timeToWait)
				{
					if (!dataset.DMSData.LockData)
					{
						ResolveDMS(dataset);
					}

					if (dataset.IsQC)
					{
						if (!this.QC_CreateTriggerOnDMSFail)
							continue;
					}
					else
					{
						if (!dataset.DMSData.LockData && !this.CreateTriggerOnDMSFail)
							continue;
					}

					CreateTriggerFile(dataset, DatasetManager.Manager.TriggerFileLocation);                    
				}
				else
				{
					// If it's not time to create the trigger file, then update
					// the display telling the user when it will be created.
					double secondsLeft = totalSecondsToWait - timeWaited.TotalSeconds;
					double percentWaited = 100 * timeWaited.TotalSeconds / totalSecondsToWait;
					int secondsLeftInt = Convert.ToInt32(secondsLeft);
					
					bool pluseIt = secondsLeftInt > dataset.SecondsTillTriggerCreation;
					
					dataset.SecondsTillTriggerCreation = Convert.ToInt32(secondsLeft);
					dataset.WaitTimePercentage = percentWaited;
					if (pluseIt)
					{
						dataset.PluseText = true;
						dataset.PluseText = false;
					}
				}
			}
		}


		#region Searcher Config
		/// <summary>
		/// This value tells the DatasetManager whether or not
		/// to create a dataset for an archived datasource that
		/// is found by the searcher.
		/// </summary>
		/// <remarks>
		/// the SearchConvfigView is responsible for setting this.
		/// </remarks>
		public bool IncludedArchivedItems { get; set; }
		#endregion


		#region Watcher Config
		/// <summary>
		/// This value tells the DatasetManager which LC Column to 
		/// use for datasets that were found by the File Watcher.
		/// </summary>
		/// <remarks>
		/// The Watcher Config control is responsible for setting this.
		/// </remarks>
		public string LCColumn { get; set; }

		/// <summary>
		/// This values tells the DatasetManager what name to use
		/// for datasets that were found by the File Watcher.
		/// </summary>
		/// <remarks>
		/// The Watcher Config control is responsible for setting this.
		/// </remarks>
		public string ExperimentName { get; set; }

		/// <summary>
		/// This values tells the DatasetManager if it can create
		/// a trigger file for datasets that fail to resulve their
		/// DMS data. This only applies when the reason for the
		/// trigger file creation is due to the count down running
		/// out. If a user wants to create the trigger file without 
		/// DMS data, we won't stop them.
		/// </summary>
		/// <remarks>
		/// The Watcher Config control is responsible for setting this.
		/// </remarks>
		public bool CreateTriggerOnDMSFail { get; set; }

		/// <summary>
		/// This is the amount of time that we should wait before
		/// creating a tigger file for a dataset that was scanned
		/// in.
		/// </summary>
		/// <remarks>
		/// This is measured in minutes.
		/// </remarks>
		/// <remarks>
		/// The Watcher control is responsible for setting this.
		/// </remarks>
		public int TriggerFileCreationWaitTime { get; set; }
        /// <summary>
        /// Gets or sets the minimum file size before starting the time.
        /// </summary>
        public int MinimumFileSize { get; set; }
		/// <summary>
		/// This item contains a copy of the SelectedInstrument value of
		/// the WatcherConfig tool.
		/// </summary>
		/// <remarks>
		/// WatcherConfig is responsible for setting this value.
		/// </remarks>
		public string WatcherConfigSelectedInstrument { get; set; }

		/// <summary>
		/// This item contains a copy of the SelectedCartName value of
		/// the WatcherConfig tool.
		/// </summary>
		/// <remarks>
		/// WatcherConfig is responsible for setting this value.
		/// </remarks>
		public string WatcherConfigSelectedCartName { get; set; }

		/// <summary>
		/// This item contains a copy of the SelectedSeperationType value of
		/// the WatcherConfig tool.
		/// </summary>
		/// <remarks>
		/// WatcherConfig is responsible for setting this value.
		/// </remarks>
		public string WatcherConfigSelectedSeparationType { get; set; }

		/// <summary>
		/// This item contains a copy of the SelectedColumnType value of
		/// the WatcherConfig tool.
		/// </summary>
		/// <remarks>
		/// WatcherConfig is responsible for setting this value.
		/// </remarks>
		public string WatcherConfigSelectedColumnType { get; set; }

		/// <summary>
		/// This item contains a copy of the SelectedOperator value of
		/// the WatcherConfig tool.
		/// </summary>
		/// <remarks>
		/// WatcherConfig is responsible for setting this value.
		/// </remarks>
		public string WatcherConfigSelectedOperator { get; set; }

		public string TriggerFileLocation { get; set; }

		public string Watcher_EMSL_Usage { get; set; }
		public string Watcher_EMSL_ProposalID { get; set; }
		public IEnumerable<classProposalUser> Watcher_SelectedProposalUsers { get; set; }
		#endregion


		#region Quality Control (QC)
		public string EMSL_Usage { get; set; }
		public string EMSL_ProposalID { get; set; }
		public string QC_ExperimentName { get; set; }
		public bool QC_CreateTriggerOnDMSFail { get; set; }

		public IEnumerable<classProposalUser> QC_SelectedProposalUsers { get; set; }
		#endregion

		public static DatasetManager Manager
		{
			get;
			private set;
		}

		public ObservableCollection<BuzzardDataset> Datasets
		{
			get;
			private set;
		}
        #endregion

        

		#region Dataset RunTime Updates
        //private void m_fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        //{
        //    // It's ok to update if a temp file is deleted, but if a file is deleted
        //    // because we're moving a dataset, then we don't update
        //    //BuzzardDataset datasetToUpdate = FindDataset(e.FullPath);
			
        //    //if (datasetToUpdate != null)
        //    //    datasetToUpdate.RunFinish = DateTime.Now;
        //}

        //private void m_fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        //{
        //    BuzzardDataset datasetToUpdate = FindDataset(e.FullPath);

        //    if (datasetToUpdate != null && !e.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
        //        datasetToUpdate.RunFinish = DateTime.Now;
        //}

        //private void m_fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        //{
        //    BuzzardDataset datasetToUpdate = FindDataset(e.FullPath);

        //    if (datasetToUpdate != null)
        //        datasetToUpdate.RunFinish = DateTime.Now;
        //}

		/// <summary>
		/// This method is used to find a dataset that a file corrisponds to.
		/// If a dataset is a directory type that contains this file, then that
		/// dataset will be returned. If a dataset is file type that is that
		/// file, then the dataset will be returned. If no dataset is found, then
		/// a null value will be returned.
		/// </summary>
		private BuzzardDataset FindDataset(string fullPath)
		{
			if (string.IsNullOrWhiteSpace(fullPath))
				return null;

			// If there's no file or directory, then we won't really find anything
			if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
				return null;

			fullPath = Path.GetFullPath(fullPath);
			BuzzardDataset result = null;
			string pathSep = Path.DirectorySeparatorChar.ToString();

			foreach (var dataset in this.Datasets)
			{
				if (dataset.DatasetSource == DatasetSource.Searcher)
					continue;

				string datasetPath = Path.GetFullPath(dataset.FilePath);

				if (fullPath.Equals(datasetPath, StringComparison.OrdinalIgnoreCase))
				{
					result = dataset;
					break;
				}
				else if (Directory.Exists(datasetPath))
				{
					if (!datasetPath.EndsWith(pathSep))
						datasetPath += pathSep;

					if (fullPath.StartsWith(datasetPath, StringComparison.OrdinalIgnoreCase))
					{
						result = dataset;
						break;
					}
				}
			}

			return result;
		}
		#endregion


		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="howWasItFound"></param>
		/// <remarks>
		/// This is not a thread safe method. In fact, if someone were to mess
		/// with the contents of the Datasets property while this method is
		/// executing, they could crash the program.
		/// </remarks>
		public void CreatePendingDataset(string path, DatasetSource howWasItFound = DatasetSource.Searcher)
		{
			/// If we're on the wrong thread, then put in 
			/// a call to this in the correct thread and exit.
			if (!MainWindow.Dispatcher.CheckAccess())
			{
				Action action = delegate
				{
					CreatePendingDataset(path, howWasItFound);
				};

				MainWindow.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
				return;
			}


			if (string.IsNullOrWhiteSpace(path))
			{
				classApplicationLogger.LogError(0, "No data path was given for the create new Dataset request.");
				return;
			}


			///
			/// Files that have been archived renamed to start with a "x_"
			/// 
			string fileName = Path.GetFileName(path);
			bool isArchived = fileName.StartsWith("x_", StringComparison.OrdinalIgnoreCase);
			string originalPath = path;
			
			if(isArchived)
				originalPath = Path.Combine(Path.GetDirectoryName(path), fileName.Substring(2));


			///
			/// Find if we need to create a new dataset.
			/// 
			bool isItAlreadyThere = Datasets.Any(
				datum => 
				{
					bool datasetFound = datum.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)
						|| datum.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase);

					return datasetFound;
				});


			BuzzardDataset dataset = null;

			if (isItAlreadyThere)
			{
				dataset = Datasets.First(
					datum => 
					{
						bool datasetFound = datum.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)
							|| datum.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase);
						
						return datasetFound; 
					});

				if (isArchived)
					dataset.FilePath = path;
				else
					dataset.UpdateFileProperties();
			}
			else if (
				!isItAlreadyThere							// If a new, archived datasource is found by the
				&& howWasItFound == DatasetSource.Searcher  // searcher, and the user set the search config
				&& isArchived								// not to include archived items, then we don't
				&& !IncludedArchivedItems)					// create a new dataset.
			{
				return;
			}
			else
			{
				dataset = DatasetFactory.LoadDataset(path);
			}


			///
			/// We don't really care if a dataset was found
			/// while doing a search, but we do care if it
			/// was picked up by the watcher. If the watcher
			/// pickes it up, then we might have to do a bit
			/// of a delay before creating the trigger file
			/// 
			if (howWasItFound == DatasetSource.Watcher)
			{
				dataset.DatasetSource = howWasItFound;


				// Since this dataset was picked up by the file scanner,
				// we want to fill this in with inforamtion that was set
				// in the watcher config tool. Yet, we don't want to 
				// overwrite something that was set by the user.
				if (string.IsNullOrWhiteSpace(dataset.Instrument))
					dataset.Instrument = this.WatcherConfigSelectedInstrument;

				if (string.IsNullOrWhiteSpace(dataset.DMSData.CartName))
					dataset.DMSData.CartName = this.WatcherConfigSelectedCartName;

				if (string.IsNullOrWhiteSpace(dataset.SeparationType))
					dataset.SeparationType = this.WatcherConfigSelectedSeparationType;

				if (string.IsNullOrWhiteSpace(dataset.DMSData.DatasetType))
					dataset.DMSData.DatasetType = this.WatcherConfigSelectedColumnType;

				if (string.IsNullOrWhiteSpace(dataset.Operator))
					dataset.Operator = this.WatcherConfigSelectedOperator;

				if (string.IsNullOrWhiteSpace(dataset.ExperimentName))
					dataset.ExperimentName = this.ExperimentName;

				if (string.IsNullOrWhiteSpace(dataset.LCColumn))
					dataset.LCColumn = this.LCColumn;


				string							emslUsageType		= null;
				string							emslProposalID		= null;
				IEnumerable<classProposalUser>	emslProposalUsers	= null;

				// QC data from the QC panel will override
				// any previous data for given properties
				if (dataset.IsQC)
				{
					dataset.ExperimentName = this.QC_ExperimentName;
					dataset.DMSData.Experiment = this.QC_ExperimentName;

					emslUsageType		= this.EMSL_Usage;
					emslProposalID		= this.EMSL_ProposalID;
					emslProposalUsers	= this.QC_SelectedProposalUsers;
				}
				else
				{
					emslUsageType		= this.Watcher_EMSL_Usage;
					emslProposalID		= this.Watcher_EMSL_ProposalID;
					emslProposalUsers	= this.Watcher_SelectedProposalUsers;
				}

				dataset.DMSData.EMSLUsageType = emslUsageType;
				if (!string.IsNullOrWhiteSpace(dataset.DMSData.EMSLUsageType) &&
					dataset.DMSData.EMSLUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
				{
					dataset.DMSData.EMSLProposalID = emslProposalID;
					dataset.EMSLProposalUsers = new ObservableCollection<classProposalUser>(emslProposalUsers);
				}
				else
				{
					dataset.DMSData.EMSLProposalID = null;
					dataset.EMSLProposalUsers = new ObservableCollection<classProposalUser>();
				}
			}


			///
			/// If it wasn't already in the set, then
			/// that means that this dataset is brand
			/// new.
			/// 
			if (!isItAlreadyThere)
			{
				if (howWasItFound == DatasetSource.Searcher)
				{
					ResolveDMS(dataset);

					bool hasTriggerFileSent =
						DatasetManager.Manager.TriggerDirectoryContents.Any
						(
							trig => { return trig.Contains(dataset.DMSData.DatasetName); }
						);

					if (hasTriggerFileSent)
						dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
				}

				Datasets.Add(dataset);

				classApplicationLogger.LogMessage(
					0,
					string.Format("Data source: '{0}' found.", path));
			}


            DatasetManager.Manager.ResolveDMS(dataset);
		}
    }
}
