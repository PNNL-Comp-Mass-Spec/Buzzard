﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using BuzzardWPF.Management;
using BuzzardWPF.ViewModels;
using LcmsNetData;
using LcmsNetData.Logging;
using LcmsNetData.System;
using LcmsNetSQLiteTools;

namespace BuzzardWPF
{
    public static class AppInitializer
    {
        // Ignore Spelling: appdata, Bionet, username, userprofile, MMMM dd, yyyy

        public static string AssemblyDate { get; }

        static AppInitializer()
        {
            AssemblyDate = GetCommitOrBuildDate();
        }

        /// <summary>
        /// This method allows accessing the GitCommitDate method in the <see cref="ThisAssembly"/> class created by NerdBank.GitVersioning
        /// </summary>
        /// <returns>Commit or build date</returns>
        public static string GetCommitOrBuildDate()
        {
            var members = Assembly.GetExecutingAssembly().GetType("ThisAssembly")?.GetMember("GitCommitDate",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (members?.Length > 0)
            {
                object dateObj = null;
                switch (members[0])
                {
                    case FieldInfo field:
                        dateObj = field.GetValue(null);
                        break;
                    case PropertyInfo prop:
                        dateObj = prop.GetValue(null);
                        break;
                }

                if (dateObj is DateTime dt)
                {
                    return dt.ToLocalTime().ToString("MMMM dd, yyyy");
                }
            }

            // Backup date, based on build date

            // Get only attributes of type 'AssemblyMetadataAttribute', then get the first one that has the key "AssemblyBuildDate"
            // If not found, assemblyDate will be null, and will be replaced in the assignment with an empty string.
            var assemblyBuildDate = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
                .Cast<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => x.Key.Equals("AssemblyBuildDate", StringComparison.OrdinalIgnoreCase))?.Value;

            if (DateTime.TryParseExact(assemblyBuildDate, "yyyy.MM.dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out var abd))
            {
                return abd.ToString("MMMM dd, yyyy");
            }

            return assemblyBuildDate;
        }

        /// <summary>
        /// Loads the application settings.
        /// </summary>
        /// <returns>An object that holds the application settings.</returns>
        private static List<Tuple<string, Exception>> LoadSettings()
        {
            // ReSharper disable CommentTypo

            // Note that settings are persisted in file user.config in a randomly named folder below %userprofile%\appdata\local
            // For example:
            // C:\Users\username\appdata\local\PNNL\BuzzardWPF.exe_Url_yufs4k44bouk50s0nygwhsc1xpnayiku\1.7.13.4\user.config

            // ReSharper restore CommentTypo

            // Possibly upgrade the settings from a previous version
            if (Properties.Settings.Default.SettingsUpgradeRequired)
            {
                // User settings for this version was not found
                // Try to upgrade from the previous version
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsUpgradeRequired = false;
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.TriggerFileFolder))
            {
                Properties.Settings.Default.TriggerFileFolder = BuzzardSettingsViewModel.DEFAULT_TRIGGER_FOLDER_PATH;
            }

            var loadErrors = LCMSSettings.LoadSettings(Properties.Settings.Default);

            Properties.Settings.Default.PropertyChanged += (sender, args) =>
                LCMSSettings.SetParameter(args.PropertyName, Properties.Settings.Default[args.PropertyName]?.ToString());

            return loadErrors;
        }

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
        private static void CreatePath(string localPath)
        {
            var appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var path = Path.Combine(appPath, "Buzzard", localPath);

            // See if the logging directory exists
            if (Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Not much we can do here...
                var errorMessage = string.Format("Buzzard could not create missing folder {0} required for operation. " +
                                                 "Please update directory permissions or run Buzzard as an administrator: {1}",
                                                 localPath, ex.Message);
                LogCriticalError(errorMessage, null);
                Application.Current.Shutdown(-1);
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static async Task<bool> InitializeApplication(Window displayWindow, Action<string> instrumentHostNameAction = null)
        {
            PersistDataPaths.SetAppName("Buzzard");

            // Load settings first - may include custom paths for log files and cache information
            var settingsErrors = LoadSettings();

            // Start up the threaded logging
            ApplicationLogger.StartUpLogging();

            var openMainWindow = false;

            CreatePath("Log");

#pragma warning disable CS0162 // Unreachable code detected

            const bool SHOW_ERROR_MESSAGES_FORM = false;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (SHOW_ERROR_MESSAGES_FORM)
            {
                DisplayErrorMessagesForm();
                return true;
            }

#pragma warning restore CS0162 // Unreachable code detected

            SQLiteTools.Initialize("BuzzardCache.que");
            //SQLiteTools.SetCacheLocation("BuzzardCache.que");
            SQLiteTools.BuildConnectionString(false);
            SQLiteTools.DisableInMemoryCaching = true;

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            // Before we do anything, let's initialize the file logging capability.
            ApplicationLogger.Error += FileLogger.Instance.LogError;
            ApplicationLogger.Message += FileLogger.Instance.LogMessage;

            LogVersionNumbers();
            LogMachineInformation();
            ApplicationLogger.LogMessage(0, "[Log]");

            // Report any settings loading errors that were encountered
            if (settingsErrors.Count > 0)
            {
                ApplicationLogger.LogMessage(-1, "Settings load errors:");
                foreach (var error in settingsErrors)
                {
                    ApplicationLogger.LogError(0, error.Item1, error.Item2);
                }
            }
            else
            {
                ApplicationLogger.LogMessage(-1, "Loaded user settings");
            }

            var instHostName = LCMSSettings.GetParameter("DMSInstrumentHostName");
            if (instHostName != null)
            {
                instrumentHostNameAction?.Invoke(instHostName);
            }

            ApplicationLogger.LogMessage(-1, "Checking for a new version");

            if (UpdateChecker.PromptToInstallNewVersionIfExists(displayWindow))
            {
                ApplicationLogger.LogMessage(-1, "Closing since new version is installing");
                // Return false, meaning do not show the main window
                return false;
            }

            ApplicationLogger.LogMessage(-1, "Loading DMS data");

            // Load the needed data from DMS into the SQLite cache file, with progress updates and special error reporting
            DMSDataAccessor.Instance.UpdateSQLiteCacheFromDms(DbTools_ProgressEvent, (msq, ex) => LogCriticalError(msq, ex));

            ApplicationLogger.LogMessage(-1, "Checking For Local Trigger Files");

            try
            {
                // Check to see if any trigger files need to be copied to the transfer server, and copy if necessary
                var copyTriggerFiles = LCMSSettings.GetParameter("CopyTriggerFiles", false);

                if (copyTriggerFiles && LcmsNetData.Data.TriggerFileTools.CheckLocalTriggerFiles())
                {
                    ApplicationLogger.LogMessage(-1, "Copying trigger files to DMS");
                    LcmsNetData.Data.TriggerFileTools.MoveLocalTriggerFiles();

                    if (LcmsNetData.Data.TriggerFileTools.ErrorMessages.Count > 0)
                    {
                        IO.BuzzardTriggerFileTools.ShowErrorMessages(LcmsNetData.Data.TriggerFileTools.ErrorMessages);
                    }
                }
            }
            catch (Exception ex)
            {
                LogCriticalError("Error processing existing local trigger files!", ex);
            }

            ApplicationLogger.LogMessage(-1, "Training the last of the buzzards...Loading DMS Cache");

            try
            {
                // Load the experiments, datasets, instruments, etc. from the SQLite cache file
                // Load this first because we filter the Requested Runs with information we get here.
                DMSDataAccessor.Instance.LoadDMSDataFromCache(true);

                // Load active requested runs from DMS
                await DatasetManager.Manager.DatasetNameMatcher.LoadRequestedRunsCache().ConfigureAwait(false);

                // Set a flag to indicate that the main window can now be shown
                openMainWindow = true;
            }
            catch (Exception ex)
            {
                FileLogger.Instance.LogError(0, new ErrorLoggerArgs(0, "Error loading data from DMS at program startup", ex));
            }
            finally
            {
                //Properties.Settings.Default.InstName                = LCMSSettings.GetParameter("InstName");
                //Properties.Settings.Default.Operator                = LCMSSettings.GetParameter("Operator");
                //Properties.Settings.Default.CacheFileName           = LCMSSettings.GetParameter("CacheFileName");

                Properties.Settings.Default.Save();

                FileLogger.Instance.LogMessage(0, new MessageLoggerArgs(0, "---------------------------------------"));
            }

            return openMainWindow;
        }

        private static void DbTools_ProgressEvent(object sender, ProgressEventArgs e)
        {
            ApplicationLogger.LogMessage(-1, "Loading DMS data: " + e.CurrentTask);
        }

        private static void DisplayErrorMessagesForm()
        {
            var testErrorMessages = new List<string> {
                "Test error message 1 - Lots of detail on the error message as populated by BuzzardWPF.AppInitializer.InitializeApplication(Window displayWindow, Action<string> instrumentNameAction = null)",
                "Test error message 2 - Buzzard is a utility for manually and automatically adding datasets to DMS. Buzzard is licensed under the Apache License, Version 2.0; you may not use this file except in compliance with the License. You may obtain a copy of the License at https://opensource.org/licenses/Apache-2.0",
                "Test error message 3 - Red",
                "Test error message 4 - Blue",
                "Test error message 5 - Green",
                "Test error message 6 - Yellow",
                "Test error message 7 - Brown",
                "Test error message 8 - Orange"
            };
            IO.BuzzardTriggerFileTools.ShowErrorMessages(testErrorMessages);
        }

        private static void LogCriticalError(string errorMessage, Exception ex, bool showPopup = true)
        {
            var exceptionMessage = string.Empty;

            if (ex == null)
            {
                FileLogger.Instance.LogError(0, new ErrorLoggerArgs(0, errorMessage));
            }
            else
            {
                FileLogger.Instance.LogError(0, new ErrorLoggerArgs(0, errorMessage, ex));
                exceptionMessage = ex.Message;
            }

            if (showPopup)
            {
                MessageBox.Show(errorMessage + "  " + exceptionMessage, "Error", MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }
        }
    }
}
