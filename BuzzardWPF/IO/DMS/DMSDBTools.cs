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
        private readonly DMSDBReader dbReader;

        public bool ForceValidation => true;

        public string ErrMsg { get; set; } = "";

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
            dbReader = new DMSDBReader();
        }

        /// <summary>
        /// Close the stored SqlConnection
        /// </summary>
        public void CloseConnection()
        {
            dbReader.CloseConnection();
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

        /// <summary>
        /// Test if we can query each of the needed DMS tables/views.
        /// </summary>
        /// <returns></returns>
        public bool CheckDMSConnection()
        {
            return dbReader.CheckDMSConnection();
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

            dbReader.RefreshConnectionConfiguration();
            var dmsConnectionString = dbReader.GetConnectionString();

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
                dbReader.RefreshConnectionConfiguration();
                return dbReader.ReadRequestedRuns();
            }
            catch (Exception ex)
            {
                ErrMsg = "Exception getting run request list";
                //                  throw new DatabaseDataException(ErrMsg, ex);
                ApplicationLogger.LogError(0, ErrMsg, ex);
                return Enumerable.Empty<DMSData>();
            }
        }

        private void CacheDMSList<T>(Func<T> dmsReadMethod, Action<T> cacheMethod, string listNameForErrors)
        {
            try
            {
                var dataList = dmsReadMethod();

                try
                {
                    cacheMethod(dataList);
                }
                catch (Exception ex)
                {
                    var errMsg = $"Exception storing {listNameForErrors} list in cache";
                    ApplicationLogger.LogError(0, errMsg, ex);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = $"Exception getting {listNameForErrors} list";
                ApplicationLogger.LogError(0, ErrMsg, ex);
            }
        }

        /// <summary>
        /// Gets a list of Cart Config Names from DMS and stores it in cache
        /// </summary>
        private void GetCartConfigNamesFromDMS()
        {
            CacheDMSList(dbReader.ReadCartConfigNames, SQLiteTools.SaveCartConfigListToCache, "LC Cart config");
        }

        /// <summary>
        /// Gets a list of Work Packages from DMS and stores it in cache
        /// </summary>
        private void GetWorkPackagesFromDMS()
        {
            CacheDMSList(dbReader.ReadWorkPackages, SQLiteTools.SaveWorkPackageListToCache, "work package");
        }

        /// <summary>
        /// Gets a list of instrument carts from DMS and stores it in cache
        /// </summary>
        private void GetCartListFromDMS()
        {
            CacheDMSList(dbReader.ReadCartList, SQLiteTools.SaveCartListToCache, "LC Cart");
        }

        private void GetDatasetListFromDMS()
        {
            CacheDMSList(() => dbReader.ReadDatasetList(RecentDatasetsMonthsToLoad), SQLiteTools.SaveDatasetNameListToCache, "dataset");
        }

        /// <summary>
        /// Gets a list of active LC columns from DMS and stores in the cache
        /// </summary>
        private void GetColumnListFromDMS()
        {
            CacheDMSList(dbReader.ReadColumnList, SQLiteTools.SaveColumnListToCache, "column");
        }

        /// <summary>
        /// Gets a list of separation types from DMS and stores it in cache
        /// </summary>
        private void GetSepTypeListFromDMS()
        {
            CacheDMSList(dbReader.ReadSeparationTypeList, SQLiteTools.SaveSeparationTypeListToCache, "separation type");
        }

        /// <summary>
        /// Gets a list of dataset types from DMS ans stores it in cache
        /// </summary>
        private void GetDatasetTypeListFromDMS()
        {
            CacheDMSList(dbReader.ReadDatasetTypeList, SQLiteTools.SaveDatasetTypeListToCache, "dataset type");
        }

        /// <summary>
        /// Obtain the list of instrument operators from DMS and store this list in the cache
        /// </summary>
        private void GetInstrumentOperatorsFromDMS()
        {
            CacheDMSList(dbReader.ReadInstrumentOperators, SQLiteTools.SaveUserListToCache, "user");
        }

        private void GetExperimentListFromDMS()
        {
            CacheDMSList(() => dbReader.ReadExperiments(RecentExperimentsMonthsToLoad), SQLiteTools.SaveExperimentListToCache, "experiment");
        }

        /// <summary>
        /// Gets a list of instruments from DMS
        /// </summary>
        private void GetInstrumentListFromDMS()
        {
            CacheDMSList(dbReader.ReadInstrument, SQLiteTools.SaveInstListToCache, "instrument");
            CacheDMSList(dbReader.ReadInstrumentGroup, SQLiteTools.SaveInstGroupListToCache, "instrument group");
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

                foreach (var pUser in dbReader.ReadProposalUsers())
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
    }
}
