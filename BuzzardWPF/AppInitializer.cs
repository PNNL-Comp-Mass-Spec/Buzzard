using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Navigation;
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

        public const string PROGRAM_DATE = "June 8, 2015";
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
        /// <param name="path">Local path to create.</param>
        static void CreatePath(string localPath)
        {

            var appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);

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
        /// <returns>True if a new version exists and the user has chosen to launch the installer</returns>
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

                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(fiInstaller.FullName);

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
                    if (GetValue(versionPartsInstaller, i) > GetValue(versionPartsRunning, i))
                    {
                        var updateMsg = "A new version of Buzzard is available at " + installerFolderPath + "; Install the new version now?";
                        var eResponse = MessageBox.Show(updateMsg, @"Upgrade Advised", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                        if (eResponse == DialogResult.Yes)
                        {
                            // Launch the installer
                            var startInfo = new ProcessStartInfo(fiInstaller.FullName)
                            {
                                UseShellExecute = true
                            };

                            Process.Start(startInfo);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                classApplicationLogger.LogMessage(0, "Error checking for a new version: " + ex.Message);
                System.Threading.Thread.Sleep(500);
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
            classSQLiteTools.AppDataFolderName = name;
            classSQLiteTools.CacheName = "BuzzardCache.que";
            classSQLiteTools.BuildConnectionString(false);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            // Before we do anything, let's initialize the file logging capability.
            classApplicationLogger.Error += classFileLogging.LogError;
            classApplicationLogger.Message += classFileLogging.LogMessage;

            LogVersionNumbers();
            LogMachineInformation();
            classApplicationLogger.LogMessage(0, string.Format("[Log]"));


            // Load settings 
            classApplicationLogger.LogMessage(-1, "Loading settings");

            LoadSettings();

            var instName = classLCMSSettings.GetParameter("InstName");
            (System.Windows.Application.Current as App).DynamicSplashScreen.InstrumentName = instName;

            // Set the logging levels
            if (classLCMSSettings.GetParameter("LoggingErrorLevel") != null)
            {
                classApplicationLogger.ErrorLevel = int.Parse(classLCMSSettings.GetParameter("LoggingErrorLevel"));
            }
            else
            {
                classApplicationLogger.ErrorLevel = CONST_DEFAULT_ERROR_LOG_LEVEL;
            }

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
                var dbTools = new classDBTools();
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
                if (bool.Parse(classLCMSSettings.GetParameter("CopyTriggerFiles")))
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
                DatasetManager.Manager.LoadDmsCache();

                // This is where we used to open the main window, but this process is 
                // now running in a background thread where creating a DependencyObject
                // would not be adivsed. So, I'm setting a flag that saying that the main
                // window should be opened. I'm also moving some initialization logic out 
                // of the main window's constructor and placing it here, since this is
                // where it was being done before.
                openMainWindow = true;
                DMS_DataAccessor.Instance.Initialize();
            }
            catch (Exception ex)
            {
                classFileLogging.LogError(0, new classErrorLoggerArgs("Program Failed!", ex));
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
