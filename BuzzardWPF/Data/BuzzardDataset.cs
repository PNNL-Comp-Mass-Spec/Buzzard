using System;
using System.IO;
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
        private string m_filePath;

        private string m_instrument;
        private string m_operator;
        private string m_separationType;
        private string m_cartName;
        private string m_cartConfigName;

        private DMSData m_dmsData;

        private bool m_notOnlySource;
        private DatasetSource m_datasetSource;
        private TriggerFileStatus m_triggerFileStatus;
        private string m_experimentName;
        private string m_comment = string.Empty;
        private string m_lcColumn;
        private string mCaptureSubfolderPath = string.Empty;

        private bool m_isQC;

        /// <summary>
        /// Status of the dataset.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        private DatasetStatus m_status;

        private string m_TriggerCreationWarning;

        private long m_fileSize;
        private DateTime m_runStart;
        private DateTime m_runFinish;

        private int m_secondsRemainingTillTriggerCreation;
        private double m_waitTimePercentage;

        private bool m_PulseText;
        private string m_interestRating;

        private bool m_CartConfigStatus;
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
        }
        #endregion

        #region UI data place holders
        public bool PulseText
        {
            get => m_PulseText;
            set => this.RaiseAndSetIfChanged(ref m_PulseText, value);
        }

        public int SecondsTillTriggerCreation
        {
            get => m_secondsRemainingTillTriggerCreation;
            set => this.RaiseAndSetIfChanged(ref m_secondsRemainingTillTriggerCreation, value);
        }

        public double WaitTimePercentage
        {
            get => m_waitTimePercentage;
            set => this.RaiseAndSetIfChanged(ref m_waitTimePercentage, value);
        }
        #endregion

        #region Datagrid Properties

        public ReactiveList<ProposalUser> EMSLProposalUsers { get; } = new ReactiveList<ProposalUser>();

        public string LCColumn
        {
            get => m_lcColumn;
            set => this.RaiseAndSetIfChanged(ref m_lcColumn, value);
        }

        public string Comment
        {
            get => m_comment;
            set => this.RaiseAndSetIfChanged(ref m_comment, value);
        }

        public string CaptureSubfolderPath
        {
            get => mCaptureSubfolderPath;
            set => this.RaiseAndSetIfChanged(ref mCaptureSubfolderPath, value);
        }

        public TriggerFileStatus TriggerFileStatus
        {
            get => m_triggerFileStatus;
            set => this.RaiseAndSetIfChanged(ref m_triggerFileStatus, value);
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
            get => m_datasetSource;
            set => this.RaiseAndSetIfChanged(ref m_datasetSource, value);
        }

        public bool IsMonitored => isMonitored.Value;

        public ReactiveCommand<Unit, Unit> ToggleMonitoringCommand { get; }

        public string ExperimentName
        {
            get => m_experimentName;
            set => this.RaiseAndSetIfChanged(ref m_experimentName, value);
        }

        public bool IsQC
        {
            get => m_isQC;
            set => this.RaiseAndSetIfChanged(ref m_isQC, value);
        }

        /// <summary>
        /// If there's another dataset with the same Request Name,
        /// but came from different source data, then someone should
        /// set this to True.
        /// </summary>
        public bool NotOnlyDatasource
        {
            get => m_notOnlySource;
            set => this.RaiseAndSetIfChanged(ref m_notOnlySource, value);
        }

        public string Instrument
        {
            get => m_instrument;
            set => this.RaiseAndSetIfChanged(ref m_instrument, value);
        }

        public string Operator
        {
            get => m_operator;
            set => this.RaiseAndSetIfChanged(ref m_operator, value);
        }

        public string SeparationType
        {
            get => m_separationType;
            set => this.RaiseAndSetIfChanged(ref m_separationType, value);
        }

        public string CartName
        {
            get => m_cartName;
            set => this.RaiseAndSetIfChanged(ref m_cartName, value, x => ValidateCartConfig());
        }

        public string CartConfigName
        {
            get => m_cartConfigName;
            set => this.RaiseAndSetIfChanged(ref m_cartConfigName, value, x => ValidateCartConfig());
        }

        public DMSData DmsData
        {
            get => m_dmsData;
            set => this.RaiseAndSetIfChanged(ref m_dmsData, value, x => this.RaisePropertyChanged(nameof(DMSStatus)));
        }

        public DateTime DMSDataLastUpdate { get; set; }

        public string InterestRating
        {
            get => m_interestRating;
            set => this.RaiseAndSetIfChanged(ref m_interestRating, value);
        }

        /// <summary>
        /// Gets or sets the status of the trigger file.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public DatasetStatus DatasetStatus
        {
            get => m_status;
            set => this.RaiseAndSetIfChanged(ref m_status, value);
        }

        public string TriggerCreationWarning
        {
            get => m_TriggerCreationWarning;
            set => this.RaiseAndSetIfChanged(ref m_TriggerCreationWarning, value);
        }

        #endregion

        public bool CartConfigStatus
        {
            get => m_CartConfigStatus;
            set => this.RaiseAndSetIfChanged(ref m_CartConfigStatus, value);
        }

        private void ValidateCartConfig()
        {
            if (m_cartConfigName != null)
            {
                CartConfigStatus = !string.IsNullOrEmpty(m_cartName) &&
                                        !m_cartConfigName.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase) &&
                                        !m_cartConfigName.StartsWith(m_cartName, StringComparison.OrdinalIgnoreCase);
            }
        }

        #region File Properties
        public string FilePath
        {
            get => m_filePath;
            set => this.RaiseAndSetIfChanged(ref m_filePath, value, x =>
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
            get => m_runStart;
            private set => this.RaiseAndSetIfChanged(ref m_runStart, value);
        }

        public DateTime RunFinish
        {
            get => m_runFinish;
            set
            {
                if (m_runFinish < value)
                {
                    this.RaiseAndSetIfChanged(ref m_runFinish, value);
                }
            }
        }

        public long FileSize
        {
            get => m_fileSize;
            private set => this.RaiseAndSetIfChanged(ref m_fileSize, value);
        }

        public DateTime FileLastChangedUtc { get; private set; }

        /// <summary>
        /// True if the dataset is a single file, otherwise false
        /// </summary>
        public bool IsFile { get; set; }

        #endregion

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

        #region Members blindly brought over from classDataset
        /// <summary>
        /// Gets or sets the flag if the dataset should be ignored.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public bool ShouldIgnore { get; set; }

        /// <summary>
        /// Gets or sets the name of the dataset
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public string Name
        {
            get;
            set;
        }
        #endregion
    }
}
