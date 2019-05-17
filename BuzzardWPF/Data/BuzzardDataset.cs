using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.Data
{
    public class BuzzardDataset : ReactiveObject
    {
        #region Attributes
        private string filePath;

        private string instrument;
        private string instrumentOperator;
        private string separationType;

        private DMSData dmsData;

        private bool notOnlySource;
        private DatasetSource datasetSource;
        private TriggerFileStatus triggerFileStatus;
        private string comment = string.Empty;
        private string lcColumn;
        private string captureSubfolderPath = string.Empty;

        private bool isQC;

        /// <summary>
        /// Status of the dataset.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        private DatasetStatus status;

        private string triggerCreationWarning;

        private long fileSize;
        private DateTime runStart;
        private DateTime runFinish;

        private int secondsUntilTriggerCreation;
        private double waitTimePercentage;

        private bool pulseText;
        private string interestRating;

        private bool cartConfigError;
        private DateTime lastRecordedLastWriteTime;
        private readonly ObservableAsPropertyHelper<bool> isMonitored;

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
            ToggleMonitoringCommand = ReactiveCommand.Create(ToggleMonitoring);
            this.WhenAnyValue(x => x.EMSLProposalUsers, x => x.EMSLProposalUsers.Count).Subscribe(_ => SetEMSLUsersList());
            this.WhenAnyValue(x => x.DmsData, x => x.DmsData.CartName, x => x.DmsData.CartConfigName)
                .ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => ValidateCartConfig());
        }
        #endregion

        #region UI data place holders
        public bool PulseText
        {
            get => pulseText;
            set => this.RaiseAndSetIfChanged(ref pulseText, value);
        }

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

        public ReactiveList<ProposalUser> EMSLProposalUsers { get; } = new ReactiveList<ProposalUser>();

        public string LCColumn
        {
            get => lcColumn;
            set => this.RaiseAndSetIfChanged(ref lcColumn, value);
        }

        public string Comment
        {
            get => comment;
            set => this.RaiseAndSetIfChanged(ref comment, value);
        }

        public string CaptureSubfolderPath
        {
            get => captureSubfolderPath;
            set => this.RaiseAndSetIfChanged(ref captureSubfolderPath, value);
        }

        public TriggerFileStatus TriggerFileStatus
        {
            get => triggerFileStatus;
            set => this.RaiseAndSetIfChanged(ref triggerFileStatus, value);
        }

        public DMSStatus DMSStatus
        {
            get
            {
                if (DmsData.LockData)
                    return DMSStatus.DMSResolved;

                return DMSStatus.NoDMSRequest;
            }
        }

        public DatasetSource DatasetSource
        {
            get => datasetSource;
            set => this.RaiseAndSetIfChanged(ref datasetSource, value);
        }

        public bool IsMonitored => isMonitored.Value;

        public ReactiveCommand<Unit, Unit> ToggleMonitoringCommand { get; }

        public bool IsQC
        {
            get => isQC;
            set => this.RaiseAndSetIfChanged(ref isQC, value);
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

        public string Instrument
        {
            get => instrument;
            set => this.RaiseAndSetIfChanged(ref instrument, value);
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

        public DMSData DmsData
        {
            get => dmsData;
            set => this.RaiseAndSetIfChanged(ref dmsData, value, x => this.RaisePropertyChanged(nameof(DMSStatus)));
        }

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

                if (DMS_DataAccessor.Instance.CartConfigNameMap.TryGetValue(DmsData.CartName, out var list))
                {
                    CartConfigError = !list.Contains(DmsData.CartConfigName);
                }
            }
        }

        #region File Properties
        public string FilePath
        {
            get => filePath;
            set => this.RaiseAndSetIfChanged(ref filePath, value, x =>
            {
                this.RaisePropertyChanged(nameof(Extension));
                UpdateFileProperties();
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

        private void SetEMSLUsersList()
        {
            DmsData.UserList = string.Join(",", EMSLProposalUsers.Select(x => x.UserID));
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
                DatasetSource = DatasetSource.Watcher;
            }
            else
            {
                DatasetSource = DatasetSource.Searcher;
            }
        }
    }
}
