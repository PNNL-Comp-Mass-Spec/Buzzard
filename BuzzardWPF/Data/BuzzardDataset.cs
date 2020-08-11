using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.Data
{
    public class BuzzardDataset : ReactiveObject, ITriggerFileData, IDisposable
    {
        #region Attributes
        private string filePath;

        private string instrumentName;
        private string instrumentOperator;
        private string separationType;

        private bool notOnlySource;
        private DatasetSource datasetSource;
        private TriggerFileStatus triggerFileStatus;
        private string columnName;
        private string captureShareName = string.Empty;
        private string captureSubdirectoryPath = string.Empty;

        private bool isQC;
        private bool isBlank;
        private bool matchedMonitor;

        /// <summary>
        /// Status of the dataset.
        /// </summary>
        private DatasetStatus status;

        private readonly ObservableAsPropertyHelper<DMSStatus> dmsStatus;

        private string triggerCreationWarning;

        private long fileSize;
        private DateTime runStart;
        private DateTime runFinish;

        private int secondsUntilTriggerCreation;
        private double waitTimePercentage;
        private string interestRating;
        private ProposalUser emslProposalUser;

        private bool cartConfigError;
        private DateTime lastRecordedLastWriteTime;
        private readonly ObservableAsPropertyHelper<bool> isMonitored;
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        private readonly ObservableAsPropertyHelper<string> emslProjectText;
        private readonly ObservableAsPropertyHelper<string> emslUserProposalText;
        private readonly ObservableAsPropertyHelper<bool> showProgress;
        private readonly ObservableAsPropertyHelper<double> progressValue;
        private readonly ObservableAsPropertyHelper<string> formattedStatus;

        #endregion

        #region Initialization

        public BuzzardDataset()
        {
            DmsData = new DMSData();
            DMSDataLastUpdate = DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0));

            NotOnlyDatasource = false;
            DatasetSource = DatasetSource.Searcher;
            DatasetStatus = DatasetStatus.Pending;

            WaitTimePercentage = 0;
            SecondsTillTriggerCreation = -1;
            InterestRating = "Unreviewed";

            IsFile = true;
            isMonitored = this.WhenAnyValue(x => x.DatasetSource).Select(x => x == DatasetSource.Watcher).ToProperty(this, x => x.IsMonitored);
            showProgress = this.WhenAnyValue(x => x.DatasetStatus)
                .Select(x =>
                    x == DatasetStatus.Pending || x == DatasetStatus.PendingFileStable ||
                    x == DatasetStatus.PendingFileSize || x == DatasetStatus.TriggerFileSent)
                .ToProperty(this, x => x.ShowProgress);
            progressValue = this.WhenAnyValue(x => x.DatasetStatus, x => x.DatasetSource, x => x.WaitTimePercentage)
                .Select(x =>
                    {
                        if (x.Item1 == DatasetStatus.TriggerFileSent)
                        {
                            return 100.0;
                        }

                        if (x.Item1 == DatasetStatus.PendingFileStable || x.Item2 == DatasetSource.Searcher)
                        {
                            return 0.0;
                        }

                        return x.Item3;
                    }).ToProperty(this, x => x.ProgressValue);
            ToggleMonitoringCommand = ReactiveCommand.Create(ToggleMonitoring);
            disposables.Add(this.WhenAnyValue(x => x.EMSLProposalUser).Subscribe(_ => DmsData.EMSLProposalUser = EMSLProposalUser?.UserID.ToString() ?? ""));
            disposables.Add(this.WhenAnyValue(x => x.DmsData, x => x.DmsData.CartName, x => x.DmsData.CartConfigName)
                .ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => ValidateCartConfig()));
            dmsStatus = this.WhenAnyValue(x => x.DmsData, x => x.DmsData.LockData)
                .Select(x => x.Item2 ? DMSStatus.DMSResolved : DMSStatus.NoDMSRequest)
                .ToProperty(this, x => x.DMSStatus);
            emslProjectText =
                this.WhenAnyValue(x => x.DmsData.EMSLUsageType, x => x.DmsData.EMSLProposalID).Select(x => x.Item1 == "USER" ? $"USER: {x.Item2}" : x.Item1)
                    .ToProperty(this, x => x.EmslProjectText);
            emslUserProposalText =
                this.WhenAnyValue(x => x.DmsData.EMSLUsageType, x => x.DmsData.EMSLProposalID,
                        x => x.EMSLProposalUser).Select(x => x.Item1 == "USER" ? $"ProposalID: {x.Item2}\nEMSL User: {x.Item3?.UserName}" : null)
                    .ToProperty(this, x => x.EmslUserProposalText);
            formattedStatus = this.WhenAnyValue(x => x.SecondsTillTriggerCreation, x => x.DatasetStatus, x => x.DatasetSource)
                .Select(x => FormatStatus(x.Item1, x.Item2, x.Item3))
                .ToProperty(this, x => x.FormattedStatus);
        }

        public void Dispose()
        {
            dmsStatus?.Dispose();
            isMonitored?.Dispose();
            emslProjectText?.Dispose();
            emslUserProposalText?.Dispose();
            ToggleMonitoringCommand?.Dispose();
            showProgress?.Dispose();
            progressValue?.Dispose();
            formattedStatus?.Dispose();
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public void Reset()
        {
            DMSDataLastUpdate = DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0));
            NotOnlyDatasource = false;
            DatasetSource = DatasetSource.Searcher;
            DatasetStatus = DatasetStatus.Pending;
            WaitTimePercentage = 0;
            SecondsTillTriggerCreation = -1;
            InterestRating = "Unreviewed";
            IsFile = true;
            ColumnName = "";
            CaptureShareName = "";
            CaptureSubdirectoryPath = "";
            TriggerFileStatus = TriggerFileStatus.Pending;
            IsQC = false;
            IsBlank = false;
            MatchedMonitor = false;
            InstrumentName = "";
            Operator = "";
            SeparationType = "";
            TriggerCreationWarning = "";
            CartConfigError = false;
            FilePath = "";
            RunStart = default(DateTime);
            runFinish = default(DateTime);
            RunFinish = RunStart.AddSeconds(1);
            FileSize = 0;
            FileLastChangedUtc = default(DateTime);
            DmsData.Reset();
        }

        #endregion

        #region UI data place holders

        public int SecondsTillTriggerCreation
        {
            get => secondsUntilTriggerCreation;
            set => this.RaiseAndSetIfChanged(ref secondsUntilTriggerCreation, value);
        }

        public double WaitTimePercentage
        {
            get => waitTimePercentage;
            set => this.RaiseAndSetIfChanged(ref waitTimePercentage, value);
        }
        #endregion

        #region Datagrid Properties

        public ProposalUser EMSLProposalUser
        {
            get => emslProposalUser;
            set => this.RaiseAndSetIfChanged(ref emslProposalUser, value);
        }

        public string ColumnName
        {
            get => columnName;
            set => this.RaiseAndSetIfChanged(ref columnName, value);
        }

        public string CaptureShareName
        {
            get => captureShareName;
            set => this.RaiseAndSetIfChanged(ref captureShareName, value);
        }

        public string CaptureSubdirectoryPath
        {
            get => captureSubdirectoryPath;
            set => this.RaiseAndSetIfChanged(ref captureSubdirectoryPath, value);
        }

        public TriggerFileStatus TriggerFileStatus
        {
            get => triggerFileStatus;
            set => this.RaiseAndSetIfChanged(ref triggerFileStatus, value);
        }

        public DMSStatus DMSStatus => dmsStatus.Value;

        public DatasetSource DatasetSource
        {
            get => datasetSource;
            set => this.RaiseAndSetIfChanged(ref datasetSource, value);
        }

        public bool IsMonitored => isMonitored.Value;
        public string EmslProjectText => emslProjectText.Value;
        public string EmslUserProposalText => emslUserProposalText.Value;
        public bool ShowProgress => showProgress.Value;
        public double ProgressValue => progressValue.Value;
        public string FormattedStatus => formattedStatus.Value;

        public ReactiveCommand<Unit, Unit> ToggleMonitoringCommand { get; }

        /// <summary>
        /// Dataset is a QC dataset, i.e. the dataset name starts with "QC_" or "QC-"
        /// </summary>
        public bool IsQC
        {
            get => isQC;
            set => this.RaiseAndSetIfChanged(ref isQC, value);
        }

        /// <summary>
        /// Dataset is a blank dataset, i.e. the dataset name starts with "Blank_" or "Blank-"
        /// </summary>
        public bool IsBlank
        {
            get => isBlank;
            set => this.RaiseAndSetIfChanged(ref isBlank, value);
        }

        /// <summary>
        /// If dataset is QC or blank, this is set to true if the dataset was matched to a monitor. This is used to prevent uploading QCs or blanks that did not match a monitor.
        /// </summary>
        public bool MatchedMonitor
        {
            get => matchedMonitor;
            set => this.RaiseAndSetIfChanged(ref matchedMonitor, value);
        }

        /// <summary>
        /// If there's another dataset with the same Request Name,
        /// but came from different source data, then someone should
        /// set this to True.
        /// </summary>
        public bool NotOnlyDatasource
        {
            get => notOnlySource;
            set => this.RaiseAndSetIfChanged(ref notOnlySource, value);
        }

        public string InstrumentName
        {
            get => instrumentName;
            set => this.RaiseAndSetIfChanged(ref instrumentName, value);
        }

        public string Operator
        {
            get => instrumentOperator;
            set => this.RaiseAndSetIfChanged(ref instrumentOperator, value);
        }

        public string SeparationType
        {
            get => separationType;
            set => this.RaiseAndSetIfChanged(ref separationType, value);
        }

        public DMSData DmsData { get; }

        public DateTime DMSDataLastUpdate { get; set; }

        public string InterestRating
        {
            get => interestRating;
            set => this.RaiseAndSetIfChanged(ref interestRating, value);
        }

        /// <summary>
        /// Gets or sets the status of the trigger file.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public DatasetStatus DatasetStatus
        {
            get => status;
            set => this.RaiseAndSetIfChanged(ref status, value);
        }

        public string TriggerCreationWarning
        {
            get => triggerCreationWarning;
            set => this.RaiseAndSetIfChanged(ref triggerCreationWarning, value);
        }

        #endregion

        public bool CartConfigError
        {
            get => cartConfigError;
            set => this.RaiseAndSetIfChanged(ref cartConfigError, value);
        }

        private void ValidateCartConfig()
        {
            if (DmsData?.CartConfigName != null)
            {
                if (string.IsNullOrWhiteSpace(DmsData.CartName))
                {
                    CartConfigError = false;
                    return;
                }

                CartConfigError = !DMS_DataAccessor.Instance.GetCartConfigNamesForCart(DmsData.CartName).Contains(DmsData.CartConfigName);
            }
        }

        #region File Properties
        public string FilePath
        {
            get => filePath;
            set => this.RaiseAndSetIfChanged(ref filePath, value, x =>
            {
                this.RaisePropertyChanged(nameof(Extension));
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    UpdateFileProperties();
                }
            });
        }

        public string Extension
        {
            get
            {
                string extension = null;
                if (FilePath != null)
                {
                    extension = Path.GetExtension(FilePath);
                }
                return extension;
            }
        }

        public DateTime RunStart
        {
            get => runStart;
            private set => this.RaiseAndSetIfChanged(ref runStart, value);
        }

        public DateTime RunFinish
        {
            get => runFinish;
            set
            {
                if (runFinish < value)
                {
                    this.RaiseAndSetIfChanged(ref runFinish, value);
                }
            }
        }

        public long FileSize
        {
            get => fileSize;
            private set => this.RaiseAndSetIfChanged(ref fileSize, value);
        }

        public DateTime FileLastChangedUtc { get; private set; }

        /// <summary>
        /// True if the dataset is a single file, otherwise false
        /// </summary>
        public bool IsFile { get; set; }

        #endregion

        private static string FormatStatus(int waitSeconds, DatasetStatus status, DatasetSource source)
        {
            switch (status)
            {
                case DatasetStatus.TriggerFileSent:
                    return "Trigger File Sent";
                case DatasetStatus.DatasetMarkedCaptured:
                    return "Dataset Captured";
                case DatasetStatus.FailedFileError:
                    return "File Error";
                case DatasetStatus.FailedAmbiguousDmsRequest:
                    return "Matches Multiple Requests";
                case DatasetStatus.FailedNoDmsRequest:
                    return "No DMS Request";
                case DatasetStatus.FailedUnknown:
                    return "Error";
                case DatasetStatus.MissingRequiredInfo:
                    return "Warning";
                case DatasetStatus.FileNotFound:
                    return "File Missing";
                case DatasetStatus.ValidatingStable:
                    return "In Progress";
                case DatasetStatus.TriggerAborted:
                    return "Aborted manual trigger";
                case DatasetStatus.FileSizeChanged:
                    return "Aborted, size changed";
                case DatasetStatus.DatasetAlreadyInDMS:
                    return "Already in DMS";
                case DatasetStatus.PendingFileStable:
                    return "Waiting for stable file";
                case DatasetStatus.PendingFileSize:
                    return "Waiting for file size";
            }

            if (source == DatasetSource.Searcher)
            {
                return "Waiting on User";
            }

            var minutes = waitSeconds / 60;
            var seconds = waitSeconds % 60;

            if (minutes >= 60)
            {
                var hours = minutes / 60;
                minutes %= 60;
                return $"Waiting: {hours}:{minutes:D2}:{seconds:D2}";
            }

            return $"Waiting: {minutes:D2}:{seconds:D2} ";
        }

        private (bool Exists, long Size, int FileCount, DateTime CreationTime, DateTime LastWriteTime) GetDatasetStats()
        {
            var exists = false;
            var size = 0L;
            var fileCount = 0;
            var creationTime = DateTime.MinValue;
            var lastWriteTime = DateTime.MinValue;
            if (File.Exists(FilePath))
            {
                exists = true;
                var info = new FileInfo(FilePath);

                size = info.Length;
                fileCount = 1;
                creationTime = info.CreationTime;
                lastWriteTime = info.LastWriteTime;
            }
            else if (Directory.Exists(FilePath))
            {
                exists = true;
                var info = new DirectoryInfo(FilePath);

                fileCount = 0;
                size = 0;

                creationTime = info.CreationTime;
                lastWriteTime = info.LastWriteTime;

                foreach (var file in info.GetFiles("*", SearchOption.AllDirectories))
                {
                    fileCount++;

                    if (file.LastWriteTime > lastWriteTime)
                    {
                        lastWriteTime = file.LastWriteTime;
                    }

                    // For some reason, 'file' does not get a refreshed size, but the LastWriteTime does get updated
                    var fileInfo = new FileInfo(file.FullName);
                    size += fileInfo.Length;
                }
            }

            return (exists, size, fileCount, creationTime, lastWriteTime);
        }

        /// <summary>
        /// This method reads a fresh FileInfo object on the FilePath, and
        /// copies the relevant parts into the Dataset's properties.
        /// </summary>
        /// <returns>True if the file or folder exists; otherwise false</returns>
        public bool UpdateFileProperties()
        {
            var info = GetDatasetStats();
            if (!info.Exists)
            {
                FileSize = 0;
                RunStart = DateTime.MinValue;
                RunFinish = DateTime.MinValue;
                DatasetStatus = DatasetStatus.FileNotFound;
                FileLastChangedUtc = DateTime.UtcNow;
                return false;
            }

            if (FileSize != info.Size || lastRecordedLastWriteTime != info.LastWriteTime)
            {
                FileLastChangedUtc = DateTime.UtcNow;
            }

            lastRecordedLastWriteTime = info.LastWriteTime;
            FileSize = info.Size;
            RunStart = info.CreationTime;
            RunFinish = info.LastWriteTime;

            if (info.CreationTime > info.LastWriteTime)
            {
                // Sanity check for moved datasets.
                RunStart = info.LastWriteTime;
            }

            return true;
        }

        public (bool Exists, long Size, int FileCount) GetFileStats()
        {
            var info = GetDatasetStats();
            return (info.Exists, info.Size, info.FileCount);
        }

        private void ToggleMonitoring()
        {
            if (DatasetSource == DatasetSource.Searcher)
            {
                // Change the dataset status to something that will not trigger the countdown display first.
                DatasetStatus = DatasetStatus.PendingFileStable;
                DatasetSource = DatasetSource.Watcher;
            }
            else
            {
                DatasetSource = DatasetSource.Searcher;
            }
        }
    }
}
