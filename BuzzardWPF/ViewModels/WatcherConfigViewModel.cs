using System;
using System.Reactive;
using BuzzardWPF.Management;
using BuzzardWPF.Searching;
using BuzzardWPF.Views;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherConfigViewModel : ReactiveObject
    {
        #region Initialization
        public WatcherConfigViewModel()
        {
            isNotMonitoring = true;

            EmslUsageSelectionVm.BoundContainer = WatcherMetadata;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
        }

        #endregion

        #region Properties

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public WatcherMetadata WatcherMetadata => DatasetManager.WatcherMetadata;

        public ReactiveList<string> InterestRatingOptions => DatasetManager.INTEREST_RATINGS_COLLECTION;

        public DMS_DataAccessor DmsData => DMS_DataAccessor.Instance;

        public bool IsNotMonitoring
        {
            get => isNotMonitoring;
            private set => this.RaiseAndSetIfChanged(ref isNotMonitoring, value);
        }
        private bool isNotMonitoring;

        #endregion

        #region Event Handlers

        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// an experiment name they wish to apply to the new datasets.
        /// </summary>
        private void SelectExperiment()
        {
            var dialogVm = new ExperimentsViewerViewModel();
            var dialog = new ExperimentsDialogWindow()
            {
                DataContext = dialogVm
            };
            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            DatasetManager.WatcherMetadata.ExperimentName = dialogVm.SelectedExperiment.Experiment;
        }

        /// <summary>
        /// Enables / disables the controls based on e.Monitoring
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MonitoringToggleHandler(object sender, StartStopEventArgs e)
        {
            IsNotMonitoring = !e.Monitoring;
        }
        #endregion
    }
}
