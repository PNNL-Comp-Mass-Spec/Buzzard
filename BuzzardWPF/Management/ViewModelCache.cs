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
            fillDownVm = null;
            experimentsVm = null;
            workPackageVm = null;
        }

        private FillDownWindowViewModel fillDownVm;
        private ExperimentsViewerViewModel experimentsVm;
        private WorkPackageSelectionViewModel workPackageVm;
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
    }
}
