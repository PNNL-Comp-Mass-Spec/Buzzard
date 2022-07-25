using System.Windows;
using BuzzardWPF.Logging;
using BuzzardWPF.ViewModels;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for WorkPackageSelectionWindow.xaml
    /// </summary>
    public partial class WorkPackageSelectionWindow : Window
    {
        public WorkPackageSelectionWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is WorkPackageSelectionViewModel evvm && evvm.SelectedWorkPackage == null)
            {
                ApplicationLogger.LogMessage(
                    0,
                    "A work package must be selected in order to proceed.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
