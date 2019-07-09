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
using BuzzardWPF.ViewModels;
using LcmsNetData.Logging;

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
            this.Close();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
