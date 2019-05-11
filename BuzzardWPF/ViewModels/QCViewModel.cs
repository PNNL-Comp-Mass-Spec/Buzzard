﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.Views;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class QCViewModel : ReactiveObject, IEmslUsageData, IStoredSettingsMonitor
    {
        #region Initialize
        public QCViewModel()
        {
            EmslUsageSelectionVm.BoundContainer = this;

            EMSLUsageType = EmslUsageSelectionVm.UsageTypesSource[1];
            EMSLProposalID = null;
            ExperimentName = null;

            isNotMonitoring = true;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            AddQcMonitorCommand = ReactiveCommand.Create(AddQcMonitor, this.WhenAnyValue(x => x.ExperimentName, x => x.DatasetNameMatch, x => x.EMSLUsageType, x => x.EMSLProposalID, x => x.EMSLProposalUsers, x => x.EMSLProposalUsers.Count, x => x.Manager.QcMonitors).Select(
                x =>
                {
                    var musts = !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2) && !string.IsNullOrWhiteSpace(x.Item3);
                    if (!musts)
                    {
                        DatasetNameMatchIsDuplicate = false;
                        return false;
                    }

                    if (x.Item7.Any(y => y.DatasetNameMatch.Equals(x.Item2)))
                    {
                        DatasetNameMatchIsDuplicate = true;
                        return false;
                    }

                    DatasetNameMatchIsDuplicate = false;

                    if (x.Item3.Equals("USER", StringComparison.OrdinalIgnoreCase))
                    {
                        return !string.IsNullOrWhiteSpace(x.Item4);
                    }

                    return true;
                }));

            RemoveQcMonitorCommand = ReactiveCommand.Create(RemoveQcMonitor, this.WhenAnyValue(x => x.SelectedQcMonitor).Select(x => x != null));

            this.WhenAnyValue(x => x.ExperimentName).Subscribe(x => DatasetNameMatch = x);
            this.WhenAnyValue(x => x.EMSLProposalUsers.Count).Subscribe(_ => SettingsChanged = true);
        }
        #endregion

        #region Properties

        private string selectedEMSLUsageType;
        private string emslProposalID;
        private string experimentName;
        private bool isNotMonitoring;
        private string datasetNameMatch;
        private QcMonitorData selectedQcMonitor;
        private bool datasetNameMatchIsDuplicate;

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> AddQcMonitorCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveQcMonitorCommand { get; }

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public bool SettingsChanged { get; set; }

        public string EMSLUsageType
        {
            get => selectedEMSLUsageType;
            set => this.RaiseAndSetIfChangedMonitored(ref selectedEMSLUsageType, value);
        }

        public string EMSLProposalID
        {
            get => emslProposalID;
            set => this.RaiseAndSetIfChangedMonitored(ref emslProposalID, value);
        }

        public string ExperimentName
        {
            get => experimentName;
            set => this.RaiseAndSetIfChangedMonitored(ref experimentName, value);
        }

        public DatasetManager Manager => DatasetManager.Manager;

        public ReactiveList<ProposalUser> EMSLProposalUsers { get; } = new ReactiveList<ProposalUser>();

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

        public bool DatasetNameMatchIsDuplicate
        {
            get => datasetNameMatchIsDuplicate;
            private set => this.RaiseAndSetIfChanged(ref datasetNameMatchIsDuplicate, value);
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
                EmslUsageType = EMSLUsageType,
                DatasetNameMatch = DatasetNameMatch
            };

            if (EMSLUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
            {
                qcMonitor.EmslProposalId = EMSLProposalID;
                using (qcMonitor.EmslProposalUsers.SuppressChangeNotifications())
                {
                    qcMonitor.EmslProposalUsers.AddRange(EMSLProposalUsers);
                }
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
        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            // Still save the changes here...
            Settings.Default.QC_ExperimentName = ExperimentName;
            Settings.Default.QC_ProposalID = EMSLProposalID;
            Settings.Default.QC_SelectedUsageType = EMSLUsageType;

            var selectedEMSLUsers = new System.Collections.Specialized.StringCollection();
            foreach (var user in EMSLProposalUsers)
                selectedEMSLUsers.Add(user.UserID.ToString());

            Settings.Default.QC_EMSL_Users = selectedEMSLUsers;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.QC_ExperimentName;
            EMSLProposalID = Settings.Default.QC_ProposalID;
            EMSLUsageType = Settings.Default.QC_SelectedUsageType;

            List<string> selectedUsers;
            if (Settings.Default.QC_EMSL_Users == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.QC_EMSL_Users.Cast<string>().ToList();

            using (EMSLProposalUsers.SuppressChangeNotifications())
            {
                EMSLProposalUsers.Clear();
                EMSLProposalUsers.AddRange(DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers));
            }

            if (!string.IsNullOrWhiteSpace(ExperimentName) && string.IsNullOrWhiteSpace(Settings.Default.QC_Monitors))
            {
                var monitor = new QcMonitorData()
                {
                    ExperimentName = ExperimentName,
                    EmslUsageType = EMSLUsageType,
                    DatasetNameMatch = "*"
                };

                if (!string.IsNullOrWhiteSpace(monitor.EmslUsageType) && monitor.EmslUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
                {
                    monitor.EmslProposalId = EMSLProposalID;
                    using (monitor.EmslProposalUsers.SuppressChangeNotifications())
                    {
                        monitor.EmslProposalUsers.AddRange(EMSLProposalUsers);
                    }
                }

                Manager.QcMonitors.Add(monitor);
            }

            SettingsChanged = false;
        }
        #endregion
    }
}
