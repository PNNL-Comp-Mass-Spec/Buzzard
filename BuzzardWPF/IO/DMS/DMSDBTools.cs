using System;
using System.Collections.Generic;
using System.Linq;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.IO.SQLite;
using BuzzardWPF.Logging;

// ReSharper disable UnusedMember.Global

namespace BuzzardWPF.IO.DMS
{
    /// <summary>
    /// Class for interacting with DMS database
    /// </summary>
    public class DMSDBTools : IDisposable
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: subaccount, unallowable, username, usernames, yyyy-MM-dd

        // ReSharper restore CommentTypo

        private DMSDBConnection db;

        public bool ForceValidation => true;

        public string ErrMsg { get; set; } = "";

        public string DMSVersion => db.DMSVersion;

        /// <summary>
        /// Controls whether datasets are loaded when LoadCacheFromDMS() is called
        /// </summary>
        public bool LoadDatasets { get; set; }

        /// <summary>
        /// Controls whether experiments are loaded when LoadCacheFromDMS() is called
        /// </summary>
        public bool LoadExperiments { get; set; }

        /// <summary>
        /// Number of months back to search when reading dataset names
        /// </summary>
        /// <remarks>Default is 12 months; use 0 to load all data</remarks>
        public int RecentDatasetsMonthsToLoad { get; set; }

        /// <summary>
        /// Number of months back to search when reading experiment information
        /// </summary>
        /// <remarks>Default is 18 months; use 0 to load all data</remarks>
        public int RecentExperimentsMonthsToLoad { get; set; }

        /// <summary>
        /// Number of months back to load expired EMSL Proposals; defaults to '12', use '-1' to load all data
        /// </summary>
        public int EMSLProposalsRecentMonthsToLoad { get; set; }

        public bool UseConnectionPooling
        {
            get => db.UseConnectionPooling;
            set => db.UseConnectionPooling = value;
        }

        public event EventHandler<ProgressEventArgs> ProgressEvent;

