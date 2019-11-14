using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
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
            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);

            EmslUsageSelectionVm.BoundContainer = this;

            EMSLUsageType = EmslUsageSelectionVm.UsageTypesSource[1];
            EMSLProposalID = null;
            ExperimentName = null;

            datasetNameMatchHasError = this.WhenAnyValue(x => x.DatasetNameMatch, x => x.Monitor.QcMonitors).Select(x =>
            {
                if (x.Item2.Any(y => y.DatasetNameMatch.Equals(x.Item1)))
                {
                    DatasetNameMatchError = "ERROR: Duplicate dataset name match entry!";
                    return true;
                }

                if (string.IsNullOrWhiteSpace(x.Item1))
                {
                    DatasetNameMatchError = null;
                    return false;
                }

                if (!validNameMatchRegex.IsMatch(x.Item1))
                {
                    DatasetNameMatchError = "ERROR: Dataset name match must start with \"QC\" or \"BLANK\", followed by a '_' or '-'!";
                    return true;
                }

                DatasetNameMatchError = null;

                return false;
            }).ToProperty(this, x => x.DatasetNameMatchHasError);

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            AddQcMonitorCommand = ReactiveCommand.Create(AddQcMonitor, this.WhenAnyValue(x => x.ExperimentName, x => x.DatasetNameMatch, x => x.EMSLUsageType, x => x.EMSLProposalID, x => x.EMSLProposalUsers, x => x.DatasetNameMatchHasError).Select(
                x =>
                {
                    var musts = !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2) && !string.IsNullOrWhiteSpace(x.Item3);
                    if (!musts || x.Item6)
                    {
                        return false;
                    }

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
        private ObservableAsPropertyHelper<bool> isNotMonitoring;
        private string datasetNameMatch;
        private string datasetNameMatchError;
        private QcMonitorData selectedQcMonitor;
        private readonly ObservableAsPropertyHelper<bool> datasetNameMatchHasError;

        private const string ValidNameMatchRegexString = @"(BLANK|QC)(_|-).*";
        private readonly Regex validNameMatchRegex = new Regex(ValidNameMatchRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        public DatasetMonitor Monitor => DatasetMonitor.Monitor;

        public ReactiveList<ProposalUser> EMSLProposalUsers { get; } = new ReactiveList<ProposalUser>();

        public string DatasetNameMatch
        {
            get => datasetNameMatch;
            set => this.RaiseAndSetIfChanged(ref datasetNameMatch, value);
        }

        public string DatasetNameMatchError
        {
            get => datasetNameMatchError;
            private set => this.RaiseAndSetIfChanged(ref datasetNameMatchError, value);
        }

        public bool IsNotMonitoring => isNotMonitoring.Value;

        public QcMonitorData SelectedQcMonitor
        {
            get => selectedQcMonitor;
            set => this.RaiseAndSetIfChanged(ref selectedQcMonitor, value);
        }

        public bool DatasetNameMatchHasError => datasetNameMatchHasError.Value;

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

            Monitor.QcMonitors.Add(qcMonitor);
        }

        private void RemoveQcMonitor()
        {
            if (SelectedQcMonitor == null)
            {
                return;
            }

            Monitor.QcMonitors.Remove(SelectedQcMonitor);
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
            Settings.Default.WatcherQCExperimentName = ExperimentName;
            Settings.Default.WatcherQCEMSLProposalID = EMSLProposalID;
            Settings.Default.WatcherQCEMSLUsageType = EMSLUsageType;

            var selectedEMSLUsers = new System.Collections.Specialized.StringCollection();
            foreach (var user in EMSLProposalUsers)
                selectedEMSLUsers.Add(user.UserID.ToString());

            Settings.Default.WatcherQCEMSLUsers = selectedEMSLUsers;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.WatcherQCExperimentName;
            EMSLProposalID = Settings.Default.WatcherQCEMSLProposalID;
            EMSLUsageType = Settings.Default.WatcherQCEMSLUsageType;

            List<string> selectedUsers;
            if (Settings.Default.WatcherQCEMSLUsers == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.WatcherQCEMSLUsers.Cast<string>().ToList();

            using (EMSLProposalUsers.SuppressChangeNotifications())
            {
                EMSLProposalUsers.Clear();
                EMSLProposalUsers.AddRange(DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers));
            }

            if (!string.IsNullOrWhiteSpace(ExperimentName) && string.IsNullOrWhiteSpace(Settings.Default.WatcherQCMonitors))
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

                Monitor.QcMonitors.Add(monitor);
            }

            SettingsChanged = false;
        }
        #endregion
    }
}
