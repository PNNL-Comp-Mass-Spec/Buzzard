﻿using System;
using System.Threading;
using System.Windows;
using BuzzardWPF.Management;
using LcmsNetData;
using LcmsNetData.Logging;
using LcmsNetSQLiteTools;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            // Show the splash screen
            resetSplashCreated = new ManualResetEvent(false);

            // Run it on a different thread.
            splashThread = new Thread(ShowSplashScreen);
            splashThread.SetApartmentState(ApartmentState.STA);
            splashThread.IsBackground = true;
            splashThread.Name = "SplashScreen";
            splashThread.Start();

            // Block until the splash screen is displayed
            resetSplashCreated.WaitOne();

            var openMainWindow = AppInitializer.InitializeApplication(splashScreen, splashScreen.SetInstrumentName).Result;
            if (openMainWindow)
            {
                dmsDataAccessorInstance = DMS_DataAccessor.Instance;
                var mainVm = new MainWindowViewModel();
                var mainWindow = new MainWindow()
                {
                    DataContext = mainVm,
                };
                Application.Current.MainWindow = mainWindow;
                MainWindow = mainWindow;

                // Set the logging levels (0 is most important; 5 is least important)
                // When logLevel is 0, only critical messages are logged
                // When logLevel is 5, all messages are logged
                var logLevel = LCMSSettings.GetParameter("LoggingErrorLevel", MainWindowViewModel.CONST_DEFAULT_ERROR_LOG_LEVEL);
                mainVm.ErrorLevel = logLevel;

                mainVm.MessageLevel = MainWindowViewModel.CONST_DEFAULT_MESSAGE_LOG_LEVEL;

                // Do this here so that closing the splash screen doesn't minimize/throw to the background the main window.
                splashScreen.LoadComplete();
                splashScreenEnded = true;
                try
                {
                    mainWindow.Show();
                    MainWindow.Activate();
                }
                catch (Exception ex)
                {
                    ApplicationLogger.LogError(0, "Buzzard quit unexpectedly. " + ex.Message, ex);
                    mainWindow.Close();
                }
            }
            else
            {
                splashScreen.LoadComplete();
                splashScreenEnded = true;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ApplicationLogger.LogError(0, "Buzzard had an unhandled critical error.  " + e.Exception.Message, e.Exception);
            ShutdownCleanup();
        }

        private void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is System.IO.FileNotFoundException && e.Exception.Message.Contains("System.XmlSerializers"))
            {
                // Exception is "Could not load file or assembly 'System.XmlSerializers, ... The system cannot find the file specified."
                // It can safely be ignored (see http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor)
                return;
            }

            if (e.Exception.Message.Contains("The dataset is just not available in this trie."))
            {
                // Exception is: Could not resolve the dataset name.  The dataset is just not available in this trie.
                // Ignore it
                return;
            }

            var ex = e.Exception;
            var buzzardLcmsNetFound = false;
            while (ex != null && !buzzardLcmsNetFound)
            {
                var stacktrace = ex.StackTrace.ToLower();
                buzzardLcmsNetFound = stacktrace.Contains("buzzard") || stacktrace.Contains("lcmsnet");
                ex = ex.InnerException;
            }

            // Only report unhandled first chance exceptions that occur within PNNL-written code; other exceptions we can only handle elsewhere, and we don't want to report the exceptions that we are handling.
            if (!buzzardLcmsNetFound)
            {
                return;
            }

            ApplicationLogger.LogError(0, "Buzzard had an unhandled non-critical error.  " + e.Exception.Message, e.Exception);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ShutdownCleanup();
        }

        private void ShutdownCleanup()
        {
            // Make sure the splash screen is closed.
            if (!splashScreenEnded)
            {
                splashScreen.LoadComplete();
            }

            sqliteToolsInstance.Dispose();
            dmsDataAccessorInstance?.Dispose();
        }

        /// <summary>
        /// Reference to splash screen window.
        /// </summary>
        private DynamicSplashScreenWindow splashScreen;

        private bool splashScreenEnded = false;

        private ManualResetEvent resetSplashCreated;
        private Thread splashThread;

        private static readonly SQLiteTools sqliteToolsInstance = SQLiteTools.GetInstance();
        private static DMS_DataAccessor dmsDataAccessorInstance = null;

        private void ShowSplashScreen()
        {
            splashScreen = new DynamicSplashScreenWindow();
            splashScreen.Show();

            // set the reset, to allow startup to continue
            resetSplashCreated.Set();
            System.Windows.Threading.Dispatcher.Run();
        }
    }
}
