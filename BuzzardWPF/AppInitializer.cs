using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BuzzardWPF.Management;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;
using LcmsNetDmsTools;
using LcmsNetSQLiteTools;

namespace BuzzardWPF
{
    public static class AppInitializer
    {
        #region "Constants and members"
        /// <summary>
        /// Default error level
        /// </summary>
        private const int CONST_DEFAULT_ERROR_LOG_LEVEL = 5;

        /// <summary>
        /// Default message level.
        /// </summary>
        private const int CONST_DEFAULT_MESSAGE_LOG_LEVEL = 5;

        public const string PROGRAM_DATE = "February 28, 2017";
        #endregion

        #region Configuration Loading
        /// <summary>
        /// Loads the application settings.
        /// </summary>
        /// <returns>An object that holds the application settings.</returns>
        static void LoadSettings()
        {
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

                classLCMSSettings.SetParameter(propertyName, propertyValue);
            }

            // Add path to executable as a saved setting
            var fi = new FileInfo(Application.ExecutablePath);
            classLCMSSettings.SetParameter("ApplicationPath", fi.DirectoryName);
;
            //       mform_splashScreen.SetEmulatedLabelVisibility(classLCMSSettings.GetParameter("InstName"), false);
        }
        #endregion

        #region Logging
        /// <summary>
        /// Logs the software version number
        /// </summary>
        public static void LogVersionNumbers()
        {
            var information = SystemInformationReporter.BuildApplicationInformation();
            classApplicationLogger.LogMessage(0, information);
        }
        /// <summary>
        /// Logs the machine information
        /// </summary>
        public static void LogMachineInformation()
        {
            var systemInformation = SystemInformationReporter.BuildSystemInformation();
            classApplicationLogger.LogMessage(0, systemInformation);
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
            if (Directory.Exists(path) == false)
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // 
                    // Not much we can do here...
                    // 
                    var errorMessage = string.Format("Buzzard could not create missing folder {0} required for operation.  Please run application with higher priveleges.  {1}",
                                                                  localPath, ex.Message);
                    LogCriticalError(errorMessage, null);
                    Application.Exit();
                }
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
        private static bool CheckForNewVersion(string installerFolderPath = @"\\proto-5\BionetSoftware\Buzzard")
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
                classApplicationLogger.LogMessage(0, "Error checking for a new version: " + ex.Message);
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
            
