using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Windows.Forms;
using System.Security.Principal;
using System.Collections.Generic;

using LcmsNetDataClasses;
using LcmsNetDataClasses.Configuration;
using LcmsNetDmsTools;
using LcmsNetDataClasses.Logging;


namespace Buzzard
{
    static class Program
    {

        #region "Constants and members"
        /// <summary>
        /// Default error level
        /// </summary>
        private const int CONST_DEFAULT_ERROR_LOG_LEVEL     = 5;
        /// <summary>
        /// Default message level.
        /// </summary>
        private const int CONST_DEFAULT_MESSAGE_LOG_LEVEL   = 5;        
        /// <summary>
        /// Reference to splash screen window.
        /// </summary>
        private static formSplashScreen mform_splashScreen;
        #endregion

        #region Configuration Loading
        /// <summary>
        /// Loads the application settings.
        /// </summary>
        /// <returns>An object that holds the application settings.</returns>
        static void LoadSettings()
        {
            SettingsPropertyCollection propColl = Properties.Settings.Default.Properties;
            foreach (SettingsProperty currProperty in propColl)
            {
                string propertyName = currProperty.Name;
                string propertyValue = Properties.Settings.Default[propertyName].ToString();
                classLCMSSettings.SetParameter(propertyName, propertyValue);
            }

            // Add path to executable as a saved setting
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            classLCMSSettings.SetParameter("ApplicationPath", fi.DirectoryName);
            
            mform_splashScreen.SetEmulatedLabelVisibility(classLCMSSettings.GetParameter("InstName"), false);            
            return;
        }
        #endregion

        #region Logging
        /// <summary>
        /// Logs the software version number
        /// </summary>
        public static void LogVersionNumbers()
        {
            string information = SystemInformationReporter.BuildApplicationInformation();
            classApplicationLogger.LogMessage(0, information);
        }
        /// <summary>
        /// Logs the machine information
        /// </summary>
        public static void LogMachineInformation()
        {
            string systemInformation = SystemInformationReporter.BuildSystemInformation();
            classApplicationLogger.LogMessage(0, systemInformation);
        }
        /// <summary>
        /// Creates the path required for local operation.
        /// </summary>
        /// <param name="path">Local path to create.</param>
        static void CreatePath(string localPath)
        {
            string path = Path.Combine(Application.StartupPath, localPath);
            /// 
            /// See if the logging directory exists
            /// 
            if (Directory.Exists(path) == false)
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (UnauthorizedAccessException ex)
                {
                    /// 
                    /// Not much we can do here...
                    /// 
                    string errorMessage = string.Format("Buzzard could not create missing folder {0} required for operation.  Please run application with higher priveleges.  {1}",
                                                                  localPath, ex.Message);
                    MessageBox.Show(errorMessage);
                    Application.Exit();
                }
            }
        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            CreatePath("Log");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Before we do anything, let's initialize the file logging capability.
            classApplicationLogger.Error   += new classApplicationLogger.DelegateErrorHandler(classFileLogging.LogError);
            classApplicationLogger.Message += new classApplicationLogger.DelegateMessageHandler(classFileLogging.LogMessage);
            
            // Show the splash screen
            mform_splashScreen = new formSplashScreen();
            mform_splashScreen.Show();

            classApplicationLogger.Message += new classApplicationLogger.DelegateMessageHandler(classApplicationLogger_Message);


            LogVersionNumbers();
            LogMachineInformation();
            classApplicationLogger.LogMessage(0, string.Format("[Log]"));
            Application.DoEvents();

            // Load settings 
            classApplicationLogger.LogMessage(-1, "Loading settings");
            Application.DoEvents();
            LoadSettings();

            // Set the logging levels
            if (classLCMSSettings.GetParameter("LoggingErrorLevel") != null)
            {
                classApplicationLogger.ErrorLevel = int.Parse(classLCMSSettings.GetParameter("LoggingErrorLevel"));
            }
            else
            {
                classApplicationLogger.ErrorLevel = CONST_DEFAULT_ERROR_LOG_LEVEL;
            }

            classApplicationLogger.LogMessage(-1, "Training the last of the buzzards with DMS data");
            Application.DoEvents();
            classDBTools.LoadCacheFromDMS();

            ///
            /// Check to see if any trigger files need to be copied to the transfer server, and copy if necessary
            /// 
            if (bool.Parse(classLCMSSettings.GetParameter("CopyTriggerFiles")))
            {
                if (LcmsNetDataClasses.Data.classTriggerFileTools.CheckLocalTriggerFiles())
                {
                    classApplicationLogger.LogMessage(-1, "Copying trigger files to DMS");
                    LcmsNetDataClasses.Data.classTriggerFileTools.MoveLocalTriggerFiles();
                }
            }

            /// 
            /// Load the main application and run
            /// 
            classApplicationLogger.LogMessage(-1, "Loading main form");
            Application.DoEvents();

            mainBuzzard main = new mainBuzzard();
            mform_splashScreen.Hide();

            classApplicationLogger.Message -= classApplicationLogger_Message;

            try
            {
                Application.Run(main);
            }
            catch (Exception ex)
            {
                classFileLogging.LogError(0, new classErrorLoggerArgs("Program Failed!", ex));
            }
            finally
            {                    
                Properties.Settings.Default.InstName                = classLCMSSettings.GetParameter("InstName");
                Properties.Settings.Default.Operator                = classLCMSSettings.GetParameter("Operator");
                Properties.Settings.Default.CacheFileName           = classLCMSSettings.GetParameter("CacheFileName");
                Properties.Settings.Default.SeparationType          = classLCMSSettings.GetParameter("SeparationType");
                Properties.Settings.Default.AutoMonitor             = Convert.ToBoolean(classLCMSSettings.GetParameter("AutoMonitor"));
                Properties.Settings.Default.Duration                = Convert.ToInt32(classLCMSSettings.GetParameter("Duration"));
                Properties.Settings.Default.WatchExtension          = classLCMSSettings.GetParameter("WatchExtension");
                Properties.Settings.Default.WatchDirectory          = classLCMSSettings.GetParameter("WatchDirectory");
                Properties.Settings.Default.SearchType              = classLCMSSettings.GetParameter("SearchType");
                Properties.Settings.Default.ColumnData              = classLCMSSettings.GetParameter("ColumnData");
                Properties.Settings.Default.CartName                = classLCMSSettings.GetParameter("CartName");

                Properties.Settings.Default.Save();

                classFileLogging.LogMessage(0, new classMessageLoggerArgs("---------------------------------------"));
            }
        }

        /// <summary>
        /// Updates the splash screen with the appropiate messages.
        /// </summary>
        /// <param name="messageLevel">Filter for displaying messages.</param>
        /// <param name="args">Messages and other arguments passed from the sender.</param>
        static void classApplicationLogger_Message(int messageLevel, classMessageLoggerArgs args)
        {
            if (messageLevel < 1)
            {
                mform_splashScreen.Status = args.Message;
            }
        }
    }
}
