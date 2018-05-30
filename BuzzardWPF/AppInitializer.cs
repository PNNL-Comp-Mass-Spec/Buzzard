using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BuzzardWPF.Management;
using LcmsNetDmsTools;
using LcmsNetSDK;
using LcmsNetSDK.Logging;
using LcmsNetSQLiteTools;

namespace BuzzardWPF
{
    public static class AppInitializer
    {
        #region "Constants and members"

        /// <summary>
        /// Default error level
        /// </summary>
        /// <remarks>Log levels are 0 to 5, where 0 is most important and 5 is least important</remarks>
        private const int CONST_DEFAULT_ERROR_LOG_LEVEL = 5;

        /// <summary>
        /// Default message level.
        /// </summary>
        /// <remarks>Log levels are 0 to 5, where 0 is most important and 5 is least important</remarks>
        private const int CONST_DEFAULT_MESSAGE_LOG_LEVEL = 5;

        public const string PROGRAM_DATE = "September 1, 2017";

        private static DMSDBTools dmsDbToolsInstance;
        private static readonly SQLiteTools sqliteToolsInstance = SQLiteTools.GetInstance();

        #endregion

        #region Configuration Loading
        /// <summary>
        /// Loads the application settings.
        /// </summary>
        /// <returns>An object that holds the application settings.</returns>
        static void LoadSettings()
        {
            // Note that settings are persisted in file user.config in a randomly named folder below %userprofile%\appdata\local
            // For example:
            // C:\Users\username\appdata\local\PNNL\BuzzardWPF.exe_Url_yufs4k44bouk50s0nygwhsc1xpnayiku\1.7.13.4\user.config

            // Possibly upgrade the settings from a previous version
            if (Properties.Settings.Default.UpgradeSettings)
            {
                // User settings for this version was not found
                // Try to upgrade from the previous version
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Reload();
                Properties.Settings.Default.UpgradeSettings = false;
            }

            var propColl = Properties.Settings.Default.Properties;

            foreach (SettingsProperty currProperty in propColl)
            {
                var propertyName = currProperty.Name;
                var propertyValue = string.Empty;

                if (Properties.Settings.Default[propertyName] != null)
                    propertyValue = Properties.Settings.Default[propertyName].ToString();

                if (propertyName == LCMSSettings.PARAM_TRIGGERFILEFOLDER && string.IsNullOrWhiteSpace(propertyValue))
                {
                    propertyValue = MainWindowViewModel.DEFAULT_TRIGGER_FOLDER_PATH;
                    Properties.Settings.Default[propertyName] = propertyValue;
                }

                LCMSSettings.SetParameter(propertyName, propertyValue);
            }

            // Add path to executable as a saved setting
            var fi = new FileInfo(Application.ExecutablePath);
            LCMSSettings.SetParameter("ApplicationPath", fi.DirectoryName);

        }

        #endregion

        #region Logging

        /// <summary>
        /// Logs the software version number
        /// </summary>
        public static void LogVersionNumbers()
        {
            var information = SystemInformationReporter.BuildApplicationInformation();
            ApplicationLogger.LogMessage(0, information);
        }

        /// <summary>
        /// Logs the machine information
        /// </summary>
        public static void LogMachineInformation()
        {
            var systemInformation = SystemInformationReporter.BuildSystemInformation();
            ApplicationLogger.LogMessage(0, systemInformation);
        }

