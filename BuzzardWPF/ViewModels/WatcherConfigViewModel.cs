using System;
using System.Reactive;
using BuzzardWPF.Management;
using BuzzardWPF.Searching;
using BuzzardWPF.Views;
using LcmsNetSDK.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherConfigViewModel : ReactiveObject, IEmslUsvUser
    {
        #region Initialization
        public WatcherConfigViewModel()
        {
            m_IsNotMonitoring = true;

            EmslUsageSelectionVm.BoundContainer = this;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);

            DatasetManager.WhenAnyValue(x => x.WatcherEmslUsage).Subscribe(x => this.RaisePropertyChanged(nameof(SelectedEMSLUsageType)));
            DatasetManager.WhenAnyValue(x => x.WatcherEmslProposalID).Subscribe(x => this.RaisePropertyChanged(nameof(EMSLProposalID)));
            DatasetManager.WhenAnyValue(x => x.WatcherSelectedProposalUsers).Subscribe(x => this.RaisePropertyChanged(nameof(SelectedEMSLProposalUsers)));
        }

        #endregion

        #region Properties

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public DMS_DataAccessor DmsData => DMS_DataAccessor.Instance;

        public bool IsNotMonitoring
        {
            get => m_IsNotMonitoring;
            private set
            {
                m_IsNotMonitoring = value;
                this.RaisePropertyChanged("IsNotMonitoring");
            }
        }
        private bool m_IsNotMonitoring;

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

            DatasetManager.ExperimentName = dialogVm.SelectedExperiment.Experiment;
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

        #region IEmslUsvUser Members

        public string SelectedEMSLUsageType
        {
            get => DatasetManager.WatcherEmslUsage;
            set => DatasetManager.WatcherEmslUsage = value;
        }

        public string EMSLProposalID
        {
            get => DatasetManager.WatcherEmslProposalID;
            set => DatasetManager.WatcherEmslProposalID = value;
        }

        public ReactiveList<ProposalUser> SelectedEMSLProposalUsers
        {
            get => DatasetManager.WatcherSelectedProposalUsers;
            set => DatasetManager.WatcherSelectedProposalUsers = value;
        }

        #endregion
    }
}
