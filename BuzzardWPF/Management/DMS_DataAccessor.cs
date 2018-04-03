using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;
using LcmsNetDmsTools;
using LcmsNetSQLiteTools;

namespace BuzzardWPF.Management
{
    public class DMS_DataAccessor
        : INotifyPropertyChanged
    {

        #region Constants

        public const int RECENT_EXPERIMENT_MONTHS = 18;
        public const int RECENT_DATASET_MONTHS = 12;

        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Initialize

        /// <summary>
        /// Constructor
        /// </summary>
        private DMS_DataAccessor()
        {
            m_proposalUserCollections = new Dictionary<string, ObservableCollection<classProposalUser>>();
            LoadProposalUsers();

            InstrumentData = new ObservableCollection<string>();
            OperatorData = new ObservableCollection<string>();
            DatasetTypes = new ObservableCollection<string>();
            SeparationTypes = new ObservableCollection<string>();

            CartNames = new ObservableCollection<string>();
            CartConfigNames = new ObservableCollection<string>();
            ColumnData = new ObservableCollection<string>();

            Experiments = new List<classExperimentData>();
            Datasets = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);

            mLastSQLiteUpdate = DateTime.UtcNow;
            mLastLoadFromCache = DateTime.UtcNow.AddMinutes(-60);

            mDataRefreshIntervalHours = 6;

            mAutoUpdateTimer = new System.Timers.Timer
            {
                AutoReset = true,
                Enabled = false,
                Interval = 30 * 1000
            };

            mAutoUpdateTimer.Elapsed += mAutoUpdateTimer_Elapsed;
        }

        static DMS_DataAccessor()
        {
            Instance = new DMS_DataAccessor();
        }

        /// <summary>
        /// Loads the DMS data from the SQLite cache file
        /// </summary>
        /// <remarks>When forceLoad is false, will not re-load the data from the cache if it was last loaded in the last 60 seconds</remarks>
        public void LoadDMSDataFromCache(bool forceLoad = false)
        {
            if (DateTime.UtcNow.Subtract(mLastLoadFromCache).TotalMinutes < 1 && !forceLoad)
                return;

            mLastLoadFromCache = DateTime.UtcNow;

            //
            // Load Instrument Data
            //
            var tempInstrumentData = classSQLiteTools.GetInstrumentList(false);
            if (tempInstrumentData == null)
            {
                classApplicationLogger.LogError(0, "Instrument list retrieval returned null.");
                InstrumentData = new ObservableCollection<string>();
            }
            else
            {
                if (tempInstrumentData.Count == 0)
                    classApplicationLogger.LogError(0, "No instruments found.");
            }

            if (tempInstrumentData != null && tempInstrumentData.Count != 0)
            {
                InstrumentData = new ObservableCollection<string>(tempInstrumentData.Select(instDatum => instDatum.DMSName));

                var instrumentDetails = new Dictionary<string, classInstrumentInfo>();

                foreach (var instrument in tempInstrumentData)
                {
                    if (!instrumentDetails.ContainsKey(instrument.DMSName))
                    {
                        instrumentDetails.Add(instrument.DMSName, instrument);
                    }
                }

                InstrumentDetails = instrumentDetails;
            }

            //
            // Load Operator Data
            //
            var tempUserList = classSQLiteTools.GetUserList(false);
            if (tempUserList == null)
                classApplicationLogger.LogError(0, "User retrieval returned null.");
            else
                OperatorData = new ObservableCollection<string>(tempUserList.Select(userDatum => userDatum.UserName));

            //
            // Load Dataset Types
            //
            var tempDatasetTypesList = classSQLiteTools.GetDatasetTypeList(false);
            if (tempDatasetTypesList == null)
                classApplicationLogger.LogError(0, "Dataset Types retrieval returned null.");
            else
                DatasetTypes = new ObservableCollection<string>(tempDatasetTypesList);

            //
            // Load Separation Types
            //
            var tempSeparationTypesList = classSQLiteTools.GetSepTypeList(false);
            if (tempSeparationTypesList == null)
                classApplicationLogger.LogError(0, "Separation types retrieval returned null.");
            else
                SeparationTypes = new ObservableCollection<string>(tempSeparationTypesList);

            //
            // Load Cart Names
            //
            var tempCartsList = classSQLiteTools.GetCartNameList();
            if (tempCartsList == null)
                classApplicationLogger.LogError(0, "LC Cart names list retrieval returned null.");
            else
                CartNames = new ObservableCollection<string>(tempCartsList);

            //
            // Load Cart Config Names
            //
            var tempCartConfigNamesList = classSQLiteTools.GetCartConfigNameList(false);
            if (tempCartConfigNamesList == null)
                classApplicationLogger.LogError(0, "LC Cart config names list retrieval returned null.");
            else
                CartConfigNames = new ObservableCollection<string>(tempCartConfigNamesList);

            //
            // Load column data
            //
            var tempColumnData = classSQLiteTools.GetColumnList(false);
            if (tempColumnData == null)
                classApplicationLogger.LogError(0, "Column data list retrieval returned null.");
            else
            {
                ColumnData = new ObservableCollection<string>(tempColumnData);
            }

            //
            // Load Experiments
            //
            var experimentList = classSQLiteTools.GetExperimentList();
            if (experimentList == null)
                classApplicationLogger.LogError(0, "Experiment list retrieval returned null.");
            else
                Experiments = experimentList;

            //
            // Load datasets
            //
            var datasetList = classSQLiteTools.GetDatasetList();
            if (datasetList == null)
                classApplicationLogger.LogError(0, "Dataset list retrieval returned null.");
            else
            {
                var datasetSortedSet = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
                foreach (var dataset in datasetList)
                {
                    try
                    {
                        datasetSortedSet.Add(dataset);
                    }
                    catch (Exception)
                    {
                        // There should not be any duplicate datasets; but this try/catch block is here to silently ignore the situation if it is encountered
                    }
                }

                Datasets = datasetSortedSet;
            }

            // Now that data has been loaded, enable the timer that will auto-update the data every mDataRefreshIntervalHours hours
            mAutoUpdateTimer.Enabled = true;
        }

