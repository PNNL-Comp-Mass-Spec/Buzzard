﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.Views;
using LcmsNetSDK.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class QCViewModel : ReactiveObject, IEmslUsvUser
    {
        #region Initialize
        public QCViewModel()
        {
            EmslUsageSelectionVm.BoundContainer = this;

            SelectedEMSLUsageType = EmslUsageSelectionVm.UsageTypesSource[1];
            EMSLProposalID = null;
            ExperimentName = null;
            SelectedEMSLProposalUsers = new ReactiveList<ProposalUser>();

            isNotMonitoring = true;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            AddQcMonitorCommand = ReactiveCommand.Create(AddQcMonitor, this.WhenAnyValue(x => x.ExperimentName, x => x.DatasetNameMatch, x => x.SelectedEMSLUsageType, x => x.EMSLProposalID, x => x.SelectedEMSLProposalUsers, x => x.SelectedEMSLProposalUsers.Count, x => x.Manager.QcMonitors).Select(
                x =>
                {
                    var musts = !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2) && !string.IsNullOrWhiteSpace(x.Item3);
                    if (!musts)
                    {
                        return false;
                    }

                    if (x.Item7.Any(y => y.DatasetNameMatch.Equals(x.Item2)))
                    {
                        return false;
                    }

                        if (x.Item3.Equals("USER", StringComparison.OrdinalIgnoreCase))
                    {
                        return !string.IsNullOrWhiteSpace(x.Item4) && x.Item5.Any();
                    }

                    return true;
                }));

            RemoveQcMonitorCommand = ReactiveCommand.Create(RemoveQcMonitor, this.WhenAnyValue(x => x.SelectedQcMonitor).Select(x => x != null));

            this.WhenAnyValue(x => x.ExperimentName).Subscribe(x => DatasetNameMatch = x);
        }
        #endregion

        #region Properties

        private string selectedEMSLUsageType;
        private string emslProposalID;
        private string experimentName;
        private ReactiveList<ProposalUser> selectedEMSLProposalUsers;
        private bool isNotMonitoring;
        private string datasetNameMatch;
        private QcMonitorData selectedQcMonitor;

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> AddQcMonitorCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveQcMonitorCommand { get; }

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public string SelectedEMSLUsageType
        {
            get => selectedEMSLUsageType;
            set => this.RaiseAndSetIfChanged(ref selectedEMSLUsageType, value);
        }

        public string EMSLProposalID
        {
            get => emslProposalID;
            set => this.RaiseAndSetIfChanged(ref emslProposalID, value);
        }

        public string ExperimentName
        {
            get => experimentName;
            set => this.RaiseAndSetIfChanged(ref experimentName, value);
        }

        public DatasetManager Manager => DatasetManager.Manager;

        public ReactiveList<ProposalUser> SelectedEMSLProposalUsers
        {
            get => selectedEMSLProposalUsers;
            set
            {
                if (selectedEMSLProposalUsers != value)
                {
                    selectedEMSLProposalUsers = value;
                    this.RaisePropertyChanged();

                    EmslUsageSelectionVm.UpdateSelectedUsersText();
                }
            }
        }

        public string DatasetNameMatch
        {
            get => datasetNameMatch;
            set => this.RaiseAndSetIfChanged(ref datasetNameMatch, value);
        }

        public bool IsNotMonitoring
        {
            get => isNotMonitoring;
            private set => this.RaiseAndSetIfChanged(ref isNotMonitoring, value);
        }

        public QcMonitorData SelectedQcMonitor
        {
            get => selectedQcMonitor;
            set => this.RaiseAndSetIfChanged(ref selectedQcMonitor, value);
        }

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

            ExperimentName = dialogVm.SelectedExperiment.Experiment;
        }

        private void AddQcMonitor()
        {
            var qcMonitor = new QcMonitorData
            {
                ExperimentName = ExperimentName,
                EmslProposalId = EMSLProposalID,
                EmslUsageType = SelectedEMSLUsageType,
                DatasetNameMatch = DatasetNameMatch
            };
            using (qcMonitor.EmslProposalUsers.SuppressChangeNotifications())
            {
                qcMonitor.EmslProposalUsers.AddRange(SelectedEMSLProposalUsers);
            }

            Manager.QcMonitors.Add(qcMonitor);
        }

        private void RemoveQcMonitor()
        {
            if (SelectedQcMonitor == null)
            {
                return;
            }

            Manager.QcMonitors.Remove(SelectedQcMonitor);
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

        #region Methods
        public void SaveSettings()
        {
            //Settings.Default.QC_ExperimentName = ExperimentName;
            //Settings.Default.QC_ProposalID = EMSLProposalID;
            //Settings.Default.QC_SelectedUsageType = SelectedEMSLUsageType;
            //Settings.Default.QC_CreateTriggerOnDMS_Fail = DatasetManager.Manager.QC_CreateTriggerOnDMSFail;
            //
            //var selectedEMSLUsers = new System.Collections.Specialized.StringCollection();
            //foreach (var user in SelectedEMSLProposalUsers)
            //    selectedEMSLUsers.Add(user.UserID.ToString());
            //
            //Settings.Default.QC_EMSL_Users = selectedEMSLUsers;

            Settings.Default.QC_CreateTriggerOnDMS_Fail = DatasetManager.Manager.QC_CreateTriggerOnDMSFail;

            if (DatasetManager.Manager.QcMonitors.Any())
            {
                QcMonitorData.SaveSettings(DatasetManager.Manager.QcMonitors);
            }
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.QC_ExperimentName;
            EMSLProposalID = Settings.Default.QC_ProposalID;
            SelectedEMSLUsageType = Settings.Default.QC_SelectedUsageType;
            DatasetManager.Manager.QC_CreateTriggerOnDMSFail = Settings.Default.QC_CreateTriggerOnDMS_Fail;

            List<string> selectedUsers;
            if (Settings.Default.QC_EMSL_Users == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.QC_EMSL_Users.Cast<string>().ToList();

            if (!string.IsNullOrWhiteSpace(ExperimentName) && string.IsNullOrWhiteSpace(Settings.Default.QC_Monitors))
            {
                SelectedEMSLProposalUsers = DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers);

                var monitor = new QcMonitorData()
                {
                    ExperimentName = ExperimentName,
                    EmslProposalId = EMSLProposalID,
                    EmslUsageType = SelectedEMSLUsageType,
                    DatasetNameMatch = "*"
                };
                using (monitor.EmslProposalUsers.SuppressChangeNotifications())
                {
                    monitor.EmslProposalUsers.AddRange(SelectedEMSLProposalUsers);
                }

                DatasetManager.Manager.QcMonitors.Add(monitor);
            }
            else if (!string.IsNullOrWhiteSpace(Settings.Default.QC_Monitors))
            {
                using (DatasetManager.Manager.QcMonitors.SuppressChangeNotifications())
                {
                    DatasetManager.Manager.QcMonitors.AddRange(QcMonitorData.LoadSettings());
                }
            }
        }
        #endregion
    }
}
