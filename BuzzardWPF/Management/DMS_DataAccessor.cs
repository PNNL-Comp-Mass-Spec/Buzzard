using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
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

        public const int RecentExperimentMonths = 18;
        public const int RecentDatasetMonths = 12;
        public const int RecentEMSLProposalMonths = 12;

        private readonly string[] interestRatingOptions = { "Unreviewed", "Not Released", "Released", "Rerun (Good Data)", "Rerun (Superseded)" };

        #endregion

        #region Initialize

        /// <summary>
        /// Constructor
        /// </summary>
        private DMS_DataAccessor()
        {
            InterestRatingCollection = interestRatingOptions;
            // These values come from table T_EUS_UsageType
            // It is rarely updated, so we're not querying the database every time
            // Previously used, but deprecated in April 2017 is USER_UNKNOWN
            EMSLUsageTypesSource = new [] { "BROKEN", "CAP_DEV", "MAINTENANCE", "USER" };

            LastSqliteCacheUpdateUtc = DateTime.UtcNow;
            LastLoadFromSqliteCacheUtc = DateTime.UtcNow.AddMinutes(-60);

            lastSqliteCacheUpdate = this.WhenAnyValue(x => x.LastSqliteCacheUpdateUtc).ObserveOn(RxApp.MainThreadScheduler).Select(x => x.ToLocalTime()).ToProperty(this, x => x.LastSqliteCacheUpdate);
            lastLoadFromSqliteCache = this.WhenAnyValue(x => x.LastLoadFromSqliteCacheUtc).ObserveOn(RxApp.MainThreadScheduler).Select(x => x.ToLocalTime()).ToProperty(this, x => x.LastLoadFromSqliteCache);

            dataRefreshIntervalHours = 6;

            // Load active experiments (created/used in the last 18 months), datasets, instruments, etc.
            dmsDbTools = new DMSDBTools
            {
                LoadExperiments = true,
                LoadDatasets = true,
                RecentExperimentsMonthsToLoad = RecentExperimentMonths,
                RecentDatasetsMonthsToLoad = RecentDatasetMonths,
                EMSLProposalsRecentMonthsToLoad = RecentEMSLProposalMonths
            };

            cartNamesSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var cartNames).Subscribe();
            CartNames = cartNames;
            proposalIDsSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var proposalIDs).Subscribe();
            ProposalIDs = proposalIDs;
            columnDataSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var columnData).Subscribe();
            ColumnData = columnData;
            instrumentDataSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var instrumentData).Subscribe();
            InstrumentData = instrumentData;
            operatorDataSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var operatorData).Subscribe();
            OperatorData = operatorData;
            datasetTypesSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var datasetTypes).Subscribe();
            DatasetTypes = datasetTypes;
            separationTypesSource.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var separationTypes).Subscribe();
            SeparationTypes = separationTypes;

            autoUpdateTimer = new Timer(AutoUpdateTimer_Tick, this, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            autoUpdateTimer?.Dispose();
            dmsDbTools?.Dispose();
            proposalIDsSource.Dispose();
            columnDataSource.Dispose();
            instrumentDataSource.Dispose();
            operatorDataSource.Dispose();
            datasetTypesSource.Dispose();
            separationTypesSource.Dispose();
            cartNamesSource.Dispose();
            cartConfigNamesSource.Dispose();
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
            if (DateTime.UtcNow.Subtract(LastLoadFromSqliteCacheUtc).TotalMinutes < 1 && !forceLoad)
                return;

            const bool forceReloadFromCache = true;

            // Load Instrument Data
            var tempInstrumentData = SQLiteTools.GetInstrumentList(forceReloadFromCache);
            if (tempInstrumentData == null)
            {
                ApplicationLogger.LogError(0, "Instrument list retrieval returned null.");
                instrumentDataSource.Clear();
            }
            else
            {
                if (tempInstrumentData.Count == 0)
                    ApplicationLogger.LogError(0, "No instruments found.");
            }

            if (tempInstrumentData != null && tempInstrumentData.Count != 0)
            {
                instrumentDataSource.Clear();
                instrumentDataSource.AddRange(tempInstrumentData.Select(instDatum => instDatum.DMSName));

                InstrumentDetails.Clear();

                foreach (var instrument in tempInstrumentData)
                {
                    if (!InstrumentDetails.ContainsKey(instrument.DMSName))
                    {
                        InstrumentDetails.Add(instrument.DMSName, instrument);
                    }
                }
            }

            // Load Operator Data
            var tempUserList = SQLiteTools.GetUserList(forceReloadFromCache);
            if (tempUserList == null)
                ApplicationLogger.LogError(0, "User retrieval returned null.");
            else
            {
                operatorDataSource.Clear();
                operatorDataSource.AddRange(tempUserList.Select(userDatum => userDatum.UserName));
            }

            // Load Dataset Types
            var tempDatasetTypesList = SQLiteTools.GetDatasetTypeList(forceReloadFromCache);
            if (tempDatasetTypesList == null)
                ApplicationLogger.LogError(0, "Dataset Types retrieval returned null.");
            else
            {
                datasetTypesSource.Clear();
                datasetTypesSource.AddRange(tempDatasetTypesList);
            }

            // Load Separation Types
            var tempSeparationTypesList = SQLiteTools.GetSepTypeList(forceReloadFromCache);
            if (tempSeparationTypesList == null)
                ApplicationLogger.LogError(0, "Separation types retrieval returned null.");
            else
            {
                separationTypesSource.Clear();
                separationTypesSource.AddRange(tempSeparationTypesList);
            }

            // Load Cart Names
            var tempCartsList = SQLiteTools.GetCartNameList();
            if (tempCartsList == null)
                ApplicationLogger.LogError(0, "LC Cart names list retrieval returned null.");
            else
            {
                cartNamesSource.Clear();
                cartNamesSource.AddRange(tempCartsList);
            }

            // Guarantee "unknown" cart name option
            if (!cartNamesSource.Items.Contains("unknown"))
            {
                cartNamesSource.Add("unknown");
            }

            // Load CartConfigNameMap
            var tempCartConfigNameMap = SQLiteTools.GetCartConfigNameMap(forceReloadFromCache);
            if (tempCartConfigNameMap == null)
                ApplicationLogger.LogError(0, "LC Cart config names map retrieval returned null.");
            else
            {
                cartConfigNameMap = tempCartConfigNameMap;
            }

            // Load column data
            var tempColumnData = SQLiteTools.GetColumnList(forceReloadFromCache);
            if (tempColumnData == null)
                ApplicationLogger.LogError(0, "Column data list retrieval returned null.");
            else
            {
                columnDataSource.Clear();
                columnDataSource.AddRange(tempColumnData);
            }

            // Load Experiments
            var experimentList = SQLiteTools.GetExperimentList();
            if (experimentList == null)
                ApplicationLogger.LogError(0, "Experiment list retrieval returned null.");
            else
            {
                Experiments.Clear();
                Experiments.AddRange(experimentList);
            }

            // Load Work Packages
            var workPackageMap = SQLiteTools.GetWorkPackageMap(forceReloadFromCache);
            if (workPackageMap == null)
                ApplicationLogger.LogError(0, "Work package list retrieval returned null.");
            else
            {
                if (!workPackageMap.ContainsKey("none"))
                {
                    workPackageMap.Add("none", new WorkPackageInfo("none", "Active", "none", "none", "No Work Package", "none", "none"));
                }

                WorkPackageMap = workPackageMap;
                WorkPackages.Clear();
                WorkPackages.AddRange(WorkPackageMap.Values.OrderBy(x => x.ChargeCode));
            }

            // Load EMSL Proposal information
            LoadProposalUsers();

            LastLoadFromSqliteCacheUtc = DateTime.UtcNow;

            // Now that data has been loaded, enable the timer that will auto-update the data every mDataRefreshIntervalHours hours
            // It's okay to re-set this every time we run an update.
            autoUpdateTimer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Force updating the SQLite cache database with instrument, experiment, dataset, etc. info
        /// </summary>
        public async Task UpdateCacheNow([CallerMemberName] string callingFunction = "unknown")
        {
            lock (cacheLoadingLock)
            {
                if (isUpdatingCache)
                {
                    return;
                }

                isUpdatingCache = true;
            }

            try
            {
                await Task.Run(() =>
                {
                    var success = UpdateSQLiteCacheFromDms();
                    if (!success)
                    {
                        return;
                    }

                    LoadDMSDataFromCache(true);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, string.Format("Exception updating the cached DMS data (called from {0}): {1}", callingFunction, ex.Message));
            }

            lock (cacheLoadingLock)
            {
                isUpdatingCache = false;
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

        public DateTime LastSqliteCacheUpdate => lastSqliteCacheUpdate.Value;

        public DateTime LastLoadFromSqliteCache => lastLoadFromSqliteCache.Value;

        #region Member Variables

        private readonly Timer autoUpdateTimer;

        private float dataRefreshIntervalHours;
        private DateTime lastSqliteCacheUpdateUtc;
        private DateTime lastLoadFromSqliteCacheUtc;
        private readonly DMSDBTools dmsDbTools;

        private readonly ObservableAsPropertyHelper<DateTime> lastSqliteCacheUpdate;
        private readonly ObservableAsPropertyHelper<DateTime> lastLoadFromSqliteCache;

        private readonly List<ProposalUser> proposalUsersList = new List<ProposalUser>();
        private readonly Dictionary<string, List<UserIDPIDCrossReferenceEntry>> pidIndexedCrossReferenceList = new Dictionary<string, List<UserIDPIDCrossReferenceEntry>>();
        private readonly Dictionary<string, IReadOnlyList<ProposalUser>> proposalUserCollections = new Dictionary<string, IReadOnlyList<ProposalUser>>();

        private readonly object cacheLoadingLock = new object();
        private bool isUpdatingCache;

        /// <summary>
        /// Key is cart name, value is list of valid cart config names for that cart.
        /// </summary>
        private Dictionary<string, List<string>> cartConfigNameMap = new Dictionary<string, List<string>>();

        // Backing lists for collections that can be provided to the UI.
        private readonly SourceList<string> proposalIDsSource = new SourceList<string>();
        private readonly SourceList<string> columnDataSource = new SourceList<string>();
        private readonly SourceList<string> instrumentDataSource = new SourceList<string>();
        private readonly SourceList<string> operatorDataSource = new SourceList<string>();
        private readonly SourceList<string> datasetTypesSource = new SourceList<string>();
        private readonly SourceList<string> separationTypesSource = new SourceList<string>();
        private readonly SourceList<string> cartNamesSource = new SourceList<string>();
        private readonly SourceList<string> cartConfigNamesSource = new SourceList<string>();

        #endregion

        #region Private Methods

        private DateTime LastSqliteCacheUpdateUtc
        {
            get => lastSqliteCacheUpdateUtc;
            set => this.RaiseAndSetIfChanged(ref lastSqliteCacheUpdateUtc, value);
        }

        private DateTime LastLoadFromSqliteCacheUtc
        {
            get => lastLoadFromSqliteCacheUtc;
            set => this.RaiseAndSetIfChanged(ref lastLoadFromSqliteCacheUtc, value);
        }

        private async void AutoUpdateTimer_Tick(object state)
        {
            if (DataRefreshIntervalHours <= 0)
                return;

            if (!(DateTime.UtcNow.Subtract(LastSqliteCacheUpdateUtc).TotalHours >= DataRefreshIntervalHours))
            {
                return;
            }

            // Set the Last Update time to now to prevent this function from calling UpdateCacheNow repeatedly if the DMS update takes over 30 seconds
            LastSqliteCacheUpdateUtc = DateTime.UtcNow;

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
            var retries = 3;
            var dmsAvailable = dmsDbTools.CheckDMSConnection();
            var result = false;
            while (retries > 0)
            {
                retries--;
                try
                {
                    if (SQLiteTools.DatabaseImageBad && dmsAvailable)
                    {
                        SQLiteTools.DeleteBadCache();
                    }

                    if (progressEventHandler != null)
                    {
                        dmsDbTools.ProgressEvent += progressEventHandler;
                    }

                    dmsDbTools.LoadCacheFromDMS();

                    if (SQLiteTools.DatabaseImageBad && dmsAvailable && retries > 0)
                    {
                        continue;
                    }

                    LastSqliteCacheUpdateUtc = DateTime.UtcNow;
                    result = true;
                    break;
                }
                catch (Exception ex)
                {
                    var message = "Error loading data from DMS and updating the SQLite cache file!";
                    ApplicationLogger.LogError(0, message, ex);
                    if (SQLiteTools.DatabaseImageBad && dmsAvailable && retries > 0)
                    {
                        continue;
                    }

                    errorAction?.Invoke(message, ex);
                    result = false;
                    break;
                }
                finally
                {
                    if (progressEventHandler != null)
                    {
                        dmsDbTools.ProgressEvent -= progressEventHandler;
                    }
                }
            }

            return result;
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

            return proposalUserToIDMap.OrderBy(item => item.Key).Select(item => item.Value).ToList();
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

                pidIndexedCrossReferenceList.Clear();
                foreach (var items in proposalUserMapping)
                {
                    if (items.Value.Count == 0)
                        ApplicationLogger.LogError(0, string.Format("EUS Proposal {0} has no users.", items.Key));

                    // Store the users for this proposal sorted by user last name
                    var sortedProposalUsers = GetSortedUsers(items.Value, userIDtoNameMap);

                    pidIndexedCrossReferenceList.Add(
                        items.Key,
                        sortedProposalUsers);
                }

                proposalUsersList.Clear();
                proposalUsersList.AddRange(eusUsers);

                proposalIDsSource.Clear();
                proposalIDsSource.AddRange(pidIndexedCrossReferenceList.Keys);

            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, "Exception in LoadProposalUsers: " + ex.Message);
            }
        }

        /// <summary>
        /// Gets a list of ProposalUsers that are involved with the given PID.
        /// </summary>
        public IReadOnlyList<ProposalUser> GetProposalUsers(string proposalID, bool returnAllWhenEmpty = false)
        {
            if (string.IsNullOrWhiteSpace(proposalID))
                proposalID = string.Empty;

            // We haven't built a quick reference collection for this PID
            // yet, so lets do that.
            if (proposalUserCollections.ContainsKey(proposalID))
            {
                return proposalUserCollections[proposalID];
            }

            List<ProposalUser> newUserCollection;

            // We weren't given a PID to filter out the results, so we are returning every user
            // (unless told otherwise).
            if (string.IsNullOrWhiteSpace(proposalID))
            {
                if (returnAllWhenEmpty)
                {
                    var query = proposalUsersList.OrderBy(item => item.UserName);
                    newUserCollection = new List<ProposalUser>(query);
                }
                else
                {
                    return new List<ProposalUser>();
                }
            }
            else if (pidIndexedCrossReferenceList.ContainsKey(proposalID))
            {
                var crossReferenceList = pidIndexedCrossReferenceList[proposalID];

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

                    newUserCollection = new List<ProposalUser>();
                }
                else
                {
                    // The dictionary has already grouped the cross references by PID, so we just need
                    // to get the UIDs that are in that group.
                    var uIDs = crossReferenceList.Select(xRef => xRef.UserID);
                    var hashedUIDs = new HashSet<int>(uIDs);

                    // Get the users based on the given UIDs.
                    var singleProposalUsers = proposalUsersList.Where(user => hashedUIDs.Contains(user.UserID))
                                                             .OrderBy(user => user.UserName);

                    // Create the user collection and set it for future use.
                    newUserCollection = new List<ProposalUser>(singleProposalUsers);
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
                return new List<ProposalUser>();
            }

            proposalUserCollections.Add(proposalID, newUserCollection.ToArray());

            return proposalUserCollections[proposalID];
        }

        /// <summary>
        /// Search cached EUS proposal users for each user ID in keys
        /// </summary>
        /// <param name="proposalID"></param>
        /// <param name="keys"></param>
        /// <returns>IEnumerable of matched users</returns>
        public IEnumerable<ProposalUser> FindSavedEMSLProposalUsers(string proposalID, List<string> keys)
        {
            if (string.IsNullOrWhiteSpace(proposalID) || keys == null || keys.Count == 0)
                return Enumerable.Empty<ProposalUser>();

            // We won't return this collection because this collection is supposed to be
            // inmutable and the items this method was designed for will be altering their
            // collections.
            var allProposalUsers = GetProposalUsers(proposalID);

            if (allProposalUsers == null || allProposalUsers.Count == 0)
                return Enumerable.Empty<ProposalUser>();

            return allProposalUsers.Where(x => keys.Contains(x.UserID.ToString()));
        }

        /// <summary>
        /// Gets the list of cart config names for a specified cart, returning an empty list if the cart name is not found.
        /// </summary>
        /// <param name="cartName"></param>
        /// <returns></returns>
        public IReadOnlyList<string> GetCartConfigNamesForCart(string cartName)
        {
            if (string.IsNullOrWhiteSpace(cartName))
            {
                return new List<string>();
            }

            if (cartConfigNameMap.TryGetValue(cartName, out var configNames))
            {
                return configNames;
            }

            return new List<string>();
        }

        /// <summary>
        /// Query the SQLite cache to determine if a dataset name exists
        /// </summary>
        /// <param name="datasetName"></param>
        /// <returns></returns>
        public bool CheckDatasetExists(string datasetName)
        {
            return SQLiteTools.CheckDatasetExists(datasetName);
        }

        #endregion

        #region Properties

        public IReadOnlyList<string> InterestRatingCollection { get; }

        public IReadOnlyList<string> EMSLUsageTypesSource { get; }

        /// <summary>
        /// Proposal IDs observable list
        /// </summary>
        public ReadOnlyObservableCollection<string> ProposalIDs { get; }

        /// <summary>
        /// DMS data refresh interval, in hours
        /// </summary>
        public float DataRefreshIntervalHours
        {
            get => dataRefreshIntervalHours;
            set
            {
                if (value < 0.5)
                    value = 0.5f;

                dataRefreshIntervalHours = value;
            }
        }

        /// <summary>
        /// Observable List of DMS LC column names
        /// </summary>
        public ReadOnlyObservableCollection<string> ColumnData { get; }

        /// <summary>
        /// Observable List of the DMS instrument names
        /// </summary>
        public ReadOnlyObservableCollection<string> InstrumentData { get; }

        /// <summary>
        /// Instrument details (Name, status, source hostname, source share name, capture method
        /// </summary>
        /// <remarks>Key is instrument name, value is the details</remarks>
        public Dictionary<string, InstrumentInfo> InstrumentDetails { get; } = new Dictionary<string, InstrumentInfo>();

        /// <summary>
        /// This is an Observable list of the names of the cart Operators.
        /// </summary>
        public ReadOnlyObservableCollection<string> OperatorData { get; }

        /// <summary>
        /// Dataset types Observable list
        /// </summary>
        public ReadOnlyObservableCollection<string> DatasetTypes { get; }

        /// <summary>
        /// Separation types Observable list
        /// </summary>
        public ReadOnlyObservableCollection<string> SeparationTypes { get; }

        /// <summary>
        /// Cart names
        /// </summary>
        public ReadOnlyObservableCollection<string> CartNames { get; }

        /// <summary>
        /// Key is charge code, value is all the details
        /// </summary>
        public Dictionary<string, WorkPackageInfo> WorkPackageMap { get; private set; } = new Dictionary<string, WorkPackageInfo>();

        public SourceList<WorkPackageInfo> WorkPackages { get; } = new SourceList<WorkPackageInfo>();

        /// <summary>
        /// List of DMS experiment names
        /// </summary>
        /// <remarks>
        /// This isn't meant to be bound to directly, which is why it's a SourceList and not an ObservableCollection.
        /// </remarks>
        public SourceList<ExperimentData> Experiments { get; } = new SourceList<ExperimentData>();

        /// <summary>
        /// Read-only, non-observable retrieval of the CartNames collection contents
        /// </summary>
        public IEnumerable<string> CartNamesItems => cartNamesSource.Items;

        /// <summary>
        /// Read-only, non-observable retrieval of the DatasetTypes collection contents
        /// </summary>
        public IEnumerable<string> DatasetTypesItems => datasetTypesSource.Items;

        /// <summary>
        /// Read-only, non-observable retrieval of the InstrumentData collection contents
        /// </summary>
        public IEnumerable<string> InstrumentDataItems => instrumentDataSource.Items;

        /// <summary>
        /// Read-only, non-observable retrieval of the OperatorData collection contents
        /// </summary>
        public IEnumerable<string> OperatorDataItems => operatorDataSource.Items;

        /// <summary>
        /// Read-only, non-observable retrieval of the SeparationTypes collection contents
        /// </summary>
        public IEnumerable<string> SeparationTypesItems => separationTypesSource.Items;

        /// <summary>
        /// Read-only, non-observable retrieval of the ColumnData collection contents
        /// </summary>
        public IEnumerable<string> ColumnDataItems => columnDataSource.Items;

        #endregion
    }
}
