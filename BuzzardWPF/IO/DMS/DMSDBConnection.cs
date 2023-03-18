using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using BuzzardWPF.Logging;
using BuzzardWPF.Utility;
using PRISM;

namespace BuzzardWPF.IO.DMS
{
    internal class DMSDBConnection : IDisposable
    {
        public static string ApplicationName { get; set; } = "-Buzzard- -version-";

        private bool mConnectionStringLogged;

        private const string CONFIG_FILE = "PrismDMS.json";
        private const string CENTRAL_CONFIG_FILE_PATH = @"\\proto-5\BionetSoftware\Buzzard\PrismDMS.json";

        public string ErrMsg { get; set; } = "";

        public string DMSVersion => mConfiguration.DatabaseName;

        public bool UseConnectionPooling { get; set; }

        private DMSConfig mConfiguration;

        /// <summary>
        /// Constructor
        /// </summary>
        public DMSDBConnection()
        {
            mConfiguration = new DMSConfig();
            LoadLocalConfiguration();
            // This should generally be true for SqlClient/SqlConnection, false means connection reuse (and potential multi-threading problems)
            UseConnectionPooling = true;
        }

        private SqlConnection connection;
        private string lastConnectionString = "";
        private DateTime lastConnectionAttempt = DateTime.MinValue;
        private readonly TimeSpan minTimeBetweenConnectionAttempts = TimeSpan.FromSeconds(30);
        private readonly TimeSpan connectionTimeoutTime = TimeSpan.FromSeconds(60);
        private Timer connectionTimeoutTimer;
        private string lastConnectionStringLoaded = "";
        private DateTime lastConnectionStringLoadTime = DateTime.MinValue;
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

        ~DMSDBConnection()
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
        /// <returns></returns>
        private SqlConnectionWrapper GetConnection()
        {
            var connString = GetConnectionString();

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
        /// Gets DMS connection string from config file
        /// </summary>
        /// <returns></returns>
        public string GetConnectionString()
        {
            if (lastConnectionStringLoadTime > DateTime.UtcNow.AddMinutes(-10))
            {
                return lastConnectionStringLoaded;
            }

            // Construct the connection string, for example:
            // Data Source=Gigasax;Initial Catalog=DMS5;User ID=LCMSNetUser;Password=ThePassword"

            // ToDo: update this to construct a Postgres connection string

            var loaded = LoadCentralConfiguration();
            if (!loaded)
            {
                LoadLocalConfiguration();
            }

            mConfiguration.ValidateConfig();

            var retStr = "Data Source=";

            // Get the DMS Server name
            retStr += mConfiguration.DatabaseServer;

            // Get name of the DMS database to use
            retStr += ";Initial Catalog=" + mConfiguration.DatabaseName + ";User ID=LCMSNetUser";

            if (!string.IsNullOrWhiteSpace(ApplicationName))
            {
                retStr += $";Application Name={ApplicationName}";
            }

            if (!mConnectionStringLogged || !lastConnectionStringLoaded.StartsWith(retStr))
            {
                ApplicationLogger.LogMessage(ApplicationLogger.CONST_STATUS_LEVEL_DETAILED,
                    "Database connection string: " + retStr + ";Password=....");
                mConnectionStringLogged = true;
            }

            // Get the password for user LCMSNetUser
            // Decrypts password received from config file
            retStr += ";Password=" + AppUtils.DecodeShiftCipher(mConfiguration.EncodedPassword);

            lastConnectionStringLoadTime = DateTime.UtcNow;
            lastConnectionStringLoaded = retStr;
            return retStr;
        }

        /// <summary>
        /// Loads DMS configuration from a centralized file
        /// </summary>
        /// <returns>True if able to read/load the central configuration</returns>
        private bool LoadCentralConfiguration()
        {
            var remoteConfigLoaded = false;

            try
            {
                if (File.Exists(CENTRAL_CONFIG_FILE_PATH))
                {
                    // Centralized config file exists; read it
                    var config = DMSConfig.FromJson(CENTRAL_CONFIG_FILE_PATH);
                    var good = config.ValidateConfig();

                    // Centralized config file contains all the important information; cache it and use it, if it is not a match for the current cached config
                    if (good && !config.Equals(mConfiguration))
                    {
                        ApplicationLogger.LogMessage(LogLevel.Info, "Loading updated DMS database configuration from centralized config file...");
                        mConfiguration = config;
                        remoteConfigLoaded = true;
                        var configPath = PersistDataPaths.GetFileSavePath(CONFIG_FILE);
                        config.ToJson(configPath);
                    }

                    remoteConfigLoaded = good;
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(LogLevel.Info, "Exception attempting to load centralized database configuration file", ex);
            }

            return remoteConfigLoaded;
        }

        /// <summary>
        /// Loads DMS configuration from file
        /// </summary>
        private void LoadLocalConfiguration()
        {
            var configurationPath = PersistDataPaths.GetFileLoadPath(CONFIG_FILE);
            if (!File.Exists(configurationPath))
            {
                mConfiguration = CreateDefaultConfigFile(configurationPath);
            }
            else
            {
                try
                {
                    mConfiguration = DMSConfig.FromJson(configurationPath);
                }
                catch (Exception ex)
                {
                    ApplicationLogger.LogError(LogLevel.Info, "Exception attempting to load local database configuration file", ex);
                }
            }
        }

        private static DMSConfig CreateDefaultConfigFile(string configurationPath)
        {
            var config = new DMSConfig();
            config.LoadDefaults();
            config.ToJson(configurationPath);
            return config;
        }

        /// <summary>
        /// Generic method to retrieve data from a single-column table
        /// </summary>
        /// <param name="cmdStr">SQL command to execute</param>
        /// <returns>List containing the table's contents</returns>
        public IEnumerable<string> GetSingleColumnTable(string cmdStr)
        {
            var cn = GetConnection();
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
        /// <returns>DataTable containing requested data</returns>
        /// <remarks>This tends to use more memory than directly reading and parsing data.</remarks>
        [Obsolete("Unused")]
        // ReSharper disable once UnusedMember.Local
        private DataTable GetDataTable(string cmdStr)
        {
            var returnTable = new DataTable();
            var cn = GetConnection();
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

        // TODO: do I need to wrap usages of this in a foreach loop and yield return?
        public IEnumerable<T> ExecuteReader<T>(string sqlCmd, Func<IDataReader, T> rowParseObjectCreator)
        {
            var cn = GetConnection();
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
                        yield return rowParseObjectCreator(reader);
                    }
                }
            }
        }

        /// <summary>
        /// Test if we can query each of the needed DMS tables/views.
        /// </summary>
        /// <param name="tableNamesAndCheckColumns">Dictionary where the key is a table/view name, and the value a sortable column name</param>
        /// <returns></returns>
        public bool CheckDMSConnection(IReadOnlyDictionary<string, string> tableNamesAndCheckColumns)
        {
            try
            {
                using (var conn = GetConnection())

                // Test getting 1 row from every table we query?...
                using (var cmd = conn.CreateCommand())
                {
                    // Keys in the dictionary are view names, values are the column to use when ranking rows using Row_number()
                    foreach (var item in tableNamesAndCheckColumns)
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
    }
}
