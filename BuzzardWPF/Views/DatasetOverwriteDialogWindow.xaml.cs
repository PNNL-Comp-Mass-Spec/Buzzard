using System.Windows;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for DatasetOverwriteDialogWindow.xaml
    /// </summary>
    public partial class DatasetOverwriteDialogWindow : Window
    {
        public DatasetOverwriteDialogWindow()
        {
            InitializeComponent();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
