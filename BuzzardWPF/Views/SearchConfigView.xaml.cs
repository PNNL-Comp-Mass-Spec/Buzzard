using System.Windows.Controls;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for SearchConfigView.xaml
    /// </summary>
    public partial class SearchConfigView : UserControl
    {
        public SearchConfigView()
        {
            InitializeComponent();
        }

        private string lastSelection = "";

        private void AutoCompleteBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is AutoCompleteBox acb))
            {
                return;
            }

            // HACK!!: The AutoCompleteBox, for some reason, will do auto-complete when you select an entry, and then immediately revert it
            // This code is to cache the selected value, and then (when the selected item is removed because it was 'applied') re-apply the selected value
            if (e.AddedItems.Count > 0)
            {
                lastSelection = e.AddedItems[0].ToString();
            }
            else if (!string.IsNullOrWhiteSpace(lastSelection))
            {
                acb.Text = lastSelection;
            }
        }
    }
}
