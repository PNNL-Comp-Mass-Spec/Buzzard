using System;
using System.Threading;
using System.Windows;
using BuzzardWPF.Management;
using LcmsNetSDK.Logging;
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

            var openMainWindow = AppInitializer.InitializeApplication(splashScreen.SetInstrumentName).Result;
            if (openMainWindow)
            {
                dmsDataAccessorInstance = DMS_DataAccessor.Instance;
                var mainWindow = new MainWindow()
                {
                    DataContext = new MainWindowViewModel()
                };
                Application.Current.MainWindow = mainWindow;
                MainWindow = mainWindow;

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
