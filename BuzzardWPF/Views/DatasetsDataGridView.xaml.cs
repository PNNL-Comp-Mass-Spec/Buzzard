﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BuzzardWPF.Data;
using BuzzardWPF.ViewModels;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for DatasetsDataGridView.xaml
    /// </summary>
    public partial class DatasetsDataGridView : UserControl
    {
        // Ignore Spelling: Deselects

        public DatasetsDataGridView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Deselects any dataset and all datasets that are held in the datagrid.
        /// </summary>
        public void SelectNoDatasets_Click(object sender, RoutedEventArgs e)
        {
            DatasetDataGrid.SelectedIndex = -1;
        }

        /// <summary>
        /// Selects all the datasets that are held in the datagrid.
        /// </summary>
        public void SelectAllDatasets_Click(object sender, RoutedEventArgs e)
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
