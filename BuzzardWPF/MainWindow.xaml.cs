using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LcmsNetSDK.Logging;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Closed += Main_Closed;
            Loaded += Main_Loaded;
        }

        private bool firstTimeLoading = true;

        private void Main_OnClosed(object sender, EventArgs e)
        {
            AppInitializer.CleanupApplication();
        }

        /// <summary>
        /// Will tell the various configuration containing controls
        /// to place their values into the settings object before
        /// saving the setting object for application shutdown.
        /// </summary>
        private void Main_Closed(object sender, EventArgs e)
        {
            ApplicationLogger.LogMessage(0, "Main Window closed.");

            // Save settings
            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.SaveSettings();
            }
        }

        /// <summary>
        /// Will load the saved configuration settings on application startup.
        /// </summary>
        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            if (firstTimeLoading)
            {
                //// This next piece of code will reset the settings
                //// to their default values before loading them into
                //// the application.
                //// This is kept here, in case I need to check that
                //// the effects of the default settings.
                //// -FCT
                //BuzzardWPF.Properties.Settings.Default.Reset();
                if (DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.LoadSettings();
                }
                firstTimeLoading = false;
            }
        }
    }
}
