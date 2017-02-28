using System.Windows;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;

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

        public classExperimentData SelectedExperiment => m_viewer.SelectedExperiment;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedExperiment == null)
            {
                classApplicationLogger.LogMessage(
                    0,
                    "An experiment must be selected in order to proceed.");
                return;
            }

            DialogResult = true;
        }
    }
}
