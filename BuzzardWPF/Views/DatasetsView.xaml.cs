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
