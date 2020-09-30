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

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for ErrorMessagesView.xaml
    /// </summary>
    public partial class ErrorMessagesView : Window
    {
        public ErrorMessagesView()
        {
            this.
            InitializeComponent();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
