﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using LcmsNetDmsTools;
using LcmsNetData;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using LcmsNetSQLiteTools;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class DMS_DataAccessor : ReactiveObject, IDisposable
    {

        #region Constants

        public const int RECENT_EXPERIMENT_MONTHS = 18;
        public const int RECENT_DATASET_MONTHS = 12;

        #endregion

        #region Initialize

        /// <summary>
        /// Constructor
        /// </summary>
        private DMS_DataAccessor()
        {
            m_proposalUserCollections = new Dictionary<string, ReactiveList<ProposalUser>>();
            LoadProposalUsers();

            Experiments = new List<ExperimentData>();
            Datasets = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);

            mLastSQLiteUpdate = DateTime.UtcNow;
            mLastLoadFromCache = DateTime.UtcNow.AddMinutes(-60);

            mDataRefreshIntervalHours = 6;

            // Load active experiments (created/used in the last 18 months), datasets, instruments, etc.
            dmsDbTools = new DMSDBTools
            {
                LoadExperiments = true,
                LoadDatasets = true,
                RecentExperimentsMonthsToLoad = RECENT_EXPERIMENT_MONTHS,
                RecentDatasetsMonthsToLoad = RECENT_DATASET_MONTHS
            };
        }

        public void Dispose()
        {
            mAutoUpdateTimer?.Dispose();
            dmsDbTools?.Dispose();
        }

        private void StartAutoUpdateTimer()
        {
            if (mAutoUpdateTimer == null)
            {
                mAutoUpdateTimer = new Timer(AutoUpdateTimer_Tick, this, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            }
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
            using (InstrumentData.SuppressChangeNotifications())
            {
                var tempInstrumentData = SQLiteTools.GetInstrumentList(false);
                if (tempInstrumentData == null)
                {
                    ApplicationLogger.LogError(0, "Instrument list retrieval returned null.");
                    InstrumentData.Clear();
                }
                else
                {
                    if (tempInstrumentData.Count == 0)
                        ApplicationLogger.LogError(0, "No instruments found.");
                }

                if (tempInstrumentData != null && tempInstrumentData.Count != 0)
                {
                    InstrumentData.Clear();
                    InstrumentData.AddRange(tempInstrumentData.Select(instDatum => instDatum.DMSName));

                    InstrumentDetails.Clear();

                    foreach (var instrument in tempInstrumentData)
                    {
                        if (!InstrumentDetails.ContainsKey(instrument.DMSName))
                        {
                            InstrumentDetails.Add(instrument.DMSName, instrument);
                        }
                    }
                }
            }

            //
            // Load Operator Data
            //
            var tempUserList = SQLiteTools.GetUserList(false);
            if (tempUserList == null)
                ApplicationLogger.LogError(0, "User retrieval returned null.");
            else
            {
                using (OperatorData.SuppressChangeNotifications())
                {
                    OperatorData.Clear();
                    OperatorData.AddRange(tempUserList.Select(userDatum => userDatum.UserName));
                }
            }

            //
            // Load Dataset Types
            //
            var tempDatasetTypesList = SQLiteTools.GetDatasetTypeList(false);
            if (tempDatasetTypesList == null)
                ApplicationLogger.LogError(0, "Dataset Types retrieval returned null.");
            else
            {
                using (DatasetTypes.SuppressChangeNotifications())
                {
                    DatasetTypes.Clear();
                    DatasetTypes.AddRange(tempDatasetTypesList);
                }
            }

            //
            // Load Separation Types
            //
            var tempSeparationTypesList = SQLiteTools.GetSepTypeList(false);
            if (tempSeparationTypesList == null)
                ApplicationLogger.LogError(0, "Separation types retrieval returned null.");
            else
            {
                using (SeparationTypes.SuppressChangeNotifications())
                {
                    SeparationTypes.Clear();
                    SeparationTypes.AddRange(tempSeparationTypesList);
                }
            }

            //
            // Load Cart Names
            //
            var tempCartsList = SQLiteTools.GetCartNameList();
            if (tempCartsList == null)
                ApplicationLogger.LogError(0, "LC Cart names list retrieval returned null.");
            else
            {
                using (CartNames.SuppressChangeNotifications())
                {
                    CartNames.Clear();
                    CartNames.AddRange(tempCartsList);
                }
            }

            //
            // Load Cart Config Names
            //
            var tempCartConfigNamesList = SQLiteTools.GetCartConfigNameList(false);
            if (tempCartConfigNamesList == null)
                ApplicationLogger.LogError(0, "LC Cart config names list retrieval returned null.");
            else
            {
                using (CartConfigNames.SuppressChangeNotifications())
                {
                    CartConfigNames.Clear();
                    CartConfigNames.AddRange(tempCartConfigNamesList);
                }
            }

            //
            // Load column data
            //
            var tempColumnData = SQLiteTools.GetColumnList(false);
            if (tempColumnData == null)
                ApplicationLogger.LogError(0, "Column data list retrieval returned null.");
            else
            {
                using (ColumnData.SuppressChangeNotifications())
                {
                    ColumnData.Clear();
                    ColumnData.AddRange(tempColumnData);
                }
            }

            //
            // Load Experiments
            //
            var experimentList = SQLiteTools.GetExperimentList();
            if (experimentList == null)
                ApplicationLogger.LogError(0, "Experiment list retrieval returned null.");
            else
                Experiments = experimentList;

            //
            // Load datasets
            //
            var datasetList = SQLiteTools.GetDatasetList();
            if (datasetList == null)
                ApplicationLogger.LogError(0, "Dataset list retrieval returned null.");
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
            StartAutoUpdateTimer();
        }

        /// <summary>
        /// Loads the DMS Data Cache
        /// </summary>
        private void UpdateCache()
        {
            mIsUpdating = true;

            var success = UpdateSQLiteCacheFromDms();
            if (!success)
            {
                mIsUpdating = false;
                return;
            }

            mLastSQLiteUpdate = DateTime.UtcNow;
            RxApp.MainThreadScheduler.Schedule(() => LoadDMSDataFromCache(true));

            mIsUpdating = false;
        }

        /// <summary>
        /// Force updating the SQLite cache database with instrument, experiment, dataset, etc. info
        /// </summary>
        public async Task UpdateCacheNow(string callingFunction = "unknown")
        {
            lock (m_cacheLoadingSync)
            {
                if (mIsUpdating)
                {
                    return;
                }

                mIsUpdating = true;
            }

            try
            {
                await Task.Run(() => UpdateCache()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, string.Format("Exception updating the cached DMS data (called from {0}): {1}", callingFunction, ex.Message));
            }

            lock (m_cacheLoadingSync)
            {
                mIsUpdating = false;
            }
        }

        public List<SampleDataBasic> LoadDMSRequestedRuns()
        {
            // Instantiate SampleQueryData using default filters (essentially no filters)
            // Only active requested runs are retrieved
            var queryData = new SampleQueryData();

            // Load the samples (essentially requested runs) from DMS
            return dmsDbTools.GetRequestedRunsFromDMS<SampleDataBasic>(queryData);
        }

        #endregion

        public static DMS_DataAccessor Instance { get; }

        #region Member Variables

        private Timer mAutoUpdateTimer;

        private float mDataRefreshIntervalHours;
        private DateTime mLastSQLiteUpdate;
        private DateTime mLastLoadFromCache;
        private readonly DMSDBTools dmsDbTools;

        private List<ProposalUser> m_proposalUsers;
        private Dictionary<string, List<UserIDPIDCrossReferenceEntry>> m_pidIndexedCrossReferenceList;
        private readonly Dictionary<string, ReactiveList<ProposalUser>> m_proposalUserCollections;
        private SortedSet<string> m_Datasets;
        private List<ExperimentData> m_experiments;

        private readonly object m_cacheLoadingSync = new object();
        private bool mIsUpdating;

        #endregion

        #region Private Methods

        private async void AutoUpdateTimer_Tick(object state)
        {
            if (DataRefreshIntervalHours <= 0)
                return;

            if (!(DateTime.UtcNow.Subtract(mLastSQLiteUpdate).TotalHours >= DataRefreshIntervalHours))
            {
                return;
            }

            // Set the Last Update time to now to prevent this function from calling UpdateCacheNow repeatedly if the DMS update takes over 30 seconds
            mLastSQLiteUpdate = DateTime.UtcNow;

            await UpdateCacheNow("AutoUpdateTimer_Tick");
        }

        /// <summary>
        /// Update data from DMS, with optional extra logging
        /// </summary>
        /// <param name="progressEventHandler">Handler to report progress information from DMSDBTools</param>
        /// <param name="errorAction">Handler to report exception information</param>
        /// <returns></returns>
        public bool UpdateSQLiteCacheFromDms(ProgressEventHandler progressEventHandler = null, Action<string, Exception> errorAction = null)
        {
            try
            {
                if (progressEventHandler != null)
                {
                    dmsDbTools.ProgressEvent += progressEventHandler;
                }

                dmsDbTools.LoadCacheFromDMS();

                return true;
            }
            catch (Exception ex)
            {
                var message = "Error loading data from DMS and updating the SQLite cache file!";
                ApplicationLogger.LogError(0, message, ex);
                errorAction?.Invoke(message, ex);
                return false;
            }
            finally
            {
                if (progressEventHandler != null)
                {
                    dmsDbTools.ProgressEvent -= progressEventHandler;
                }
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
            IDictionary<string, UserIDPIDCrossReferenceEntry> proposalUserToIdMap,
            string userName,
            UserIDPIDCrossReferenceEntry user,
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
        private List<UserIDPIDCrossReferenceEntry> GetSortedUsers(
            List<UserIDPIDCrossReferenceEntry> proposalUsers,
            IDictionary<int, string> userIDtoNameMap)
        {
            if (proposalUsers.Count < 2)
            {
                return proposalUsers;
            }

            var proposalUserToIDMap = new Dictionary<string, UserIDPIDCrossReferenceEntry>();
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
                    ApplicationLogger.LogError(
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
                m_pidIndexedCrossReferenceList = new Dictionary<string, List<UserIDPIDCrossReferenceEntry>>();

                List<ProposalUser> eusUsers;

                // Keys in this dictionary are proposal numbers; values are the users for that proposal
                Dictionary<string, List<UserIDPIDCrossReferenceEntry>> proposalUserMapping;

                SQLiteTools.GetProposalUsers(out eusUsers, out proposalUserMapping);

                if (eusUsers.Count == 0)
                    ApplicationLogger.LogError(0, "No Proposal Users found");

                var userIDtoNameMap = new Dictionary<int, string>();
                foreach (var user in eusUsers)
                {
                    userIDtoNameMap.Add(user.UserID, user.UserName);
                }

                foreach (var items in proposalUserMapping)
                {
                    if (items.Value.Count == 0)
                        ApplicationLogger.LogError(0, string.Format("EUS Proposal {0} has no users.", items.Key));

                    // Store the users for this proposal sorted by user last name
                    var sortedProposalUsers = GetSortedUsers(items.Value, userIDtoNameMap);

                    m_pidIndexedCrossReferenceList.Add(
                        items.Key,
                        sortedProposalUsers);
                }

                m_proposalUsers = eusUsers;

                ProposalIDs = new ReactiveList<string>(m_pidIndexedCrossReferenceList.Keys);

            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, "Exception in LoadProposalUsers: " + ex.Message);
            }
        }

        /// <summary>
        /// Gets an ReactiveList of ProposalUsers that are involved with the given PID.
        /// </summary>
        public ReactiveList<ProposalUser> GetProposalUsers(string proposalID, bool returnAllWhenEmpty = false)
        {
            if (string.IsNullOrWhiteSpace(proposalID))
                proposalID = string.Empty;

            // We haven't built a quick reference collection for this PID
            // yet, so lets do that.
            if (m_proposalUserCollections.ContainsKey(proposalID))
            {
                return m_proposalUserCollections[proposalID];
            }

            ReactiveList<ProposalUser> newUserCollection;

            // We weren't given a PID to filter out the results, so we are returning every user
            // (unless told otherwise).
            if (string.IsNullOrWhiteSpace(proposalID))
            {
                if (returnAllWhenEmpty)
                {
                    var query = (from item in m_proposalUsers orderby item.UserName select item);
                    newUserCollection = new ReactiveList<ProposalUser>(query);
                }
                else
                {
                    return new ReactiveList<ProposalUser>();
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
                    ApplicationLogger.LogError(
                        0,
                        string.Format(
                            "Requested Proposal ID '{0}' has no users. Returning empty collection of Proposal Users.",
                            proposalID));

                    newUserCollection = new ReactiveList<ProposalUser>();
                }
                else
                {
                    // The dictionary has already grouped the cross references by PID, so we just need
                    // to get the UIDs that are in that group.
                    var uIDs = from UserIDPIDCrossReferenceEntry xRef in crossReferenceList
                               select xRef.UserID;
                    var hashedUIDs = new HashSet<int>(uIDs);

                    // Get the users based on the given UIDs.
                    var singleProposalUsers = from ProposalUser user in m_proposalUsers
                                              where hashedUIDs.Contains(user.UserID)
                                              orderby user.UserName
                                              select user;

                    // Create the user collection and set it for future use.
                    newUserCollection = new ReactiveList<ProposalUser>(singleProposalUsers);
                }
            }
            // The given PID wasn't in our cross reference list, log the error
            // and return insert an empty collection under it. And, don't insert
            // this into the dictionary of user collections.
            else
            {
                ApplicationLogger.LogMessage(
                    0,
                    string.Format(
                        "Requested Proposal ID '{0}' was not found. Returning empty collection of Proposal Users.",
                        proposalID));

                // Return the collection before we can insert it into the dictionary.
                return new ReactiveList<ProposalUser>();
            }

            m_proposalUserCollections.Add(proposalID, newUserCollection);

            return m_proposalUserCollections[proposalID];
        }

        /// <summary>
        /// Proposal IDs
        /// </summary>
        public ReactiveList<string> ProposalIDs
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
        public ReactiveList<ProposalUser> FindSavedEMSLProposalUsers(string proposalID, List<string> keys)
        {
            if (string.IsNullOrWhiteSpace(proposalID) || keys == null || keys.Count == 0)
                return new ReactiveList<ProposalUser>();

            // We won't return this collection because this collection is supposed to be
            // inmutable and the items this method was designed for will be altering their
            // collections.
            var allOfProposal_sUsers = GetProposalUsers(proposalID);

            if (allOfProposal_sUsers == null || allOfProposal_sUsers.Count == 0)
                return new ReactiveList<ProposalUser>();

            var selectedUsers = from ProposalUser u in allOfProposal_sUsers
                                where keys.Contains(u.UserID.ToString())
                                select u;

            var result = new ReactiveList<ProposalUser>(selectedUsers);
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
        public ReactiveList<string> ColumnData { get; } = new ReactiveList<string>();

        /// <summary>
        /// List of the DMS instrument names
        /// </summary>
        public ReactiveList<string> InstrumentData { get; } = new ReactiveList<string>();

        /// <summary>
        /// Instrument details (Name, status, source hostname, source share name, capture method
        /// </summary>
        /// <remarks>Key is instrument name, value is the details</remarks>
        public Dictionary<string, InstrumentInfo> InstrumentDetails { get; } = new Dictionary<string, InstrumentInfo>();

        /// <summary>
        /// This is a list of the names of the cart Operators.
        /// </summary>
        public ReactiveList<string> OperatorData { get; } = new ReactiveList<string>();

        /// <summary>
        /// Dataset types
        /// </summary>
        public ReactiveList<string> DatasetTypes { get; } = new ReactiveList<string>();

        /// <summary>
        /// Separation types
        /// </summary>
        public ReactiveList<string> SeparationTypes { get; } = new ReactiveList<string>();

        /// <summary>
        /// Cart names
        /// </summary>
        public ReactiveList<string> CartNames { get; } = new ReactiveList<string>();

        /// <summary>
        /// Cart config names
        /// </summary>
        public ReactiveList<string> CartConfigNames { get; } = new ReactiveList<string>();

        /// <summary>
        /// List of DMS dataset names
        /// </summary>
        /// <remarks>Sorted set for fast lookups (not-case sensitive)</remarks>
        public SortedSet<string> Datasets
        {
            get => m_Datasets;
            private set => this.RaiseAndSetIfChanged(ref m_Datasets, value);
        }

        /// <summary>
        /// List of DMS experiment names
        /// </summary>
        /// <remarks>
        /// This isn't meant to be bound to directly, which is why it's a
        /// list and not an ReactiveList. Due to the large number
        /// of items this tends to hold, I would advise people to try to
        /// filter it down a bit first before inserting it into an
        /// ReactiveList for binding.
        /// </remarks>
        public List<ExperimentData> Experiments
        {
            get => m_experiments;
            set => this.RaiseAndSetIfChanged(ref m_experiments, value);
        }

        #endregion
    }
}
