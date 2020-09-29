using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Views;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class QCViewModel : ReactiveObject, IStoredSettingsMonitor
    {
        #region Initialize
        public QCViewModel()
        {
            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);
            ExperimentName = null;

            datasetNameMatchHasError = this.WhenAnyValue(x => x.DatasetNameMatch, x => x.Monitor.QcMonitors, x => x.Monitor.QcMonitors.Count).Select(x =>
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
            AddQcMonitorCommand = ReactiveCommand.Create(AddQcMonitor, this.WhenAnyValue(x => x.ExperimentName, x => x.DatasetNameMatch, x => x.DatasetNameMatchHasError).Select(
                x =>
                {
                    var musts = !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2);
                    if (!musts || x.Item3)
                    {
                        return false;
                    }

                    return true;
                }));

            RemoveQcMonitorCommand = ReactiveCommand.Create(RemoveQcMonitor, this.WhenAnyValue(x => x.SelectedQcMonitor).Select(x => x != null));

            this.WhenAnyValue(x => x.ExperimentName).Subscribe(x => DatasetNameMatch = x);
        }
        #endregion

        #region Properties

        private string experimentName;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;
        private string datasetNameMatch;
        private string datasetNameMatchError;
        private QcMonitorData selectedQcMonitor;
        private readonly ObservableAsPropertyHelper<bool> datasetNameMatchHasError;

        private const string ValidNameMatchRegexString = @"(BLANK|QC)(_|-).*";
        private readonly Regex validNameMatchRegex = new Regex(ValidNameMatchRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> AddQcMonitorCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveQcMonitorCommand { get; }

        public bool SettingsChanged { get; set; }

        public string ExperimentName
        {
            get => experimentName;
            set => this.RaiseAndSetIfChangedMonitored(ref experimentName, value);
        }

        public DatasetMonitor Monitor => DatasetMonitor.Monitor;

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
            var dialogVm = ViewModelCache.Instance.GetExperimentsVm();
            var dialog = new ExperimentsDialogWindow()
            {
                DataContext = dialogVm
            };

            if (dialog.ShowDialog() ?? false)
            {
                ExperimentName = dialogVm.SelectedExperiment.Experiment;
            }
        }

        private void AddQcMonitor()
        {
            var qcMonitor = new QcMonitorData
            {
                ExperimentName = ExperimentName,
                DatasetNameMatch = DatasetNameMatch
            };

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

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.WatcherQCExperimentName;

            if (!string.IsNullOrWhiteSpace(ExperimentName) && string.IsNullOrWhiteSpace(Settings.Default.WatcherQCMonitors))
            {
                var monitor = new QcMonitorData()
                {
                    ExperimentName = ExperimentName,
                    DatasetNameMatch = "*"
                };

                Monitor.QcMonitors.Add(monitor);
            }

            SettingsChanged = false;
        }
        #endregion
    }
}
