using System;
using BuzzardWPF.Data;
using BuzzardWPF.ViewModels;

namespace BuzzardWPF.Management
{
    public class ViewModelCache : IDisposable
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

        public FillDownWindowViewModel GetFillDownVm(FilldownBuzzardDataset fillDownDataset)
        {
            if (fillDownVm == null)
            {
                fillDownVm = new FillDownWindowViewModel(fillDownDataset);
            }

            return fillDownVm;
        }

        public ExperimentsViewerViewModel GetExperimentsVm()
        {
            if (experimentsVm == null)
            {
                experimentsVm = new ExperimentsViewerViewModel();
            }

            return experimentsVm;
        }

        public WorkPackageSelectionViewModel GetWorkPackageVm()
        {
            if (workPackageVm == null)
            {
                workPackageVm = new WorkPackageSelectionViewModel();
            }

            return workPackageVm;
        }
    }
}
