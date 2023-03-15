using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
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

        // Ignore Spelling: DMSPwd, ini, subaccount, unallowable, username, usernames, utf, xmlns, xs, yyyy-MM-dd

        // ReSharper restore CommentTypo

        public static string ApplicationName { get; set; } = "-LcmsNetDmsTools- -version-";

        private bool mConnectionStringLogged;

        /// <summary>
        /// Key to access the DMS version string in the configuration dictionary.
        /// </summary>
        private const string CONST_DMS_SERVER_KEY = "DMSServer";

        /// <summary>
        /// Key to access the DMS version string in the configuration dictionary.
        /// </summary>
        /// <remarks>This is the name of the database to connect to</remarks>
        private const string CONST_DMS_VERSION_KEY = "DMSVersion";

        /// <summary>
        /// Key to access the encoded DMS password string in the configuration dictionary.
        /// </summary>
        /// <remarks>This is the password of SQL Server user LCMSNetUser</remarks>
        private const string CONST_DMS_PASSWORD_KEY = "DMSPwd";

        private const string CONFIG_FILE = "PrismDMS.config";

        public bool ForceValidation => true;

        public string ErrMsg { get; set; } = "";

        public string DMSVersion => GetConfigSetting(CONST_DMS_VERSION_KEY, "UnknownVersion");

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

        public bool UseConnectionPooling { get; set; }

        private readonly Dictionary<string,string> mConfiguration;

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
            mConfiguration = new Dictionary<string, string>();
            RecentDatasetsMonthsToLoad = 12;
            RecentExperimentsMonthsToLoad = 18;
            EMSLProposalsRecentMonthsToLoad = 12;
            LoadConfiguration();
            // This should generally be true for SqlClient/SqlConnection, false means connection reuse (and potential multi-threading problems)
            UseConnectionPooling = true;
        }

        private SqlConnection connection;
        private string lastConnectionString = "";
        private DateTime lastConnectionAttempt = DateTime.MinValue;
        private readonly TimeSpan minTimeBetweenConnectionAttempts = TimeSpan.FromSeconds(30);
        private readonly TimeSpan connectionTimeoutTime = TimeSpan.FromSeconds(60);
        private Timer connectionTimeoutTimer;
        private string failedConnectionAttemptMessage = "";

        private void ConnectionTimeoutActions(object sender)
        {
            CloseConnection();
        }

        /// <summary>
        /// Close the stored SqlConnection
        /// </summary>
        public void CloseConnection()
        {
            try
            {
                connection?.Close();
                connection?.Dispose();
                connectionTimeoutTimer?.Dispose();
                connection = null;
            }
            catch
            {
                // Swallow any exceptions that occurred...
            }
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

        /// <summary>
        /// Get a SQLiteConnection, but control creation of new connections based on UseConnectionPooling
        /// </summary>
        /// <param name="connString"></param>
        /// <returns></returns>
        private SqlConnectionWrapper GetConnection(string connString)
        {
            // Reset out the close timer with every use
            connectionTimeoutTimer?.Dispose();
            connectionTimeoutTimer = new Timer(ConnectionTimeoutActions, this, connectionTimeoutTime, TimeSpan.FromMilliseconds(-1));

            var newServer = false;
            if (!lastConnectionString.Equals(connString))
            {
                CloseConnection();
                newServer = true;
            }

            if (connection == null && (DateTime.UtcNow > lastConnectionAttempt.Add(minTimeBetweenConnectionAttempts) || newServer))
            {
                lastConnectionString = connString;
                lastConnectionAttempt = DateTime.UtcNow;
                try
                {
                    var cn = new SqlConnection(connString);
                    cn.Open();
                    connection = cn;
                    failedConnectionAttemptMessage = "";
                }
                catch (Exception e)
                {
                    failedConnectionAttemptMessage = $"Error connecting to database; Please check network connections and try again. Exception message: {e.Message}";
                    ErrMsg = failedConnectionAttemptMessage;
                    ApplicationLogger.LogError(0, failedConnectionAttemptMessage);
                }
            }

            if (UseConnectionPooling && connection != null)
            {
                // MSSQL/SqlConnection connection pooling: handled transparently based on connection strings
                // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-connection-pooling
                connection.Close();
                return new SqlConnectionWrapper(connString);
            }

            return new SqlConnectionWrapper(connection, failedConnectionAttemptMessage);
        }

        /// <summary>
        /// Loads DMS configuration from file
        /// </summary>
        private void LoadConfiguration()
        {
            var readerSettings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema
            };
            readerSettings.ValidationEventHandler += SettingsValidationEventHandler;

            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new DirectoryNotFoundException("Directory for the executing assembly is empty; unable to load the configuration in DMSDBTools");
            }

            var configurationPath = Path.Combine(folderPath, CONFIG_FILE);
            if (!File.Exists(configurationPath))
            {
                CreateDefaultConfigFile(configurationPath);
            }

            var reader = XmlReader.Create(configurationPath, readerSettings);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (string.Equals(reader.GetAttribute("DmsSetting"), "true", StringComparison.OrdinalIgnoreCase))
                        {
                            var settingName = reader.Name.Remove(0, 2);
                            // Add/update the configuration item
                            mConfiguration[settingName] = reader.ReadString();
                        }
                        break;
                }
            }
        }

        private void SettingsValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                throw new InvalidOperationException(e.Message, e.Exception);
            }

            ApplicationLogger.LogMessage(ApplicationLogger.CONST_STATUS_LEVEL_CRITICAL, "DmsTools Configuration warning: " + e.Message);
        }

        /// <summary>
        /// Lookup the value for the given setting
        /// </summary>
        /// <param name="configName">Setting name</param>
        /// <param name="valueIfMissing">Value to return if configName is not defined in mConfiguration</param>
        /// <returns></returns>
        private string GetConfigSetting(string configName, string valueIfMissing)
        {
            if (mConfiguration.TryGetValue(configName, out var configValue))
            {
                return configValue;
            }
            return valueIfMissing;
        }

        /// <summary>
        /// Gets DMS connection string from config file
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString()
        {
            // Construct the connection string, for example:
            // Data Source=Gigasax;Initial Catalog=DMS5;User ID=LCMSNetUser;Password=ThePassword"

            // ToDo: update this to construct a Postgres connection string

            var retStr = "Data Source=";

            // Get the DMS Server name
            var dmsServer = GetConfigSetting(CONST_DMS_SERVER_KEY, "Gigasax");
            if (dmsServer != null)
            {
                retStr += dmsServer;
            }
            else
            {
                retStr += "Gigasax";
            }

            // Get name of the DMS database to use
            var dmsVersion = GetConfigSetting(CONST_DMS_VERSION_KEY, "DMS5");
            if (dmsVersion != null)
            {
                retStr += ";Initial Catalog=" + dmsVersion + ";User ID=LCMSNetUser";
            }
            else
            {
                throw new DatabaseConnectionStringException(
                    "DMS version string not found in configuration file (this parameter is the " +
                    "name of the database to connect to).  Delete the " + CONFIG_FILE + " file and " +
                    "it will be automatically re-created with the default values.");
            }

            if (!string.IsNullOrWhiteSpace(ApplicationName))
            {
                retStr += $";Application Name={ApplicationName}";
            }

            if (!mConnectionStringLogged)
            {
                ApplicationLogger.LogMessage(ApplicationLogger.CONST_STATUS_LEVEL_DETAILED,
                                                  "Database connection string: " + retStr + ";Password=....");
                mConnectionStringLogged = true;
            }

            // Get the password for user LCMSNetUser
            var dmsPassword = GetConfigSetting(CONST_DMS_PASSWORD_KEY, "Mprptq3v");
            if (dmsPassword != null)
            {
                retStr += ";Password=" + DecodePassword(dmsPassword);
            }
            else
            {
                throw new DatabaseConnectionStringException(
                    "DMS password string not found in configuration file (this is the password " +
                    "for the LCMSOperator username.  Delete the " + CONFIG_FILE + " file and " +
                    "it will be automatically re-created with the default values.");
            }

            return retStr;
        }

        /// <summary>
        /// Generic method to retrieve data from a single-column table in DMS
        /// </summary>
        /// <param name="cmdStr">SQL command to execute</param>
        /// <param name="connStr">Database connection string</param>
        /// <returns>List containing the table's contents</returns>
        private IEnumerable<string> GetSingleColumnTableFromDMS(string cmdStr, string connStr)
        {
            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = cmdStr;
                cmd.CommandType = CommandType.Text;

                SqlDataReader reader;

                // Get a table from the database
                try
                {
                    reader = cmd.ExecuteReader();
                }
                catch (Exception ex)
                {
                    ErrMsg = "Exception getting single column table via command: " + cmdStr;
                    //                  throw new DatabaseDataException(ErrMsg, ex);
                    ApplicationLogger.LogError(0, ErrMsg, ex);
                    throw new Exception(ErrMsg, ex);
                }

                using (reader)
                {
                    // Copy the table contents into the list
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a data table from DMS
        /// </summary>
        /// <param name="cmdStr">SQL command to retrieve table</param>
        /// <param name="connStr">DMS connection string</param>
        /// <returns>DataTable containing requested data</returns>
        /// <remarks>This tends to use more memory than directly reading and parsing data.</remarks>
        [Obsolete("Unused")]
        // ReSharper disable once UnusedMember.Local
        private DataTable GetDataTable(string cmdStr, string connStr)
        {
            var returnTable = new DataTable();
            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var da = new SqlDataAdapter())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = cmdStr;
                cmd.CommandType = CommandType.Text;
                da.SelectCommand = cmd;
                try
                {
                    da.Fill(returnTable);
                }
                catch (Exception ex)
                {
                    var errMsg = "SQL exception getting data table via query " + cmdStr;
                    ApplicationLogger.LogError(0, errMsg, ex);
                    throw new DatabaseDataException(errMsg, ex);
                }
            }

            // Return the output table
            return returnTable;
        }

        private static void CreateDefaultConfigFile(string configurationPath)
        {
            // Create a new file with default config data
            using (var writer = new StreamWriter(new FileStream(configurationPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<catalog>");
                writer.WriteLine("  <!-- DMS Configuration Schema definition -->");
                writer.WriteLine("  <xs:schema elementFormDefault=\"qualified\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"PrismDMS\"> ");
                writer.WriteLine("    <xs:element name=\"PrismDMSConfig\">");
                writer.WriteLine("      <xs:complexType><xs:sequence>");
                writer.WriteLine("          <xs:element name=\"DMSServer\" minOccurs=\"0\" maxOccurs=\"1\">");
                writer.WriteLine("             <xs:complexType><xs:simpleContent><xs:extension base=\"xs:string\">");
                writer.WriteLine("                  <xs:attribute name=\"dmssetting\" use=\"optional\" type=\"xs:string\"/>");
                writer.WriteLine("             </xs:extension></xs:simpleContent></xs:complexType>");
                writer.WriteLine("          </xs:element>");
                writer.WriteLine("          <xs:element name=\"DMSVersion\">");
                writer.WriteLine("             <xs:complexType><xs:simpleContent><xs:extension base=\"xs:string\">");
                writer.WriteLine("                  <xs:attribute name=\"dmssetting\" use=\"required\" type=\"xs:string\"/>                 ");
                writer.WriteLine("             </xs:extension></xs:simpleContent></xs:complexType>");
                writer.WriteLine("          </xs:element>");
                writer.WriteLine("          <xs:element name=\"DMSPwd\">");
                writer.WriteLine("             <xs:complexType><xs:simpleContent><xs:extension base=\"xs:string\">");
                writer.WriteLine("                  <xs:attribute name=\"dmssetting\" use=\"required\" type=\"xs:string\"/>");
                writer.WriteLine("             </xs:extension></xs:simpleContent></xs:complexType>");
                writer.WriteLine("          </xs:element>                 ");
                writer.WriteLine("      </xs:sequence></xs:complexType>");
                writer.WriteLine("    </xs:element>");
                writer.WriteLine("  </xs:schema>");
                writer.WriteLine(" ");
                writer.WriteLine("  <!-- DMS configuration -->");
                writer.WriteLine("  <p:PrismDMSConfig xmlns:p=\"PrismDMS\">");
                writer.WriteLine("    <!-- Server hosting DMS (defaults to Gigasax if missing) -->");
                writer.WriteLine("    <p:DMSServer dmssetting=\"true\">Gigasax</p:DMSServer>");
                writer.WriteLine("    <!-- DMSVersion is the name of the database to connect to -->");
                writer.WriteLine("    <p:DMSVersion dmssetting=\"true\">DMS5</p:DMSVersion>");
                writer.WriteLine("    <!-- DMSPwd is the encoded DMS password for SQL server user LCMSNetUser -->");
                writer.WriteLine("    <p:DMSPwd dmssetting=\"true\">Mprptq3v</p:DMSPwd>");
                writer.WriteLine("  </p:PrismDMSConfig>");
                writer.WriteLine("</catalog>");
            }
        }

        /// <summary>
        /// Decrypts password received from ini file
        /// </summary>
        /// <param name="enPwd">Encoded password</param>
        /// <returns>Clear text password</returns>
        private static string DecodePassword(string enPwd)
        {
            // Decrypts password received from ini file
            // Password was created by alternately subtracting or adding 1 to the ASCII value of each character

            // Convert the password string to a character array
            var pwdChars = enPwd.ToCharArray();
            var pwdBytes = new byte[pwdChars.Length];
            var pwdCharsAdj = new char[pwdChars.Length];

            for (var i = 0; i < pwdChars.Length; i++)
            {
                pwdBytes[i] = (byte)pwdChars[i];
            }

            // Modify the byte array by shifting alternating bytes up or down and convert back to char, and add to output string
            var retStr = "";
            for (var byteCounter = 0; byteCounter < pwdBytes.Length; byteCounter++)
            {
                if (byteCounter % 2 == 0)
                {
                    pwdBytes[byteCounter]++;
                }
                else
                {
                    pwdBytes[byteCounter]--;
                }
                pwdCharsAdj[byteCounter] = (char)pwdBytes[byteCounter];
                retStr += pwdCharsAdj[byteCounter].ToString(CultureInfo.InvariantCulture);
            }
            return retStr;
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
            var connStr = GetConnectionString();

            // Get a List containing all the carts
            const string sqlCmd = "SELECT DISTINCT cart_name FROM v_lc_cart_active_export " +
                                  "ORDER BY cart_name";
            try
            {
                tmpCartList = GetSingleColumnTableFromDMS(sqlCmd, connStr);
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
            var connStr = GetConnectionString();

            var sqlCmd = "SELECT dataset FROM v_lcmsnet_dataset_export";

            if (RecentDatasetsMonthsToLoad > 0)
            {
                var dateThreshold = DateTime.Now.AddMonths(-RecentDatasetsMonthsToLoad).ToString("yyyy-MM-dd");
                sqlCmd += " WHERE created >= '" + dateThreshold + "'";
            }

            try
            {
                var datasetList = GetSingleColumnTableFromDMS(sqlCmd, connStr);

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
            var connStr = GetConnectionString();

            // Get a list of active columns
            const string sqlCmd = "SELECT column_number FROM v_lcmsnet_column_export WHERE state <> 'Retired' ORDER BY column_number";
            try
            {
                tmpColList = GetSingleColumnTableFromDMS(sqlCmd, connStr);
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
            var connStr = GetConnectionString();

            const string sqlCmd = "SELECT Distinct separation_type FROM v_secondary_sep_export WHERE active > 0 ORDER BY separation_type";

            try
            {
                tmpRetVal = GetSingleColumnTableFromDMS(sqlCmd, connStr);
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
            var connStr = GetConnectionString();

            // Get a list of the dataset types
            const string sqlCmd = "SELECT Distinct dataset_type FROM v_dataset_type_name_export ORDER BY dataset_type";
            try
            {
                tmpRetVal = GetSingleColumnTableFromDMS(sqlCmd, connStr);
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
            var connStr = GetConnectionString();

            // Get a list containing all active cart configuration names
            const string sqlCmd =
                "SELECT cart_config_name, cart_name " +
                "FROM v_lc_cart_config_export " +
                "WHERE cart_config_state = 'Active' " +
                "ORDER BY cart_name, cart_config_name";

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new CartConfigInfo(
                            reader["cart_config_name"].CastDBValTo<string>(),
                            reader["cart_name"].CastDBValTo<string>());
                    }
                }
            }
        }

        private IEnumerable<ExperimentData> ReadExperimentsFromDMS()
        {
            var connStr = GetConnectionString();

            var sqlCmd = "SELECT id, experiment, created, organism, reason, request, researcher FROM v_lcmsnet_experiment_export";

            if (RecentExperimentsMonthsToLoad > 0)
            {
                var dateThreshold = DateTime.Now.AddMonths(-RecentExperimentsMonthsToLoad).ToString("yyyy-MM-dd");
                sqlCmd += " WHERE last_used >= '" + dateThreshold + "'";
            }

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            var deDupDictionary = new Dictionary<string, string>();

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new ExperimentData
                        {
                            Created = reader["created"].CastDBValTo<DateTime>(),
                            Experiment = reader["experiment"].CastDBValTo<string>(),
                            ID = reader["id"].CastDBValTo<int>(),
                            Organism = reader["organism"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                            Reason = reader["reason"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                            Request = reader["request"].CastDBValTo<int>(),
                            Researcher = reader["researcher"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary)
                        };
                    }
                }
            }
        }

        private IEnumerable<InstrumentInfo> ReadInstrumentFromDMS()
        {
            var connStr = GetConnectionString();

            // Get a table containing the instrument data
            const string sqlCmd = "SELECT instrument, name_and_usage, instrument_group, capture_method, " +
                                  "status, host_name, share_path " +
                                  "FROM v_instrument_info_lcmsnet " +
                                  "ORDER BY instrument";

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new InstrumentInfo
                        {
                            DMSName = reader["instrument"].CastDBValTo<string>(),
                            CommonName = reader["name_and_usage"].CastDBValTo<string>(),
                            InstrumentGroup = reader["instrument_group"].CastDBValTo<string>(),
                            CaptureMethod = reader["capture_method"].CastDBValTo<string>(),
                            Status = reader["status"].CastDBValTo<string>(),
                            HostName = reader["host_name"].CastDBValTo<string>().Replace(".bionet", ""),
                            SharePath = reader["share_path"].CastDBValTo<string>()
                        };
                    }
                }
            }
        }

        private IEnumerable<InstrumentGroupInfo> ReadInstrumentGroupFromDMS()
        {
            var connStr = GetConnectionString();

            // Get a table containing the instrument data
            const string sqlCmd = "SELECT instrument_group, default_dataset_type, allowed_dataset_types " +
                                  "FROM v_instrument_group_dataset_types_active";

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new InstrumentGroupInfo
                        {
                            InstrumentGroup = reader["instrument_group"].CastDBValTo<string>(),
                            DefaultDatasetType = reader["default_dataset_type"].CastDBValTo<string>(),
                            AllowedDatasetTypes = reader["allowed_dataset_types"].CastDBValTo<string>()
                        };
                    }
                }
            }
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
            var connStr = GetConnectionString();

            const string sqlCmdStart = "SELECT user_id, user_name, proposal FROM v_eus_proposal_users";
            var sqlCmd = sqlCmdStart;
            if (EMSLProposalsRecentMonthsToLoad > -1)
            {
                var oldestExpiration = DateTime.Now.AddMonths(-EMSLProposalsRecentMonthsToLoad);
                sqlCmd += $" WHERE proposal_end_date >= '{oldestExpiration:yyyy-MM-dd}' OR proposal_end_date IS NULL";
            }

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new DmsProposalUserEntry
                        (
                            reader["user_id"].CastDBValTo<int?>(),
                            reader["user_name"].CastDBValTo<string>(),
                            reader["proposal"].CastDBValTo<string>()
                        );
                    }
                }
            }
        }

        private IEnumerable<DMSData> ReadRequestedRunsFromDMS()
        {
            var connStr = GetConnectionString();
            const string sqlCmd = "SELECT request, name, instrument, type, experiment, comment, work_package, cart, usage_type, eus_users, proposal_id FROM v_requested_run_active_export ORDER BY name";

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            var deDupDictionary = new Dictionary<string, string>();

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tmpDMSData = new DMSData
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
                        };

                        yield return tmpDMSData;
                    }
                }
            }
        }

        private IEnumerable<UserInfo> ReadInstrumentOperatorsFromDMS()
        {
            var connStr = GetConnectionString();

            // Get the instrument operator names and usernames
            // Switched from V_Active_Users to V_Active_Instrument_Operators in January 2020
            // Switched from V_Active_Instrument_Operators to V_Active_Instrument_Users in October 2021
            // Note that EMSL Users have a separate list
            const string sqlCmd = "SELECT name, username FROM v_active_instrument_users ORDER BY name";

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new UserInfo
                        {
                            Name = reader["name"].CastDBValTo<string>(),
                            Id = reader["username"].CastDBValTo<string>()
                        };
                    }
                }
            }
        }

        private IEnumerable<WorkPackageInfo> ReadWorkPackagesFromDMS()
        {
            var connStr = GetConnectionString();

            // Get a list containing all active work packages

            // Filters:
            // * Only get the last 6 years
            // * None from an 'unallowable' subaccount
            // * None that are inactive and never used
            // * None that have not been used, where the owner name is unknown (not in DMS)
            var sqlCmd =
                "SELECT charge_code, state, sub_account, work_breakdown_structure, title, owner_prn, owner_name " +
                "FROM v_charge_code_export " +
                $"WHERE setup_date > '{DateTime.Now.AddYears(-6):yyyy-MM-dd}' AND sub_account NOT LIKE '%UNALLOWABLE%' AND state <> 'Inactive, unused' AND (state LIKE '%, used%' OR owner_name IS NOT NULL)" +
                "ORDER BY sort_key";

            var cn = GetConnection(connStr);
            if (!cn.IsValid)
            {
                cn.Dispose();
                throw new Exception(cn.FailedConnectionAttemptMessage);
            }

            var deDupDictionary = new Dictionary<string, string>();

            using (cn)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = sqlCmd;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new WorkPackageInfo(
                            reader["charge_code"].CastDBValTo<string>()?.Trim(),
                            reader["state"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                            reader["sub_account"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                            reader["work_breakdown_structure"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                            reader["title"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                            reader["owner_prn"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                            reader["owner_name"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary));
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Test if we can query each of the needed DMS tables/views.
        /// </summary>
        /// <returns></returns>
        public bool CheckDMSConnection()
        {
            try
            {
                var connStr = GetConnectionString();

                using (var conn = GetConnection(connStr))

                // Test getting 1 row from every table we query?...
                using (var cmd = conn.CreateCommand())
                {
                    // Keys in this dictionary are view names, values are the column to use when ranking rows using Row_number()
                    var viewInfo = new Dictionary<string, string>
                    {
                        { "v_lc_cart_config_export", "Cart_Config_ID" },
                        { "v_charge_code_export", "Charge_Code" },
                        { "v_lc_cart_active_export", "ID" },
                        { "v_lcmsnet_dataset_export", "ID" },
                        { "v_lcmsnet_column_export", "ID" },
                        { "v_secondary_sep_export", "Separation_Type_ID" },
                        { "v_dataset_type_name_export", "Dataset_Type_ID" },
                        { "v_active_instrument_users", "Username" },
                        { "v_lcmsnet_experiment_export", "ID" },
                        { "v_eus_proposal_users", "user_id" },
                        { "v_instrument_info_lcmsnet", "Instrument" },
                        { "v_requested_run_active_export", "Request" },
                        { "v_instrument_group_dataset_types_active", "Instrument_Group" }
                    };

                    foreach (var item in viewInfo)
                    {
                        cmd.CommandText = $"SELECT RowNum FROM (SELECT Row_number() Over (ORDER BY {item.Value}) AS RowNum FROM {item.Key}) RankQ WHERE RowNum = 1;";
                        cmd.ExecuteScalar(); // TODO: Test the returned value? (for what?)
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, "Failed to test read a needed table!", ex);
                return false;
            }

            return true;
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

            var dmsConnectionString = GetConnectionString();

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
