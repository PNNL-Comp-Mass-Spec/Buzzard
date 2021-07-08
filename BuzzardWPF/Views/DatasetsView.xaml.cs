using System.Windows;
using System.Windows.Controls;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for DatasetsView.xaml
    /// </summary>
    public partial class DatasetsView : UserControl
    {
        public DatasetsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Deselects any dataset and all datasets that are held in the datagrid.
        /// </summary>
        private void SelectNoDatasets_Click(object sender, RoutedEventArgs e)
        {
            DatasetsDataGrid.SelectNoDatasets_Click(sender, e);
        }

        /// <summary>
        /// Selects all the datasets that are held in the datagrid.
        /// </summary>
        private void SelectAllDatasets_Click(object sender, RoutedEventArgs e)
        {
            DatasetsDataGrid.SelectAllDatasets_Click(sender, e);
        }
    }
}
