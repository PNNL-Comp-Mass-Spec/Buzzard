using System;
using BuzzardWPF.Data;
using BuzzardWPF.ViewModels;

namespace BuzzardWPF.Management
{
    public sealed class ViewModelCache : IDisposable
    {
        static ViewModelCache()
        {
            Instance = new ViewModelCache();
        }

        public static ViewModelCache Instance { get; }

        private ViewModelCache()
        {
        }

        public void Dispose()
        {
            fillDownVm?.Dispose();
            experimentsVm?.Dispose();
            workPackageVm?.Dispose();
            errorMessagesVm?.Dispose();
            fillDownVm = null;
            experimentsVm = null;
            workPackageVm = null;
            errorMessagesVm = null;
        }

        private FillDownWindowViewModel fillDownVm;
        private ExperimentsViewerViewModel experimentsVm;
        private WorkPackageSelectionViewModel workPackageVm;
        private ErrorMessagesViewModel errorMessagesVm;

        public FilldownBuzzardDataset FilldownDataset { get; private set; }

        public FillDownWindowViewModel GetFillDownVm(FilldownBuzzardDataset filldownDataset)
        {
            if (FilldownDataset == null || (!ReferenceEquals(FilldownDataset, filldownDataset) &&
                                            filldownDataset != null))
            {
                FilldownDataset = filldownDataset;
            }

            if (fillDownVm == null || !ReferenceEquals(FilldownDataset, fillDownVm.Dataset))
            {
                fillDownVm = new FillDownWindowViewModel(FilldownDataset);
            }

            return fillDownVm;
        }

        public void SetFillDownDataset(FilldownBuzzardDataset filldownDataset)
        {
            FilldownDataset = filldownDataset;
        }

        /// <summary>
        /// Returns the current FillDownViewModel. May return null.
        /// </summary>
        /// <returns></returns>
        public FillDownWindowViewModel GetFillDownVm()
        {
            return fillDownVm;
        }

        public ExperimentsViewerViewModel GetExperimentsVm()
        {
            return experimentsVm ?? (experimentsVm = new ExperimentsViewerViewModel());
        }

        public WorkPackageSelectionViewModel GetWorkPackageVm()
        {
            return workPackageVm ?? (workPackageVm = new WorkPackageSelectionViewModel());
        }

        public ErrorMessagesViewModel GetErrorMessagesVm()
        {
            // The goal of this method is to only ever show a single instance of the error messages window.
            // If other errors occur before the user acknowledges/closes the window, they are added to the list displayed.
            return errorMessagesVm ?? (errorMessagesVm = new ErrorMessagesViewModel(() =>
            {
                // When the window is closed, dispose of the view model
                var errorVm = errorMessagesVm;
                errorMessagesVm = null;
                errorVm?.Dispose();
            }));
        }
    }
}
