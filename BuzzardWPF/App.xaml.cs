using System;
using System.Windows;
using BuzzardWPF.Windows;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public DynamicSplash DynamicSplashScreen { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            DynamicSplashScreen = new DynamicSplash
            {
                Title = "Buzzard",
                ShowInTaskbar = false
            };

            DynamicSplashScreen.Show();
            DynamicSplashScreen.InitializeInBackground();

        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            classApplicationLogger.LogError(0, "Buzzard had an unhandled error.  " + e.Exception.Message, e.Exception);
        }

        private void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is System.IO.IOException && e.Exception.Message.Contains("logo_2017"))
            {
                // Exception is "Cannot locate resource 'resources/logo_2017.png'."
                // It can safely be ignored
                return;
            }

            if (e.Exception is System.IO.FileNotFoundException && e.Exception.Message.Contains("System.XmlSerializers"))
            {
                // Exception is "Could not load file or assembly 'System.XmlSerializers, ... The system cannot find the file specified."
                // It can safely be ignored (see http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor)
                return;
            }

            classApplicationLogger.LogError(0, "Buzzard had an unhandled error.  " + e.Exception.Message, e.Exception);
        }

    }
}