        /// <summary>
        /// Abort for the Dms Thread.
        /// </summary>
        private void AbortUpdateThread()
        {
            try
            {
                mUpdateCacheThread.Abort();
            }
            catch
            {
                // Ignore errors here
            }
            finally
            {
                try
                {
                    mUpdateCacheThread.Join(100);
                }
                catch
                {
                    // Ignore errors here
                }
            }
            mUpdateCacheThread = null;
        }

        /// <summary>
        /// Loads the DMS Data Cache
        /// </summary>
        private void StartUpdateThreaded()
        {
            if (mUpdateCacheThread != null)
            {
                AbortUpdateThread();
            }

            // Create a new threaded update
            var start = new ThreadStart(UpdateCacheThread);
            mUpdateCacheThread = new Thread(start);
            mUpdateCacheThread.Start();
        }

        private void UpdateCacheThread()
        {
            mIsUpdating = true;

            var success = UpdateSQLiteCache();
            if (!success)
            {
                mIsUpdating = false;
                return;
            }

            mLastSQLiteUpdate = DateTime.UtcNow;
            LoadDMSDataFromCache(true);

            mIsUpdating = false;
        }

        /// <summary>
        /// Force updating the SQLite cache database with instrument, experiment, dataset, etc. info
        /// </summary>
        public void UpdateCacheNow(string callingFunction = "unknown")
        {

            try
            {
                lock (m_cacheLoadingSync)
                {
                    if (mIsUpdating)
                    {
                        return;
                    }
                    StartUpdateThreaded();
                }
            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(0, string.Format("Exception updating the cached DMS data (called from {0}): {1}", callingFunction, ex.Message));
            }

        }

        #endregion

        public static DMS_DataAccessor Instance
        {
            get;
        }

        #region Member Variables

        private readonly System.Timers.Timer mAutoUpdateTimer;

        private float mDataRefreshIntervalHours;
        private DateTime mLastSQLiteUpdate;
        private DateTime mLastLoadFromCache;

        private readonly object m_cacheLoadingSync = new object();
        private bool mIsUpdating;
        private Thread mUpdateCacheThread;

        #endregion

        #region Private Methods

