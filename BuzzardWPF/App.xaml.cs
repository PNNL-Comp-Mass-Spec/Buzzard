﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BuzzardWPF.IO;
using BuzzardWPF.IO.DMS;
using BuzzardWPF.IO.SQLite;
using BuzzardWPF.Logging;
using BuzzardWPF.Management;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // Ignore Spelling: trie, lcmsnet

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            MainLoad();
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            ShutdownCleanup();
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // This is fired after Dispatcher.UnhandledException - exceptions cannot be "handled" here, we can only report them
            var e = (Exception)args.ExceptionObject;
            try
            {
                ApplicationLogger.LogError(0, "Buzzard had an unhandled critical error.  " + e.Message, e);
            }
            catch
            {
                // Do nothing, we already tried to log it.
            }

            ShutdownCleanup();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                ApplicationLogger.LogError(0, "Buzzard had an unhandled critical error.  " + e.Exception.Message, e.Exception);
            }
            catch
            {
                // Do nothing, we already tried to log it.
            }

            e.Handled = true;
            ShutdownCleanup();
        }

        private void CurrentDomainOnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is FileNotFoundException && e.Exception.Message.Contains("System.XmlSerializers"))
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
            while (ex?.StackTrace != null && !buzzardLcmsNetFound)
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

            try
            {
                ApplicationLogger.LogError(0, "Buzzard had an unhandled non-critical error.  " + e.Exception.Message, e.Exception);
            }
            catch
            {
                MessageBox.Show("Warning: Unable to log a non-critical error; error was " + e.Exception, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShutdownCleanup()
        {
            // Make sure the splash screen is closed.
            if (!splashScreenEnded)
            {
                splashScreen?.LoadComplete();
            }

            DatasetManager.Manager.Dispose();
            sqliteDisposable.Dispose();
            dmsDataAccessorInstance?.Dispose();
            TriggerFileTools.Instance.Dispose();
            ViewModelCache.Instance.Dispose();
            mainWindowViewModel?.Dispose();
            ShutDownLogging();
        }

        private void ShutDownLogging()
        {
            ApplicationLogger.ShutDownLogging();
            FileLogger.Instance.Dispose();
        }

        /// <summary>
        /// Reference to splash screen window.
        /// </summary>
        private DynamicSplashScreenWindow splashScreen;

        private bool splashScreenEnded;

        private ManualResetEvent resetSplashCreated;

        private static readonly IDisposable sqliteDisposable = SQLiteTools.GetDisposable();
        private static DMSDataAccessor dmsDataAccessorInstance;

        private static MainWindowViewModel mainWindowViewModel;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private void MainLoad()
        {
            // Show the splash screen
            resetSplashCreated = new ManualResetEvent(false);

            // Run it on a different thread.
            var splashThread = new Thread(ShowSplashScreen);
            splashThread.SetApartmentState(ApartmentState.STA);
            splashThread.IsBackground = true;
            splashThread.Name = "SplashScreen";
            splashThread.Start();

            // Block until the splash screen is displayed
            resetSplashCreated.WaitOne();

            var assembly = Assembly.GetExecutingAssembly().GetName();
            DMSDBConnection.ApplicationName = $"Buzzard {assembly.Version}";

            var openMainWindow = AppInitializer.InitializeApplication(splashScreen, splashScreen.SetInstrumentHostName).Result;
            if (openMainWindow)
            {
                dmsDataAccessorInstance = DMSDataAccessor.Instance;
                mainWindowViewModel = new MainWindowViewModel();

                // Load the saved configuration settings on application startup.
                // Uncomment the following line to test with default settings.
                //BuzzardWPF.Properties.Settings.Default.Reset();
                mainWindowViewModel.LoadSettings();
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };
                Current.MainWindow = mainWindow;
                MainWindow = mainWindow;

                // Set the logging levels (0 is most important; 5 is least important)
                // When logLevel is 0, only critical messages are logged
                // When logLevel is 5, all messages are logged
                mainWindowViewModel.ErrorLevel = MainWindowViewModel.CONST_DEFAULT_ERROR_LOG_LEVEL;
                mainWindowViewModel.MessageLevel = MainWindowViewModel.CONST_DEFAULT_MESSAGE_LOG_LEVEL;

                // Do this here so that closing the splash screen doesn't minimize/throw to the background the main window.
                splashScreen.LoadComplete();
                splashScreenEnded = true;

                try
                {
                    splashThread.Join(200);
                    if (splashThread.IsAlive)
                    {
                        splashThread.Abort();
                    }
                }
                catch
                {
                    // Don't care about the exception.
                }

                splashScreen = null;

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
                ShutdownCleanup();
                // Trigger an immediate shutdown - Otherwise we might get errors when updating
                Shutdown(0);
            }
        }

        private void ShowSplashScreen()
        {
            splashScreen = new DynamicSplashScreenWindow();
            splashScreen.Show();

            // set the reset, to allow startup to continue
            resetSplashCreated.Set();
            Dispatcher.Run();
        }
    }
}
