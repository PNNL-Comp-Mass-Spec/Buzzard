using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BuzzardWPF.ViewModels;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for ErrorMessagesView.xaml
    /// </summary>
    public partial class ErrorMessagesView : Window
    {
        public ErrorMessagesView()
        {
            InitializeComponent();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CtrlCCopyCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var lb = (ListBox)(sender);
            var selected = lb.SelectedItem;
            if (selected != null) Clipboard.SetText(selected.ToString());
        }

        private void CtrlCCopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void RightClickCopyCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var mi = (MenuItem)sender;
            var selected = mi.DataContext;
            if (selected != null && selected is ErrorMessagesViewModel vm)
            {
                var errorMessages = string.Join("\n", vm.ErrorMessageList);
                Clipboard.SetText(errorMessages);
            }
        }

        private void RightClickCopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
