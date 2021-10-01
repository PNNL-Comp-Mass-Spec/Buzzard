using System;
using System.Windows;
using BuzzardWPF.Logging;

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
                mwvm.SaveSettingsOnClose();
            }
        }
    }
}
