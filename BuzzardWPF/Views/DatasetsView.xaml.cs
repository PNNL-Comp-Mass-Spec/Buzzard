using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BuzzardWPF.Data;
using BuzzardWPF.ViewModels;

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
            DatasetDataGrid.SelectedIndex = -1;
        }

        /// <summary>
        /// Selects all the datasets that are held in the datagrid.
        /// </summary>
        private void SelectAllDatasets_Click(object sender, RoutedEventArgs e)
        {
            DatasetDataGrid.SelectAll();
        }

        /// <summary>
        /// Provide a version of "SelectedItems" one-way-to-source binding
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatasetDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dc = this.DataContext as DatasetsViewModel;
            if (dc == null)
            {
                return;
            }

            var selector = sender as MultiSelector;
            if (selector == null)
            {
                return;
            }

            dc.SelectedDatasets.Clear();
            dc.SelectedDatasets.AddRange(selector.SelectedItems.Cast<BuzzardDataset>());
        }
    }
}
