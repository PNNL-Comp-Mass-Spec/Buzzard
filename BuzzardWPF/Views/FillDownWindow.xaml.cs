using System.Windows;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for FillDownWindow.xaml
    /// </summary>
    public partial class FillDownWindow : Window
    {
        public FillDownWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
