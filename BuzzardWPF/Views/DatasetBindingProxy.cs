using System.Windows;
using BuzzardWPF.ViewModels;
using WpfExtras;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Binding proxy to help with DataGrid binding to base DataContext
    /// </summary>
    /// <remarks>https://thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/</remarks>
    public class DatasetBindingProxy : BindingProxy<DatasetsViewModel>
    {
        protected override BindingProxy<DatasetsViewModel> CreateNewInstance()
        {
            return new DatasetBindingProxy();
        }

        /// <summary>
        /// Data object for binding
        /// </summary>
        public override DatasetsViewModel Data
        {
            get => (DatasetsViewModel)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <summary>
        /// DependencyProperty definition for Data
        /// </summary>
        public new static readonly DependencyProperty DataProperty = BindingProxy<DatasetsViewModel>.DataProperty.AddOwner(typeof(DatasetBindingProxy));
    }
}
