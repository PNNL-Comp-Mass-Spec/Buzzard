using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using BuzzardWPF.Data;
using BuzzardWPF.Properties;
using DynamicData.Binding;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public sealed class DatasetMonitor : ReactiveObject, IStoredSettingsMonitor, IDisposable
    {
        // Ignore Spelling: uncheck

        static DatasetMonitor()
        {
            Monitor = new DatasetMonitor();
        }

        private DatasetMonitor()
        {
            TriggerFileCreationWaitTime = 5;

            SetupTimers();
        }

        public static DatasetMonitor Monitor { get; }

        private const int DefaultTriggerCreationWaitTimeMinutes = 15;

        private Timer mScannedDatasetTimer;
        private Timer mTriggerCountdownTimer;
        private readonly ConcurrentDictionary<BuzzardDataset, bool> triggerCountdownDatasets = new ConcurrentDictionary<BuzzardDataset, bool>(3, 10);

        private bool createTriggerOnDmsFail;
        private bool qcCreateTriggerOnDmsFail;
        private int triggerFileCreationWaitTime;

        public const string EXPERIMENT_NAME_DESCRIPTION = "Experiment";
        public const string WorkPackageDescription = "Work Package (use 'none' for no work package)";
        public const string QC_MONITORS_DESCRIPTION = "QC Monitor(s) (or uncheck uploading \"QCs with 'QC Samples' metadata\")";
        public const string EmslUsageTypeDescription = "EMSL Usage Type";
        public const string EmslProposalIdDescription = "EMSL Proposal ID";

        public bool SettingsChanged { get; set; }

        public WatcherMetadata WatcherMetadata => DatasetManager.Manager.WatcherMetadata;
        public DatasetManager Manager => DatasetManager.Manager;
        public TriggerFileMonitor TriggerMonitor => TriggerFileMonitor.Instance;

        public ObservableCollectionExtended<QcMonitorData> QcMonitors { get; } = new ObservableCollectionExtended<QcMonitorData>();

        /// <summary>
        /// This values tells the DatasetManager if it can create
        /// a trigger file for datasets that fail to resolve their
        /// DMS data. This only applies when the reason for the
        /// trigger file creation is due to the count down running
        /// out. If a user wants to create the trigger file without
        /// DMS data, we won't stop them.
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public bool CreateTriggerOnDMSFail
        {
            get => createTriggerOnDmsFail;
            set => this.RaiseAndSetIfChangedMonitored(ref createTriggerOnDmsFail, value);
        }

        /// <summary>
        /// This is the amount of time that we should wait before
        /// creating a trigger file for a dataset that was found by the scanner.
        /// </summary>
        /// <remarks>
        /// This is measured in minutes.
        /// </remarks>
        /// <remarks>
        /// The Watcher control is responsible for setting this.
        /// </remarks>
        public int TriggerFileCreationWaitTime
        {
            get => triggerFileCreationWaitTime;
            set => this.RaiseAndSetIfChangedMonitored(ref triggerFileCreationWaitTime, value);
        }

        public bool QcCreateTriggerOnDMSFail
        {
            get => qcCreateTriggerOnDmsFail;
            set => this.RaiseAndSetIfChangedMonitored(ref qcCreateTriggerOnDmsFail, value);
        }

        public void ResetToDefaults()
        {
            TriggerFileCreationWaitTime = DefaultTriggerCreationWaitTimeMinutes;
            CreateTriggerOnDMSFail = false;
        }

        private void SetupTimers()
        {
            // Update every 5 seconds
            mScannedDatasetTimer = new Timer(ScannedDatasetTimer_Tick, this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            // Update countdown every half second
            mTriggerCountdownTimer = new Timer(CountdownTimer_Tick, this, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5));
        }

        /// <summary>
        /// This will keep the UI components of the Datasets that are found by the scanner up to date.
        /// </summary>
        private void ScannedDatasetTimer_Tick(object state)
        {
            try
            {
                // Find the datasets that have source data found by the file watcher.
                var datasets = Manager.Datasets.Items.Where(ds => ds.DatasetSource == DatasetSource.Watcher).ToList();

                // If there aren't any, then we're done.
                if (datasets.Count == 0)
                {
                    return;
                }

                var now = DateTime.Now;
                var timeToWait = new TimeSpan(0, TriggerFileCreationWaitTime, 0);

                var datasetsToCheck = datasets.Where(item =>
                    item.DatasetStatus != DatasetStatus.TriggerFileSent &&
                    item.DatasetStatus != DatasetStatus.Ignored &&
                    item.DatasetStatus != DatasetStatus.DatasetAlreadyInDMS
                    ).ToList();

                RxApp.MainThreadScheduler.Schedule(() => {
                    foreach (var dataset in datasetsToCheck)
                    {
                        var datasetName = dataset.DmsData.DatasetName;

                        if (DMS_DataAccessor.Instance.CheckDatasetExists(datasetName))
                        {
                            dataset.DatasetStatus = DatasetStatus.DatasetAlreadyInDMS;
                            continue;
                        }

                        try
                        {
                            // Also make sure that the trigger file does not exist on the server...
                            if (TriggerMonitor.CheckForTriggerFile(dataset.DmsData.DatasetName))
                            {
                                dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                                continue;
                            }

                            if (!dataset.UpdateFileProperties())
                            {
                                dataset.DatasetStatus = DatasetStatus.FileNotFound;
                                continue;
                            }

                            if ((dataset.FileSize / 1024d) < Manager.Config.MinimumSizeKB)
                            {
                                dataset.DatasetStatus = DatasetStatus.PendingFileSize;
                                continue;
                            }

                            if (DateTime.UtcNow.Subtract(dataset.FileLastChangedUtc).TotalSeconds < 60 ||
                                Manager.DatasetHasAcquisitionLock(dataset.FilePath))
                            {
                                dataset.DatasetStatus = DatasetStatus.PendingFileStable;
                                continue;
                            }

                            if (dataset.DatasetStatus == DatasetStatus.FileNotFound ||
                                dataset.DatasetStatus == DatasetStatus.PendingFileSize ||
                                dataset.DatasetStatus == DatasetStatus.PendingFileStable)
                            {
                                dataset.DatasetStatus = DatasetStatus.Pending;
                            }

                            var timeWaited = now - dataset.RunFinish;
                            if (timeWaited >= timeToWait)
                            {
                                triggerCountdownDatasets.TryRemove(dataset, out _);
                                CreateTriggerFileForDataset(dataset);
                            }
                            else
                            {
                                triggerCountdownDatasets.TryAdd(dataset, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (string.IsNullOrWhiteSpace(datasetName))
                            {
                                datasetName = "??";
                            }

                            ApplicationLogger.LogError(
                            0,
                            "Exception in ScannedDatasetTimer_Tick for dataset " + datasetName, ex);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(
                       0,
                       "Exception in ScannedDatasetTimer_Tick (general)", ex);
            }
        }

        /// <summary>
        /// This will keep the trigger file creation timers updated every second.
        /// </summary>
        private void CountdownTimer_Tick(object state)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var now = DateTime.Now;
                var totalSecondsToWait = TriggerFileCreationWaitTime * 60;
                foreach (var dataset in triggerCountdownDatasets.Keys)
                {
                    var timeWaited = now - dataset.RunFinish;

                    // If it's not time to create the trigger file, then update
                    // the display telling the user when it will be created.
                    var secondsLeft = Math.Max(totalSecondsToWait - timeWaited.TotalSeconds, 0);
                    var percentWaited = 100 * timeWaited.TotalSeconds / totalSecondsToWait;

                    dataset.SecondsTillTriggerCreation = Convert.ToInt32(secondsLeft);
                    dataset.WaitTimePercentage = percentWaited;

                    if (dataset.DatasetStatus == DatasetStatus.TriggerFileSent ||
                        dataset.DatasetStatus == DatasetStatus.PendingFileStable ||
                        dataset.DatasetSource == DatasetSource.Searcher ||
                        totalSecondsToWait - timeWaited.TotalSeconds < 0)
                    {
                        // Different reasons to remove this from the list; if it actually should be here, it will be re-added within 10 seconds
                        triggerCountdownDatasets.TryRemove(dataset, out var _);
                    }
                }
            });
        }

        private void CreateTriggerFileForDataset(BuzzardDataset dataset)
        {
            var datasetName = dataset.DmsData.DatasetName;

            try
            {
                if (!dataset.DmsData.LockData)
                {
                    Manager.ResolveDms(dataset, true);
                }

                if (dataset.IsQC || dataset.IsBlank)
                {
                    if (!QcCreateTriggerOnDMSFail || !dataset.MatchedMonitor)
                    {
                        return;
                    }
                }
                else
                {
                    if (!dataset.DmsData.LockData && !CreateTriggerOnDMSFail)
                    {
                        return;
                    }
                }

                var triggerFilePath = DatasetManager.CreateTriggerFileBuzzard(dataset, forceSend: false, preview: false);
                if (string.IsNullOrWhiteSpace(triggerFilePath))
                {
                    return;
                }

                var fiTriggerFile = new FileInfo(triggerFilePath);
                TriggerMonitor.AddNewTriggerFile(fiTriggerFile.FullName);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrWhiteSpace(datasetName))
                {
                    datasetName = "??";
                }

                ApplicationLogger.LogError(
                    0,
                    "Exception in CreateTriggerFileForDataset for dataset " + datasetName, ex);
            }
        }

        public List<string> GetMissingRequiredFields()
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(WatcherMetadata.Instrument))
            {
                missingFields.Add("Instrument");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.CartName))
            {
                missingFields.Add("LC Cart");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.CartConfigName))
            {
                missingFields.Add("LC Cart Config");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.SeparationType))
            {
                missingFields.Add("Separation Type");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.DatasetType))
            {
                missingFields.Add("Dataset Type");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.InstrumentOperator))
            {
                missingFields.Add("Operator");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.ExperimentName))
            {
                missingFields.Add(EXPERIMENT_NAME_DESCRIPTION);
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.WorkPackage))
            {
                missingFields.Add(WorkPackageDescription);
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.LCColumn))
            {
                missingFields.Add("LC Column");
            }
            else if (!DMS_DataAccessor.Instance.ColumnData.Contains(WatcherMetadata.LCColumn))
            {
                missingFields.Add("Invalid LC Column name");
            }

            if (string.IsNullOrWhiteSpace(WatcherMetadata.EMSLUsageType))
            {
                missingFields.Add(EmslUsageTypeDescription);
            }
            else if (WatcherMetadata.EMSLUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(WatcherMetadata.EMSLProposalID))
            {
                missingFields.Add(EmslProposalIdDescription);
            }

            if (QcMonitors.Count == 0)
            {
                missingFields.Add(QC_MONITORS_DESCRIPTION);
            }

            return missingFields;
        }

        public bool SaveSettings(bool force = false)
        {
            if (QcMonitors.Any())
            {
                QcMonitorData.SaveSettings(QcMonitors);
            }

            WatcherMetadata.SaveSettings(force);

            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.WatcherQCCreateTriggerOnDMSFail = QcCreateTriggerOnDMSFail;
            Settings.Default.WatcherCreateTriggerOnDMSFail = CreateTriggerOnDMSFail;
            Settings.Default.Watcher_WaitTime = TriggerFileCreationWaitTime;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            QcCreateTriggerOnDMSFail = Settings.Default.WatcherQCCreateTriggerOnDMSFail;

            if (!string.IsNullOrWhiteSpace(Settings.Default.WatcherQCMonitors))
            {
                QcMonitors.AddRange(QcMonitorData.LoadSettings());
            }

            CreateTriggerOnDMSFail = Settings.Default.WatcherCreateTriggerOnDMSFail;
            TriggerFileCreationWaitTime = Settings.Default.Watcher_WaitTime;

            WatcherMetadata.LoadSettings();

            SettingsChanged = false;
        }

        public void Dispose()
        {
            mScannedDatasetTimer?.Dispose();
            mTriggerCountdownTimer?.Dispose();
        }
    }
}