        void mAutoUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DataRefreshIntervalHours <= 0)
                return;

            if (!(DateTime.UtcNow.Subtract(mLastSQLiteUpdate).TotalHours >= DataRefreshIntervalHours))
            {
                return;
            }

            // Set the Last Update time to now to prevent this function from calling UpdateCacheNow repeatedly if the DMS update takes over 30 seconds
            mLastSQLiteUpdate = DateTime.UtcNow;

            UpdateCacheNow("mAutoUpdateTimer_Elapsed");
        }

        private bool UpdateSQLiteCache()
        {
            try
            {
                // Load active experiments (created/used in the last 18 months), daasets, instruments, etc.
                var dbTools = new classDBTools
                {
                    LoadExperiments = true,
                    LoadDatasets = true,
                    RecentExperimentsMonthsToLoad = RECENT_EXPERIMENT_MONTHS,
                    RecentDatasetsMonthsToLoad = RECENT_DATASET_MONTHS
                };

                dbTools.LoadCacheFromDMS();

                return true;
            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(0, "Error updating the SQLite cache file", ex);
                return false;
            }
        }

        #endregion

        #region EMSL Proposal User Items

        /// <summary>
        /// Adds the given username and user UserIDPIDCrossReference entry to the dictionary
        /// </summary>
        /// <param name="proposalUserToIdMap"></param>
        /// <param name="userName"></param>
        /// <param name="user"></param>
        /// <param name="uniqueifier"></param>
        /// <remarks>
        /// If the username is already defined in the dictionary, appends the uniqueifier
        /// This is necessary because some users are defined in EUS with the same name but different EUS user IDs
        /// </remarks>
        private void AddUserToProposalIdMap(
            IDictionary<string, classUserIDPIDCrossReferenceEntry> proposalUserToIdMap,
            string userName,
            classUserIDPIDCrossReferenceEntry user,
            int uniqueifier)
        {
            if (proposalUserToIdMap.ContainsKey(userName))
                proposalUserToIdMap.Add(userName + uniqueifier, user);
            else
                proposalUserToIdMap.Add(userName, user);
        }

        /// <summary>
        /// Obtain a sorted list of users for the given proposal
        /// </summary>
        /// <param name="proposalUsers"></param>
        /// <param name="userIDtoNameMap"></param>
        /// <returns></returns>
        private List<classUserIDPIDCrossReferenceEntry> GetSortedUsers(
            List<classUserIDPIDCrossReferenceEntry> proposalUsers,
            IDictionary<int, string> userIDtoNameMap)
        {
            if (proposalUsers.Count < 2)
            {
                return proposalUsers;
            }

            var proposalUserToIDMap = new Dictionary<string, classUserIDPIDCrossReferenceEntry>();
            var uniqueifier = 0;

            foreach (var user in proposalUsers)
            {
                try
                {
                    string userName;
                    if (userIDtoNameMap.TryGetValue(user.UserID, out userName))
                    {
                        AddUserToProposalIdMap(proposalUserToIDMap, userName, user, uniqueifier);
                    }
                    else
                    {
                        AddUserToProposalIdMap(proposalUserToIDMap, user.UserID.ToString(), user, uniqueifier);
                    }
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(
                       0,
                       string.Format(
                           "Exception in GetSortedUsers; skipping user {0} for proposal {1}: {2}",
                           user.UserID, user.PID, ex.Message));
                }

                uniqueifier++;
            }

            var query = from item in proposalUserToIDMap orderby item.Key select item.Value;
            return query.ToList();

        }

        /// <summary>
        /// This method loads Proposal User data from a SQLite cache of DMS data. The data includes
        /// a list of the Proposal Users and a dictionary of UserIDs to ProposalID cross references.
        /// The dictionary is indexed by ProposalID.
        /// </summary>
        private void LoadProposalUsers()
        {
            try
            {

                m_pidIndexedCrossReferenceList =
                        new Dictionary<string, List<classUserIDPIDCrossReferenceEntry>>();

                List<classProposalUser> eusUsers;

                // Keys in this dictionary are proposal numbers; values are the users for that proposal
                Dictionary<string, List<classUserIDPIDCrossReferenceEntry>> proposalUserMapping;

                classSQLiteTools.GetProposalUsers(out eusUsers, out proposalUserMapping);

                if (eusUsers.Count == 0)
                    classApplicationLogger.LogError(0, "No Proposal Users found");

                var userIDtoNameMap = new Dictionary<int, string>();
                foreach (var user in eusUsers)
                {
                    userIDtoNameMap.Add(user.UserID, user.UserName);
                }

                foreach (var items in proposalUserMapping)
                {
                    if (items.Value.Count == 0)
                        classApplicationLogger.LogError(0, string.Format("EUS Proposal {0} has no users.", items.Key));

                    // Store the users for this proposal sorted by user last name
                    var sortedProposalUsers = GetSortedUsers(items.Value, userIDtoNameMap);

                    m_pidIndexedCrossReferenceList.Add(
                        items.Key,
                        sortedProposalUsers);
                }

                m_proposalUsers = eusUsers;

                ProposalIDs = new ObservableCollection<string>(m_pidIndexedCrossReferenceList.Keys);

            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(0, "Exception in LoadProposalUsers: " + ex.Message);
            }
        }

        private List<classProposalUser> m_proposalUsers;
        private Dictionary<string, List<classUserIDPIDCrossReferenceEntry>> m_pidIndexedCrossReferenceList;

        /// <summary>
        /// Gets an ObservableCollection of ProposalUsers that are involved with the given PID.
        /// </summary>
        public ObservableCollection<classProposalUser> GetProposalUsers(string proposalID, bool returnAllWhenEmpty = false)
        {
            if (string.IsNullOrWhiteSpace(proposalID))
                proposalID = string.Empty;

            // We haven't built a quick reference collection for this PID
            // yet, so lets do that.
            if (m_proposalUserCollections.ContainsKey(proposalID))
            {
                return m_proposalUserCollections[proposalID];
            }

            ObservableCollection<classProposalUser> newUserCollection;

            // We weren't given a PID to filter out the results, so we are returning every user
            // (unless told otherwise).
            if (string.IsNullOrWhiteSpace(proposalID))
            {
                if (returnAllWhenEmpty)
                {
                    var query = (from item in m_proposalUsers orderby item.UserName select item);
                    newUserCollection = new ObservableCollection<classProposalUser>(query);
                }
                else
                {
                    return new ObservableCollection<classProposalUser>();
                }
            }
            else if (m_pidIndexedCrossReferenceList.ContainsKey(proposalID))
            {
                var crossReferenceList = m_pidIndexedCrossReferenceList[proposalID];

                // This really shouldn't be possible because the PIDs are generated from the
                // User lists, so if there are no Users list, then there's no PID generated.
                // Log there error, and hope that the person that reads it realizes that something
                // is going wrong in the code.
                if (crossReferenceList.Count == 0)
                {
                    classApplicationLogger.LogError(
                        0,
                        string.Format(
                            "Requested Proposal ID '{0}' has no users. Returning empty collection of Proposal Users.",
                            proposalID));

                    newUserCollection = new ObservableCollection<classProposalUser>();
                }
                else
                {
                    // The dictionary has already grouped the cross references by PID, so we just need
                    // to get the UIDs that are in that group.
                    var uIDs = from classUserIDPIDCrossReferenceEntry xRef in crossReferenceList
                               select xRef.UserID;
                    var hashedUIDs = new HashSet<int>(uIDs);

                    // Get the users based on the given UIDs.
                    var singleProposalUsers = from classProposalUser user in m_proposalUsers
                                              where hashedUIDs.Contains(user.UserID)
                                              orderby user.UserName
                                              select user;

                    // Create the user collection and set it for future use.
                    newUserCollection = new ObservableCollection<classProposalUser>(singleProposalUsers);
                }
            }
            // The given PID wasn't in our cross reference list, log the error
            // and return insert an empty collection under it. And, don't insert
            // this into the dictionary of user collections.
            else
            {
                classApplicationLogger.LogMessage(
                    0,
                    string.Format(
                        "Requested Proposal ID '{0}' was not found. Returning empty collection of Proposal Users.",
                        proposalID));

                // Return the collection before we can insert it into the dictionary.
                return new ObservableCollection<classProposalUser>();
            }

            m_proposalUserCollections.Add(proposalID, newUserCollection);

            return m_proposalUserCollections[proposalID];
        }
        private readonly Dictionary<string, ObservableCollection<classProposalUser>> m_proposalUserCollections;

        /// <summary>
        /// Proposal IDs
        /// </summary>
        public ObservableCollection<string> ProposalIDs
        {
            get;
            private set;
        }

        /// <summary>
        /// Search cached EUS proposal users for each user ID in keys
        /// </summary>
        /// <param name="proposalID"></param>
        /// <param name="keys"></param>
        /// <returns>Observable collection of matched users</returns>
        public ObservableCollection<classProposalUser> FindSavedEMSLProposalUsers(string proposalID, List<string> keys)
        {
            if (string.IsNullOrWhiteSpace(proposalID) || keys == null || keys.Count == 0)
                return new ObservableCollection<classProposalUser>();

            // We won't return this collection because this collection is supposed to be
            // inmutable and the items this method was designed for will be altering their
            // collections.
            var allOfProposal_sUsers = GetProposalUsers(proposalID);

            if (allOfProposal_sUsers == null || allOfProposal_sUsers.Count == 0)
                return new ObservableCollection<classProposalUser>();

            var selectedUsers = from classProposalUser u in allOfProposal_sUsers
                                where keys.Contains(u.UserID.ToString())
                                select u;

            var result = new ObservableCollection<classProposalUser>(selectedUsers);
            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// DMS data refresh interval, in hours
        /// </summary>
        public float DataRefreshIntervalHours
        {
            get => mDataRefreshIntervalHours;
            set
            {
                if (value < 0.5)
                    value = 0.5f;

                mDataRefreshIntervalHours = value;

            }
        }

        /// <summary>
        /// List of DMS LC column names
        /// </summary>
        public ObservableCollection<string> ColumnData
        {
            get { return m_ColumnData; }
            private set
            {
                if (m_ColumnData != value)
                {
                    m_ColumnData = value;
                    OnPropertyChanged("ColumnData");
                }
            }
        }
        private ObservableCollection<string> m_ColumnData;

        /// <summary>
        /// List of the DMS instrument names
        /// </summary>
        public ObservableCollection<string> InstrumentData
        {
            get { return m_instrumentData; }
            private set
            {
                if (m_instrumentData != value)
                {
                    m_instrumentData = value;
                    OnPropertyChanged("InstrumentData");
                }
            }
        }
        private ObservableCollection<string> m_instrumentData;

        /// <summary>
        /// Instrument details (Name, status, source hostname, source share name, capture method
        /// </summary>
        /// <remarks>Key is instrument name, value is the details</remarks>
        public Dictionary<string, classInstrumentInfo> InstrumentDetails { get; private set; }

        /// <summary>
        /// This is a list of the names of the cart Operators.
        /// </summary>
        public ObservableCollection<string> OperatorData
        {
            get { return m_operatorData; }
            private set
            {
                if (m_operatorData != value)
                {
                    m_operatorData = value;
                    OnPropertyChanged("OperatorData");
                }
            }
        }
        private ObservableCollection<string> m_operatorData;

        /// <summary>
        /// Dataset types
        /// </summary>
        public ObservableCollection<string> DatasetTypes
        {
            get { return m_datasetTypes; }
            private set
            {
                if (m_datasetTypes != value)
                {
                    m_datasetTypes = value;
                    OnPropertyChanged("DatasetTypes");
                }
            }
        }
        private ObservableCollection<string> m_datasetTypes;

        /// <summary>
        /// Separation types
        /// </summary>
        public ObservableCollection<string> SeparationTypes
        {
            get { return m_separationTypes; }
            private set
            {
                if (m_separationTypes != value)
                {
                    m_separationTypes = value;
                    OnPropertyChanged("SeparationTypes");
                }
            }
        }
        private ObservableCollection<string> m_separationTypes;

        /// <summary>
        /// Cart names
        /// </summary>
        public ObservableCollection<string> CartNames
        {
            get { return m_cartNames; }
            private set
            {
                if (m_cartNames != value)
                {
                    m_cartNames = value;
                    OnPropertyChanged("CartNames");
                }
            }
        }
        private ObservableCollection<string> m_cartNames;

        /// <summary>
        /// Cart config names
        /// </summary>
        public ObservableCollection<string> CartConfigNames
        {
            get { return m_cartConfigNames; }
            private set
            {
                if (m_cartConfigNames != value)
                {
                    m_cartConfigNames = value;
                    OnPropertyChanged("CartConfigNames");
                }
            }
        }
        private ObservableCollection<string> m_cartConfigNames;

        /// <summary>
        /// List of DMS dataset names
        /// </summary>
        /// <remarks>Sorted set for fast lookups (not-case sensitive)</remarks>
        public SortedSet<string> Datasets
        {
            get { return m_Datasets; }
            private set
            {
                if (m_Datasets != value)
                {
                    m_Datasets = value;
                    OnPropertyChanged("Datasets");
                }
            }
        }
        private SortedSet<string> m_Datasets;

        /// <summary>
        /// List of DMS experiment names
        /// </summary>
        /// <remarks>
        /// This isn't meant to be bound to directly, which is why it's a
        /// list and not an ObservableCollection. Due to the large number
        /// of items this tends to hold, I would advise people to try to
        /// filter it down a bit first before inserting it into an
        /// ObservableCollection for binding.
        /// </remarks>
        public List<classExperimentData> Experiments
        {
            get { return m_experiments; }
            set
            {
                if (m_experiments != value)
                {
                    m_experiments = value;
                    OnPropertyChanged("Experiments");
                }
            }
        }
        private List<classExperimentData> m_experiments;

        #endregion

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
