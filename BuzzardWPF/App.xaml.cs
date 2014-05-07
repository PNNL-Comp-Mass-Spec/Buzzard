using System.Windows;
using BuzzardWPF.Windows;
using LcmsNetDataClasses.Logging;
using LcmsNetDmsTools;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		public DynamicSplash DynamicSplashScreen { get; set; }

		private void Application_Startup(object sender, StartupEventArgs e)
		{


                DynamicSplashScreen         = new DynamicSplash();
                DynamicSplashScreen.Title   = "Buzzard";
                DynamicSplashScreen.ShowInTaskbar = false;
                DynamicSplashScreen.Show();
                DynamicSplashScreen.InitializeInBackground();                  
		}

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            classApplicationLogger.LogError(0, "Buzzard had an unhandled error.  " + e.Exception.Message, e.Exception);
        }       
	}
}