            int value;
            if (int.TryParse(versionArray[index], out value))
            {
                return value;
            }
            return 0;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static bool InitializeApplication()
        {
            var openMainWindow = false;

            CreatePath("Log");

            const string name = "Buzzard";
            classFileLogging.AppFolder = name;
            
            classSQLiteTools.Initialize(name);
            classSQLiteTools.SetCacheLocation("BuzzardCache.que");
            classSQLiteTools.BuildConnectionString(false);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            // Before we do anything, let's initialize the file logging capability.
            classApplicationLogger.Error += classFileLogging.LogError;
            classApplicationLogger.Message += classFileLogging.LogMessage;

            LogVersionNumbers();
            LogMachineInformation();
            classApplicationLogger.LogMessage(0, "[Log]");


            // Load settings
            classApplicationLogger.LogMessage(-1, "Loading settings");

            LoadSettings();

            var instName = classLCMSSettings.GetParameter("InstName");
            var app = System.Windows.Application.Current as App;
            if (app != null)
            {
                app.DynamicSplashScreen.InstrumentName = instName;
            }

            // Set the logging levels (0 is most important; 5 is least important)
            // When logLevel is 0, only critical messages are logged
            // When logLevel is 5, all messages are logged
            var logLevel = classLCMSSettings.GetParameter("LoggingErrorLevel", CONST_DEFAULT_ERROR_LOG_LEVEL);
            classApplicationLogger.ErrorLevel = logLevel;

            classApplicationLogger.MessageLevel = CONST_DEFAULT_MESSAGE_LOG_LEVEL;

            classApplicationLogger.LogMessage(-1, "Checking for a new version");

            var newVersionInstalling = CheckForNewVersion();
            if (newVersionInstalling)
            {
                classApplicationLogger.LogMessage(-1, "Closing since new version is installing");
                // Return false, meaning do not show the main window
                return false;
            }

            classApplicationLogger.LogMessage(-1, "Loading DMS data");

            try
            {
                // Load active experiments (created/used in the last 18 months), daasets, instruments, etc.
                var dbTools = new classDBTools
                {
                    LoadExperiments = true,
                    LoadDatasets = true,
                    RecentExperimentsMonthsToLoad = DMS_DataAccessor.RECENT_EXPERIMENT_MONTHS,
                    RecentDatasetsMonthsToLoad = DMS_DataAccessor.RECENT_DATASET_MONTHS
                };

                dbTools.ProgressEvent += dbTools_ProgressEvent;

                dbTools.LoadCacheFromDMS();
               
            }
            catch (Exception ex)
            {
                var errorMessage = "Error loading data from DMS!";
                LogCriticalError(errorMessage, ex);
            }

            classApplicationLogger.LogMessage(-1, "Checking For Local Trigger Files");

            try
            {
                //
                // Check to see if any trigger files need to be copied to the transfer server, and copy if necessary
                // 
                var copyTriggerFiles = classLCMSSettings.GetParameter("CopyTriggerFiles", false);
                if (copyTriggerFiles)
                {
                    if (LcmsNetDataClasses.Data.classTriggerFileTools.CheckLocalTriggerFiles())
                    {
                        classApplicationLogger.LogMessage(-1, "Copying trigger files to DMS");
                        LcmsNetDataClasses.Data.classTriggerFileTools.MoveLocalTriggerFiles();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Error processing existing local trigger files!";
                LogCriticalError(errorMessage, ex);
            }

            classApplicationLogger.LogMessage(-1, "Training the last of the buzzards...Loading DMS Cache");

            try
            {
                // Load active requested runs from DMS
                DatasetManager.Manager.LoadDmsCache();

                // Set a flag to indicate that the main window can now be shown
                openMainWindow = true;

                // Load the experiments, datasets, instruments, etc. from the SQLite cache file
                DMS_DataAccessor.Instance.LoadDMSDataFromCache(true);
            }
            catch (Exception ex)
            {
                classFileLogging.LogError(0, new classErrorLoggerArgs("Error loading data from DMS at program startup", ex));
            }
            finally
            {
                //Properties.Settings.Default.InstName                = classLCMSSettings.GetParameter("InstName");
                //Properties.Settings.Default.Operator                = classLCMSSettings.GetParameter("Operator");
                //Properties.Settings.Default.CacheFileName           = classLCMSSettings.GetParameter("CacheFileName");
                //Properties.Settings.Default.SeparationType          = classLCMSSettings.GetParameter("SeparationType");
                //Properties.Settings.Default.AutoMonitor             = Convert.ToBoolean(classLCMSSettings.GetParameter("AutoMonitor"));
                //Properties.Settings.Default.Duration                = Convert.ToInt32(classLCMSSettings.GetParameter("Duration"));
                //Properties.Settings.Default.WatchExtension          = classLCMSSettings.GetParameter("WatchExtension");
                //Properties.Settings.Default.WatchDirectory          = classLCMSSettings.GetParameter("WatchDirectory");
                //Properties.Settings.Default.SearchType              = classLCMSSettings.GetParameter("SearchType");

                Properties.Settings.Default.Save();

                classFileLogging.LogMessage(0, new classMessageLoggerArgs("---------------------------------------"));
            }


            return openMainWindow;
        }

        static void dbTools_ProgressEvent(object sender, ProgressEventArgs e)
        {
            classApplicationLogger.LogMessage(-1, "Loading DMS data: " + e.CurrentTask);
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
                classApplicationLogger.LogMessage(0, "Error launching the installer for the new version (" + localInstallerPath + "): " + ex.Message);
                System.Threading.Thread.Sleep(750);
            }

        }

        private static void LogCriticalError(string errorMessage, Exception ex, bool showPopup = true)
        {
            var exceptionMessage = string.Empty;

            if (ex == null)
                classFileLogging.LogError(0, new classErrorLoggerArgs(errorMessage));
            else
            {
                classFileLogging.LogError(0, new classErrorLoggerArgs(errorMessage, ex));
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
