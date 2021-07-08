using System.Windows;
using BuzzardWPF.ViewModels;
using LcmsNetData.Logging;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for ExperimentsDialogWindow.xaml
    /// </summary>
    public partial class ExperimentsDialogWindow : Window
    {
        public ExperimentsDialogWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ExperimentsViewerViewModel evvm && evvm.SelectedExperiment == null)
            {
                ApplicationLogger.LogMessage(
                    0,
                    "An experiment must be selected in order to proceed.");
                return;
            }

            DialogResult = true;
            this.Close();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
