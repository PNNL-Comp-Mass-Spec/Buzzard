using System;
using System.Windows;
using WpfExtras.CueBannerOverlay;
using Xceed.Wpf.Toolkit;

namespace BuzzardWPF.Views
{
    public class CheckComboBoxOverlayable : CheckComboBox, IInputOverlayable
    {
        public CheckComboBoxOverlayable()
        {
            // Propagate the desired event
            ItemSelectionChanged += (sender, args) => ContentChanged?.Invoke(sender, args);
        }

        public event EventHandler<RoutedEventArgs> ContentChanged;
        public bool ShouldShowCueBanner()
        {
            // Show the cue banner if there are no selected items.
            return SelectedItems.Count == 0;
        }

        public bool IgnoreKeyboardFocusEvents { get; } = true;
    }
}