        /// <summary>
        /// Creates the path required for local operation.
        /// </summary>
        /// <param name="localPath">Local path to create.</param>
        static void CreatePath(string localPath)
        {

            var appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var path = Path.Combine(appPath, "Buzzard", localPath);

            //
            // See if the logging directory exists
            //
            if (Directory.Exists(path)) return;

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (UnauthorizedAccessException ex)
            {
                //
                // Not much we can do here...
                //
                var errorMessage = string.Format("Buzzard could not create missing folder {0} required for operation. " +
                                                 "Please update directory permissions or run Buzzard as an administrator: {1}",
                                                 localPath, ex.Message);
                LogCriticalError(errorMessage, null);
                Application.Exit();
            }
        }
        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="installerFolderPath"></param>
        /// <returns>
        /// True if a new version exists and the user launched the installer
        /// In that case, this program will exit, thus allowing the installer to complete successfully
        /// </returns>
#if DotNET4
        private static bool CheckForNewVersion(string installerFolderPath = @"\\proto-5\BionetSoftware\Buzzard\DotNet4.0_XP")
#else
        private static bool CheckForNewVersion(string installerFolderPath = @"\\proto-5\BionetSoftware\Buzzard")
#endif
        {
            try
            {
                var diInstallerFolder = new DirectoryInfo(installerFolderPath);

                if (!diInstallerFolder.Exists)
                    return false;

                // Look for one or more installers; keep the newest one
                var installers = diInstallerFolder.GetFiles("Buzzard*PNNL*.exe").OrderBy(info => info.LastWriteTimeUtc).Reverse().ToList();
                if (installers.Count == 0)
                    return false;

                var fiInstaller = installers.FirstOrDefault();

                if (fiInstaller == null)
                {
                    return false;
                }

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(fiInstaller.FullName);

                var fileVersion = fileVersionInfo.FileVersion.Trim();

                if (string.IsNullOrWhiteSpace(fileVersion))
                    return false;

                var versionPartsInstaller = fileVersion.Split('.');

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var versionRunning = assembly.GetName().Version.ToString();

                if (string.IsNullOrWhiteSpace(versionRunning))
                    return false;

                var versionPartsRunning = versionRunning.Split('.');

                for (var i = 0; i < versionPartsInstaller.Length; i++)
                {
                    var installerVersionPart = GetValue(versionPartsInstaller, i);
                    var runningVersionPart = GetValue(versionPartsRunning, i);

                    if (installerVersionPart > runningVersionPart)
                    {
                        var updateMsg = "A new version of Buzzard is available at " + installerFolderPath + "; Install the new version now?";
                        var eResponse = MessageBox.Show(updateMsg, @"Upgrade Advised", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                        if (eResponse == DialogResult.Yes)
                        {
                            // Launch the installer
                            // First need to copy it locally (since running over the network fails on some of the computers)

                            LaunchTheInstaller(fiInstaller);

                            return true;
                        }
                        break;
                    }

                    if (installerVersionPart < runningVersionPart)
                    {
                        // This version is user; stop comparing to the installer
                        break;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogMessage(0, "Error checking for a new version: " + ex.Message);
                System.Threading.Thread.Sleep(750);
                return false;
            }
        }

        private static int GetValue(IList<string> versionArray, int index)
        {
            if (index >= versionArray.Count)
            {
                return 0;
            }

            if (int.TryParse(versionArray[index], out var value))
            {
                return value;
            }
            return 0;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static async Task<bool> InitializeApplication(Action<string> instrumentNameAction = null)
        {
            var openMainWindow = false;

            CreatePath("Log");

            const string name = "Buzzard";
            FileLogging.AppFolder = name;

            SQLiteTools.Initialize(name);
            SQLiteTools.SetCacheLocation("BuzzardCache.que");
            SQLiteTools.BuildConnectionString(false);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            // Before we do anything, let's initialize the file logging capability.
            ApplicationLogger.Error += FileLogging.LogError;
            ApplicationLogger.Message += FileLogging.LogMessage;

            LogVersionNumbers();
            LogMachineInformation();
            ApplicationLogger.LogMessage(0, "[Log]");

            // Load settings
            ApplicationLogger.LogMessage(-1, "Loading settings");

            LoadSettings();

            var instName = LCMSSettings.GetParameter("InstName");
            if (instName != null)
            {
                instrumentNameAction?.Invoke(instName);
            }

            // Set the logging levels (0 is most important; 5 is least important)
            // When logLevel is 0, only critical messages are logged
            // When logLevel is 5, all messages are logged
            var logLevel = LCMSSettings.GetParameter("LoggingErrorLevel", CONST_DEFAULT_ERROR_LOG_LEVEL);
            ApplicationLogger.ErrorLevel = logLevel;

            ApplicationLogger.MessageLevel = CONST_DEFAULT_MESSAGE_LOG_LEVEL;

            ApplicationLogger.LogMessage(-1, "Checking for a new version");

            var newVersionInstalling = CheckForNewVersion();
            if (newVersionInstalling)
            {
                ApplicationLogger.LogMessage(-1, "Closing since new version is installing");
                // Return false, meaning do not show the main window
                return false;
            }

            ApplicationLogger.LogMessage(-1, "Loading DMS data");

            try
            {
                // Load active experiments (created/used in the last 18 months), datasets, instruments, etc.
                dmsDbToolsInstance = new DMSDBTools
                {
                    LoadExperiments = true,
                    LoadDatasets = true,
                    RecentExperimentsMonthsToLoad = DMS_DataAccessor.RECENT_EXPERIMENT_MONTHS,
                    RecentDatasetsMonthsToLoad = DMS_DataAccessor.RECENT_DATASET_MONTHS
                };

                dmsDbToolsInstance.ProgressEvent += dbTools_ProgressEvent;

                dmsDbToolsInstance.LoadCacheFromDMS();
            }
            catch (Exception ex)
            {
                var errorMessage = "Error loading data from DMS!";
                LogCriticalError(errorMessage, ex);
            }

            ApplicationLogger.LogMessage(-1, "Checking For Local Trigger Files");

            try
            {
                //
                // Check to see if any trigger files need to be copied to the transfer server, and copy if necessary
                //
                var copyTriggerFiles = LCMSSettings.GetParameter("CopyTriggerFiles", false);
                if (copyTriggerFiles)
                {
                    if (LcmsNetSDK.Data.TriggerFileTools.CheckLocalTriggerFiles())
                    {
                        ApplicationLogger.LogMessage(-1, "Copying trigger files to DMS");
                        LcmsNetSDK.Data.TriggerFileTools.MoveLocalTriggerFiles();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Error processing existing local trigger files!";
                LogCriticalError(errorMessage, ex);
            }

            ApplicationLogger.LogMessage(-1, "Training the last of the buzzards...Loading DMS Cache");

            try
            {
                // Load active requested runs from DMS
                await DatasetManager.Manager.LoadDmsCache().ConfigureAwait(false);

                // Set a flag to indicate that the main window can now be shown
                openMainWindow = true;

                // Load the experiments, datasets, instruments, etc. from the SQLite cache file
                DMS_DataAccessor.Instance.LoadDMSDataFromCache(true);
            }
            catch (Exception ex)
            {
                FileLogging.LogError(0, new ErrorLoggerArgs("Error loading data from DMS at program startup", ex));
            }
            finally
            {
                //Properties.Settings.Default.InstName                = LCMSSettings.GetParameter("InstName");
                //Properties.Settings.Default.Operator                = LCMSSettings.GetParameter("Operator");
                //Properties.Settings.Default.CacheFileName           = LCMSSettings.GetParameter("CacheFileName");
                //Properties.Settings.Default.SeparationType          = LCMSSettings.GetParameter("SeparationType");
                //Properties.Settings.Default.AutoMonitor             = Convert.ToBoolean(LCMSSettings.GetParameter("AutoMonitor"));
                //Properties.Settings.Default.Duration                = Convert.ToInt32(LCMSSettings.GetParameter("Duration"));
                //Properties.Settings.Default.WatchExtension          = LCMSSettings.GetParameter("WatchExtension");
                //Properties.Settings.Default.WatchDirectory          = LCMSSettings.GetParameter("WatchDirectory");
                //Properties.Settings.Default.SearchType              = LCMSSettings.GetParameter("SearchType");

                Properties.Settings.Default.Save();

                FileLogging.LogMessage(0, new MessageLoggerArgs("---------------------------------------"));
            }

            return openMainWindow;
        }

        public static void CleanupApplication()
        {
            dmsDbToolsInstance.Dispose();
            sqliteToolsInstance.Dispose();
        }

        static void dbTools_ProgressEvent(object sender, ProgressEventArgs e)
        {
            ApplicationLogger.LogMessage(-1, "Loading DMS data: " + e.CurrentTask);
        }

        private static void LaunchTheInstaller(FileInfo fiInstaller)
        {
            var localInstallerPath = "??";

            try
            {
                var tempFolder = Path.GetTempPath();
                localInstallerPath = Path.Combine(tempFolder, fiInstaller.Name);

                var fiLocalInstaller = new FileInfo(localInstallerPath);

                fiInstaller.CopyTo(fiLocalInstaller.FullName, true);

                var startInfo = new ProcessStartInfo(fiLocalInstaller.FullName)
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogMessage(0, "Error launching the installer for the new version (" + localInstallerPath + "): " + ex.Message);
                System.Threading.Thread.Sleep(750);
            }

        }

        private static void LogCriticalError(string errorMessage, Exception ex, bool showPopup = true)
        {
            var exceptionMessage = string.Empty;

            if (ex == null)
                FileLogging.LogError(0, new ErrorLoggerArgs(errorMessage));
            else
            {
                FileLogging.LogError(0, new ErrorLoggerArgs(errorMessage, ex));
                exceptionMessage = ex.Message;
            }

            if (showPopup)
            {
                MessageBox.Show(errorMessage + @"  " + exceptionMessage, @"Error", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
            }
        }
    }
}
