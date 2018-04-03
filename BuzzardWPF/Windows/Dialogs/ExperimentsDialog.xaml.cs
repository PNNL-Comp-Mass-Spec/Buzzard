using System.Windows;
using LcmsNetSDK.Data;
using LcmsNetSDK.Logging;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for ExperimentsDialog.xaml
    /// </summary>
    public partial class ExperimentsDialog
        : Window
    {
        public ExperimentsDialog()
        {
            InitializeComponent();
        }

        public ExperimentData SelectedExperiment => m_viewer.SelectedExperiment;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedExperiment == null)
            {
                ApplicationLogger.LogMessage(
                    0,
                    "An experiment must be selected in order to proceed.");
                return;
            }

            DialogResult = true;
        }
    }
}