        public void OnProgressUpdate(ProgressEventArgs e)
        {
            if (ProgressEvent == null)
                Console.WriteLine(e.CurrentTask + ": " + e.PercentComplete);
            else
                ProgressEvent.Invoke(this, e);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DMSDBTools()
        {
            RecentDatasetsMonthsToLoad = 12;
            RecentExperimentsMonthsToLoad = 18;
            EMSLProposalsRecentMonthsToLoad = 12;
            db = new DMSDBConnection();
        }

        /// <summary>
        /// Close the stored SqlConnection
        /// </summary>
        public void CloseConnection()
        {
            db.CloseConnection();
        }

        ~DMSDBTools()
        {
            Dispose();
        }

        public void Dispose()
        {
            CloseConnection();
            GC.SuppressFinalize(this);
        }

        private void ReportProgress(string currentTask, int currentStep, int stepCountTotal)
        {
            var percentComplete = currentStep / (double)stepCountTotal * 100;
            OnProgressUpdate(new ProgressEventArgs(currentTask, percentComplete));
        }

        #region Private Methods: Read from DMS and cache to SQLite

        /// <summary>
        /// Gets a list of Cart Config Names from DMS and stores it in cache
        /// </summary>
        private void GetCartConfigNamesFromDMS()
        {
            try
            {
                var cartConfigs = ReadCartConfigNamesFromDMS();

                // Store the list of cart config names in the cache db
                try
                {
                    SQLiteTools.SaveCartConfigListToCache(cartConfigs);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing LC cart config names in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting cart config list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of Work Packages from DMS and stores it in cache
        /// </summary>
        private void GetWorkPackagesFromDMS()
        {
            try
            {
                var dataFromDms = ReadWorkPackagesFromDMS();

                // Store the list of cart config names in the cache db
                try
                {
                    SQLiteTools.SaveWorkPackageListToCache(dataFromDms);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing work packages in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting work package list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of instrument carts from DMS and stores it in cache
        /// </summary>
        private void GetCartListFromDMS()
        {
            IEnumerable<string> tmpCartList;   // Temp list for holding return values

            // Get a List containing all the carts
            const string sqlCmd = "SELECT DISTINCT cart_name FROM v_lc_cart_active_export " +
                                  "ORDER BY cart_name";
            try
            {
                tmpCartList = db.GetSingleColumnTableFromDMS(sqlCmd);
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting cart list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return;
            }

            // Store the list of carts in the cache db
            try
            {
                SQLiteTools.SaveCartListToCache(tmpCartList);
            }
            catch (Exception ex)
            {
                const string errMsg = "Exception storing LC cart list in cache";
                ApplicationLogger.LogError(0, errMsg, ex);
            }
        }

        private void GetDatasetListFromDMS()
        {
            var sqlCmd = "SELECT dataset FROM v_lcmsnet_dataset_export";

            if (RecentDatasetsMonthsToLoad > 0)
            {
                var dateThreshold = DateTime.Now.AddMonths(-RecentDatasetsMonthsToLoad).ToString("yyyy-MM-dd");
                sqlCmd += " WHERE created >= '" + dateThreshold + "'";
            }

            try
            {
                var datasetList = db.GetSingleColumnTableFromDMS(sqlCmd);

                // Store the data in the cache db
                try
                {
                    SQLiteTools.SaveDatasetNameListToCache(datasetList);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing dataset list in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting dataset list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of active LC columns from DMS and stores in the cache
        /// </summary>
        private void GetColumnListFromDMS()
        {
            IEnumerable<string> tmpColList;    // Temp list for holding return values

            // Get a list of active columns
            const string sqlCmd = "SELECT column_number FROM v_lcmsnet_column_export WHERE state <> 'Retired' ORDER BY column_number";
            try
            {
                tmpColList = db.GetSingleColumnTableFromDMS(sqlCmd);
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting column list";
                //              throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return;
            }

            // Store the list of carts in the cache db
            try
            {
                SQLiteTools.SaveColumnListToCache(tmpColList);
            }
            catch (Exception ex)
            {
                const string errMsg = "Exception storing column list in cache";
                ApplicationLogger.LogError(0, errMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of separation types from DMS and stores it in cache
        /// </summary>
        private void GetSepTypeListFromDMS()
        {
            IEnumerable<string> tmpRetVal; // Temp list for holding separation types

            const string sqlCmd = "SELECT Distinct separation_type FROM v_secondary_sep_export WHERE active > 0 ORDER BY separation_type";

            try
            {
                tmpRetVal = db.GetSingleColumnTableFromDMS(sqlCmd);
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting separation type list";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return;
            }

            // Store data in cache
            try
            {
                SQLiteTools.SaveSeparationTypeListToCache(tmpRetVal);
            }
            catch (Exception ex)
            {
                const string errMsg = "Exception storing separation type list in cache";
                ApplicationLogger.LogError(0, errMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of dataset types from DMS ans stores it in cache
        /// </summary>
        private void GetDatasetTypeListFromDMS()
        {
            IEnumerable<string> tmpRetVal; // Temp list for holding dataset types

            // Get a list of the dataset types
            const string sqlCmd = "SELECT Distinct dataset_type FROM v_dataset_type_name_export ORDER BY dataset_type";
            try
            {
                tmpRetVal = db.GetSingleColumnTableFromDMS(sqlCmd);
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting dataset type list";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return;
            }

            // Store data in cache
            try
            {
                SQLiteTools.SaveDatasetTypeListToCache(tmpRetVal);
            }
            catch (Exception ex)
            {
                const string errMsg = "Exception storing dataset type list in cache";
                ApplicationLogger.LogError(0, errMsg, ex);
            }
        }

        /// <summary>
        /// Obtain the list of instrument operators from DMS and store this list in the cache
        /// </summary>
        private void GetInstrumentOperatorsFromDMS()
        {
            try
            {
                var operators = ReadInstrumentOperatorsFromDMS();

                // Store data in cache
                try
                {
                    SQLiteTools.SaveUserListToCache(operators);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing user list in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting user list";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        private void GetExperimentListFromDMS()
        {
            try
            {
                var experiments = ReadExperimentsFromDMS();

                try
                {
                    SQLiteTools.SaveExperimentListToCache(experiments);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing experiment list in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting experiment list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        /// <summary>
        /// Get EMSL User Proposal IDs and associated users. Uses <see cref="EMSLProposalsRecentMonthsToLoad"/> to control how much data is loaded.
        /// </summary>
        private void GetProposalUsers()
        {
            var users = new List<ProposalUser>();
            var referenceList = new List<UserIDPIDCrossReferenceEntry>();
            var referenceDictionary = new Dictionary<string, List<UserIDPIDCrossReferenceEntry>>();

            try
            {
                // Split the View back into the two tables it was built from.
                // Note: It would be faster if we had the component tables the View was created from.
                var userMap = new Dictionary<int, ProposalUser>();

                foreach (var pUser in ReadProposalUsersFromDMS())
                {
                    if (!pUser.UserId.HasValue || string.IsNullOrWhiteSpace(pUser.ProposalId) || string.IsNullOrWhiteSpace(pUser.UserName))
                        continue;

                    var user = new ProposalUser(pUser.UserId.Value, pUser.UserName);
                    var crossReference = new UserIDPIDCrossReferenceEntry(pUser.UserId.Value, pUser.ProposalId);

                    if (!userMap.ContainsKey(user.UserID))
                    {
                        userMap.Add(user.UserID, user);
                        users.Add(user);
                    }

                    if (!referenceDictionary.ContainsKey(crossReference.PID))
                        referenceDictionary.Add(crossReference.PID, new List<UserIDPIDCrossReferenceEntry>());

                    if (referenceDictionary[crossReference.PID].Any(cr => cr.UserID == crossReference.UserID))
                    {
                        continue;
                    }

                    referenceDictionary[crossReference.PID].Add(crossReference);
                    referenceList.Add(crossReference);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting EUS Proposal Users list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return;
            }

            try
            {
                SQLiteTools.SaveProposalUsers(users, referenceList, referenceDictionary);
            }
            catch (Exception ex)
            {
                const string errMsg = "Exception storing Proposal Users list in cache";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, errMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of instruments from DMS
        /// </summary>
        private void GetInstrumentListFromDMS()
        {
            try
            {
                var instruments = ReadInstrumentFromDMS();

                // Store data in cache
                try
                {
                    SQLiteTools.SaveInstListToCache(instruments);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing instrument list in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }

                var instrumentGroups = ReadInstrumentGroupFromDMS();

                // Store data in cache
                try
                {
                    SQLiteTools.SaveInstGroupListToCache(instrumentGroups);
                }
                catch (Exception ex)
                {
                    const string errMsg = "Exception storing instrument group list in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting instrument list";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        #endregion

        #region Private DMS database read-and-convert methods

        private IEnumerable<CartConfigInfo> ReadCartConfigNamesFromDMS()
        {
            // Get a list containing all active cart configuration names
            const string sqlCmd =
                "SELECT cart_config_name, cart_name " +
                "FROM v_lc_cart_config_export " +
                "WHERE cart_config_state = 'Active' " +
                "ORDER BY cart_name, cart_config_name";

            return db.ExecuteReader(sqlCmd, reader => new CartConfigInfo
            (
                reader["cart_config_name"].CastDBValTo<string>(),
                reader["cart_name"].CastDBValTo<string>()
            ));
        }

        private IEnumerable<ExperimentData> ReadExperimentsFromDMS()
        {
            var sqlCmd = "SELECT id, experiment, created, organism, reason, request, researcher FROM v_lcmsnet_experiment_export";

            if (RecentExperimentsMonthsToLoad > 0)
            {
                var dateThreshold = DateTime.Now.AddMonths(-RecentExperimentsMonthsToLoad).ToString("yyyy-MM-dd");
                sqlCmd += " WHERE last_used >= '" + dateThreshold + "'";
            }

            var deDupDictionary = new Dictionary<string, string>();

            return db.ExecuteReader(sqlCmd, reader => new ExperimentData
            {
                Created = reader["created"].CastDBValTo<DateTime>(),
                Experiment = reader["experiment"].CastDBValTo<string>(),
                ID = reader["id"].CastDBValTo<int>(),
                Organism = reader["organism"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Reason = reader["reason"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Request = reader["request"].CastDBValTo<int>(),
                Researcher = reader["researcher"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary)
            });
        }

        private IEnumerable<InstrumentInfo> ReadInstrumentFromDMS()
        {
            // Get a table containing the instrument data
            const string sqlCmd = "SELECT instrument, name_and_usage, instrument_group, capture_method, " +
                                  "status, host_name, share_path " +
                                  "FROM v_instrument_info_lcmsnet " +
                                  "ORDER BY instrument";

            return db.ExecuteReader(sqlCmd, reader => new InstrumentInfo
            {
                DMSName = reader["instrument"].CastDBValTo<string>(),
                CommonName = reader["name_and_usage"].CastDBValTo<string>(),
                InstrumentGroup = reader["instrument_group"].CastDBValTo<string>(),
                CaptureMethod = reader["capture_method"].CastDBValTo<string>(),
                Status = reader["status"].CastDBValTo<string>(),
                HostName = reader["host_name"].CastDBValTo<string>().Replace(".bionet", ""),
                SharePath = reader["share_path"].CastDBValTo<string>()
            });
        }

        private IEnumerable<InstrumentGroupInfo> ReadInstrumentGroupFromDMS()
        {
            // Get a table containing the instrument data
            const string sqlCmd = "SELECT instrument_group, default_dataset_type, allowed_dataset_types " +
                                  "FROM v_instrument_group_dataset_types_active";

            return db.ExecuteReader(sqlCmd, reader => new InstrumentGroupInfo
            {
                InstrumentGroup = reader["instrument_group"].CastDBValTo<string>(),
                DefaultDatasetType = reader["default_dataset_type"].CastDBValTo<string>(),
                AllowedDatasetTypes = reader["allowed_dataset_types"].CastDBValTo<string>()
            });
        }

        private readonly struct DmsProposalUserEntry
        {
            public readonly int? UserId;
            public readonly string UserName;
            public readonly string ProposalId;

            public DmsProposalUserEntry(int? userId, string userName, string proposalId)
            {
                UserId = userId;
                UserName = userName;
                ProposalId = proposalId;
            }
        }

        private IEnumerable<DmsProposalUserEntry> ReadProposalUsersFromDMS()
        {
            const string sqlCmdStart = "SELECT user_id, user_name, proposal FROM v_eus_proposal_users";
            var sqlCmd = sqlCmdStart;
            if (EMSLProposalsRecentMonthsToLoad > -1)
            {
                var oldestExpiration = DateTime.Now.AddMonths(-EMSLProposalsRecentMonthsToLoad);
                sqlCmd += $" WHERE proposal_end_date >= '{oldestExpiration:yyyy-MM-dd}' OR proposal_end_date IS NULL";
            }

            return db.ExecuteReader(sqlCmd, reader => new DmsProposalUserEntry
            (
                reader["user_id"].CastDBValTo<int?>(),
                reader["user_name"].CastDBValTo<string>(),
                reader["proposal"].CastDBValTo<string>()
            ));
        }

        private IEnumerable<DMSData> ReadRequestedRunsFromDMS()
        {
            const string sqlCmd = "SELECT request, name, instrument, type, experiment, comment, work_package, cart, usage_type, eus_users, proposal_id FROM v_requested_run_active_export ORDER BY name";

            var deDupDictionary = new Dictionary<string, string>();

            return db.ExecuteReader(sqlCmd, reader => new DMSData
            {
                DatasetType = reader["type"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Experiment = reader["experiment"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                EMSLProposalID = reader["proposal_id"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                RequestID = reader["request"].CastDBValTo<int>(),
                RequestName = reader["name"].CastDBValTo<string>(),
                InstrumentGroup = reader["instrument"].CastDBValTo<string>(),
                WorkPackage = reader["work_package"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                EMSLUsageType = reader["usage_type"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                EMSLProposalUser = reader["eus_users"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                CartName = reader["cart"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Comment = reader["comment"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
            });
        }

        private IEnumerable<UserInfo> ReadInstrumentOperatorsFromDMS()
        {
            // Get the instrument operator names and usernames
            // Switched from V_Active_Users to V_Active_Instrument_Operators in January 2020
            // Switched from V_Active_Instrument_Operators to V_Active_Instrument_Users in October 2021
            // Note that EMSL Users have a separate list
            const string sqlCmd = "SELECT name, username FROM v_active_instrument_users ORDER BY name";

            return db.ExecuteReader(sqlCmd, reader => new UserInfo
            {
                Name = reader["name"].CastDBValTo<string>(),
                Id = reader["username"].CastDBValTo<string>()
            });
        }

        private IEnumerable<WorkPackageInfo> ReadWorkPackagesFromDMS()
        {
            // Get a list containing all active work packages

            // Filters:
            // * Only get the last 6 years
            // * None from an 'unallowable' subaccount
            // * None that are inactive and never used
            // * None that have not been used, where the owner name is unknown (not in DMS)
            var sqlCmd =
                "SELECT charge_code, state, sub_account, work_breakdown_structure, title, owner_username, owner_name " +
                "FROM v_charge_code_export " +
                $"WHERE setup_date > '{DateTime.Now.AddYears(-6):yyyy-MM-dd}' AND sub_account NOT LIKE '%UNALLOWABLE%' AND state <> 'Inactive, unused' AND (state LIKE '%, used%' OR owner_name IS NOT NULL)" +
                "ORDER BY sort_key";

            var deDupDictionary = new Dictionary<string, string>();

            return db.ExecuteReader(sqlCmd, reader => new WorkPackageInfo
            (
                reader["charge_code"].CastDBValTo<string>()?.Trim(),
                reader["state"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["sub_account"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["work_breakdown_structure"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["title"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["owner_username"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["owner_name"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary)
            ));
        }

        #endregion

        /// <summary>
        /// Test if we can query each of the needed DMS tables/views.
        /// </summary>
        /// <returns></returns>
        public bool CheckDMSConnection()
        {
            return db.CheckDMSConnection();
        }

        /// <summary>
        /// Loads all DMS data into cache
        /// </summary>
        public void LoadCacheFromDMS()
        {
            LoadCacheFromDMS(LoadExperiments, LoadDatasets);
        }

        public void LoadCacheFromDMS(bool loadExperiments)
        {
            ReportProgress("Loading data from DMS (entering LoadCacheFromDMS(boolean loadExperiments)", 0, 20);
            LoadCacheFromDMS(loadExperiments, LoadDatasets);
        }

        public void LoadCacheFromDMS(bool loadExperiments, bool loadDatasets)
        {
            const int STEP_COUNT_BASE = 11;
            const int EXPERIMENT_STEPS = 20;
            const int DATASET_STEPS = 50;

            var stepCountTotal = STEP_COUNT_BASE;

            if (loadExperiments)
                stepCountTotal += EXPERIMENT_STEPS;

            if (loadDatasets)
                stepCountTotal += DATASET_STEPS;

            ReportProgress("Loading data from DMS (determining Connection String)", 0, stepCountTotal);

            var sqLiteConnectionString = SQLiteTools.ConnString;
            var equalsIndex = sqLiteConnectionString.IndexOf('=');
            string cacheFilePath;

            if (equalsIndex > 0 && equalsIndex < sqLiteConnectionString.Length - 1)
                cacheFilePath = "SQLite cache file path: " + sqLiteConnectionString.Substring(equalsIndex + 1);
            else
                cacheFilePath = "SQLite cache file path: " + sqLiteConnectionString;

            var dmsConnectionString = db.GetConnectionString();

            // Remove the password from the connection string
            var passwordStartIndex = dmsConnectionString.IndexOf(";Password", StringComparison.InvariantCultureIgnoreCase);
            if (passwordStartIndex > 0)
                dmsConnectionString = dmsConnectionString.Substring(0, passwordStartIndex);

            ReportProgress("Loading data from DMS (" + dmsConnectionString + ") and storing in " + cacheFilePath, 0, stepCountTotal);

            ReportProgress("Loading cart names", 1, stepCountTotal);
            GetCartListFromDMS();

            ReportProgress("Loading cart config names", 2, stepCountTotal);
            GetCartConfigNamesFromDMS();

            ReportProgress("Loading separation types", 3, stepCountTotal);
            GetSepTypeListFromDMS();

            ReportProgress("Loading dataset types", 4, stepCountTotal);
            GetDatasetTypeListFromDMS();

            ReportProgress("Loading instruments", 5, stepCountTotal);
            GetInstrumentListFromDMS();

            ReportProgress("Loading work packages", 6, stepCountTotal);
            GetWorkPackagesFromDMS();

            ReportProgress("Loading operators", 7, stepCountTotal);
            GetInstrumentOperatorsFromDMS();

            ReportProgress("Loading LC columns", 8, stepCountTotal);
            GetColumnListFromDMS();

            ReportProgress("Loading proposal users", 9, stepCountTotal);
            GetProposalUsers();

            var stepCountCompleted = STEP_COUNT_BASE;
            if (loadExperiments)
            {
                var currentTask = "Loading experiments";
                if (RecentExperimentsMonthsToLoad > 0)
                    currentTask += " created/used in the last " + RecentExperimentsMonthsToLoad + " months";

                ReportProgress(currentTask, stepCountCompleted, stepCountTotal);

                GetExperimentListFromDMS();
                stepCountCompleted += EXPERIMENT_STEPS;
            }

            if (loadDatasets)
            {
                var currentTask = "Loading datasets";
                if (RecentDatasetsMonthsToLoad > 0)
                    currentTask += " from the last " + RecentDatasetsMonthsToLoad + " months";

                ReportProgress(currentTask, stepCountCompleted, stepCountTotal);

                GetDatasetListFromDMS();
                // stepCountCompleted += DATASET_STEPS;
            }

            ReportProgress("DMS data loading complete", stepCountTotal, stepCountTotal);
        }

        /// <summary>
        /// Gets a list of samples (essentially requested runs) from DMS
        /// </summary>
        /// <remarks>Retrieves data from view V_Requested_Run_Active_Export</remarks>
        public IEnumerable<DMSData> GetRequestedRunsFromDMS()
        {
            try
            {
                return ReadRequestedRunsFromDMS();
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting run request list";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return Enumerable.Empty<DMSData>();
            }
        }
    }
}
