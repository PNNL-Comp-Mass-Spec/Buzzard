using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using BuzzardLib.LcmsNetTemp;
using LcmsNetDataClasses.Data;

namespace BuzzardLib.Data
{
    public class BuzzardDataset
        : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Attributes
        private string m_filePath;

        private string m_instrument;
        private string m_operator;
        private string m_separationType;
        private string m_cartName;

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
        private ObservableCollection<classProposalUser> m_emslProposalUsers;
        private string m_interestRating;

        #endregion

        #region Initialization
        public BuzzardDataset()
        {
            DMSData = new DMSData();
            DMSDataLastUpdate = DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0));

            NotOnlyDatasource = false;
            DatasetSource = DatasetSource.Searcher;
            DatasetStatus = DatasetStatus.Pending;

            WaitTimePercentage = 0;
            SecondsTillTriggerCreation = -1;
            InterestRating = "Unreviewed";
            EMSLProposalUsers = new ObservableCollection<classProposalUser>();

            IsFile = true;
        }
        #endregion


        #region UI data place holders
        public bool PulseText
        {
            get { return m_PulseText; }
            set
            {
                if (m_PulseText != value)
                {
                    m_PulseText = value;
                    OnPropertyChanged("PulseText");
                }
            }
        }        

        public int SecondsTillTriggerCreation
        {
            get { return m_secondsRemainingTillTriggerCreation; }
            set
            {
                if (m_secondsRemainingTillTriggerCreation != value)
                {
                    m_secondsRemainingTillTriggerCreation = value;
                    OnPropertyChanged("SecondsTillTriggerCreation");
                }
            }
        }

        public double WaitTimePercentage
        {
            get { return m_waitTimePercentage; }
            set
            {
                if (Math.Abs(m_waitTimePercentage - value) > float.Epsilon)
                {
                    m_waitTimePercentage = value;
                    OnPropertyChanged("WaitTimePercentage");
                }
            }
        }
        #endregion

        public ObservableCollection<classProposalUser> EMSLProposalUsers
        {
            get { return m_emslProposalUsers; }
            set
            {
                if (m_emslProposalUsers != value)
                {
                    m_emslProposalUsers = value;
                    OnPropertyChanged("EMSLProposalUsers");
                }
            }
        }

        public string LCColumn
        {
            get { return m_lcColumn; }
            set
            {
                if (m_lcColumn != value)
                {
                    m_lcColumn = value;
                    OnPropertyChanged("LCColumn");
                }
            }
        }
        
        public string Comment
        {
            get
            {
                return m_comment;
            }
            set
            {
                if (m_comment != value)
                {
                    m_comment = value;
                    OnPropertyChanged("Comment");
                }
            }
        }

        public string CaptureSubfolderPath
        {
            get { return mCaptureSubfolderPath; }
            set
            {
                if (mCaptureSubfolderPath != value)
                {
                    mCaptureSubfolderPath = value;
                    OnPropertyChanged("CaptureSubfolderPath");
                }
            }
        }

        public TriggerFileStatus TriggerFileStatus
        {
            get { return m_triggerFileStatus; }
            set
            {
                if (m_triggerFileStatus != value)
                {
                    m_triggerFileStatus = value;
                    OnPropertyChanged("TriggerFileStatus");
                }
            }
        }

        public DMSStatus DMSStatus
        {
            get
            {
                if (DMSData.LockData)
                    return DMSStatus.DMSResolved;
                
                return DMSStatus.NoDMSRequest;                
            }
        }

        public DatasetSource DatasetSource
        {
            get { return m_datasetSource; }
            set
            {
                if (m_datasetSource != value)
                {
                    m_datasetSource = value;
                    OnPropertyChanged("DatasetSource");
                }
            }
        }

        public string ExperimentName
        {
            get { return m_experimentName; }
            set
            {
                if (m_experimentName != value)
                {
                    m_experimentName = value;
                    OnPropertyChanged("ExperimentName");
                }
            }
        }

        public bool IsQC
        {
            get { return m_isQC; }
            set
            {
                if (m_isQC != value)
                {
                    m_isQC = value;
                    OnPropertyChanged("IsQC");
                }
            }
        }

        /// <summary>
        /// If there's another dataset with the same Request Name,
        /// but came from different source data, then someone should
        /// set this to True.
        /// </summary>
        public bool NotOnlyDatasource
        {
            get { return m_notOnlySource; }
            set
            {
                if (m_notOnlySource != value)
                {
                    m_notOnlySource = value;
                    OnPropertyChanged("NotOnlyDatasource");
                }
            }
        }

        public string Instrument
        {
            get { return m_instrument; }
            set
            {
                if (m_instrument != value)
                {
                    m_instrument = value;
                    OnPropertyChanged("Instrument");
                }
            }
        }

        public string Operator
        {
            get { return m_operator; }
            set
            {
                if (m_operator != value)
                {
                    m_operator = value;
                    OnPropertyChanged("Operator");
                }
            }
        }

        public string SeparationType
        {
            get { return m_separationType; }
            set
            {
                if (m_separationType != value)
                {
                    m_separationType = value;
                    OnPropertyChanged("SeparationType");
                }
            }
        }

        public string CartName
        {
            get { return m_cartName; }
            set
            {
                if (m_cartName != value)
                {
                    m_cartName = value;
                    OnPropertyChanged("CartName");
                }
            }
        }

        public DMSData DMSData
        {
            get { return m_dmsData; }
            set
            {
                if (m_dmsData != value)
                {
                    m_dmsData = value;
                    OnPropertyChanged("DMSData");
                    OnPropertyChanged("DMSStatus");                     
                }
            }
        }

        public DateTime DMSDataLastUpdate
        {
            get;
            set;
        }

        public string InterestRating
        {
            get { return m_interestRating; }
            set
            {
                if (m_interestRating != value)
                {
                    m_interestRating = value;
                    OnPropertyChanged("InterestRating");
                }
            }
        }

        /// <summary>
        /// Gets or sets the status of the trigger file.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public DatasetStatus DatasetStatus
        {
            get { return m_status; }
            set
            {
                if (m_status == value) return;
                m_status = value;

                OnPropertyChanged("DatasetStatus");
            }
        }

        public string TriggerCreationWarning
        {
            get { return m_TriggerCreationWarning; }
            set
            {
                if (String.CompareOrdinal(m_TriggerCreationWarning, value) == 0) return;
                m_TriggerCreationWarning = value;

                OnPropertyChanged("TriggerCreationWarning");
            }
        }

        #region File Properties
        public string FilePath
        {
            get { return m_filePath; }
            set
            {
                if (m_filePath != value)
                {
                    m_filePath = value;
                    OnPropertyChanged("FilePath");
                    OnPropertyChanged("Extension");

                    UpdateFileProperties();
                }
            }
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
            get { return m_runStart; }
            private set
            {
                if (m_runStart != value)
                {
                    m_runStart = value;
                    OnPropertyChanged("RunStart");
                }
            }
        }

        public DateTime RunFinish
        {
            get { return m_runFinish; }
            set
            {
                if (m_runFinish != value && m_runFinish < value)
                {
                    m_runFinish = value;
                    OnPropertyChanged("RunFinish");
                }
            }
        }

        public long FileSize
        {
            get { return m_fileSize; }
            private set
            {
                if (m_fileSize != value)
                {
                    m_fileSize = value;
                    OnPropertyChanged("FileSize");
                }
            }
        }
        
        /// <summary>
        /// True if the dataset is a single file, otherwise false
        /// </summary>
        public bool IsFile { get; set; }

        #endregion

        private long CalculateDirectorySize(string path)
        {
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                return info.Length;
            }
            if (!Directory.Exists(path))
            {
                return 0;
            }

            long sum = 0;
            foreach (var file in Directory.GetFiles(path))
            {
                sum += CalculateDirectorySize(file);
            }

            return sum;
        }

        /// <summary>
        /// This method reads a fresh FileInfo object on the FilePath, and
        /// copies the relevant parts into the Dataset's properties.
        /// </summary>
        /// <returns>True if the file or folder exists; otherwise false</returns>
        public bool UpdateFileProperties()
        {
            if (File.Exists(FilePath))
            {
                var info = new FileInfo(FilePath);

                FileSize = info.Length;
                RunStart = info.CreationTime;
                RunFinish = info.LastWriteTime;
                return true;
            }

            if (Directory.Exists(FilePath))
            {
                var info = new DirectoryInfo(FilePath);

                FileSize = CalculateDirectorySize(FilePath);
                RunStart = info.CreationTime;
                RunFinish = info.LastWriteTime;
                return true;
            }

            FileSize = 0;
            RunStart = DateTime.MinValue;
            RunFinish = DateTime.MinValue;
            return false;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        #region Members blindly brought over from classDataset

        #region Events
        /// <summary>
        /// Fired when the dataset has been written to.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public event EventHandler LastWriteChanged;

        /// <summary>
        /// Fired when the dataset was told to be ignored for creating a trigger file.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public event EventHandler IgnoreChanged;
        #endregion


        #region Attributes
        /// <summary>
        /// Last time that the file was written to.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        private DateTime m_lastWrite;

        /// <summary>
        /// Flag indicating whether a dataset should be ignored.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        private bool m_mboolShouldIgnore;
        #endregion


        #region Properties
        /// <summary>
        /// Gets or sets the flag if the dataset should be ignored.
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public bool ShouldIgnore
        {
            get
            {
                return m_mboolShouldIgnore;
            }
            set
            {
                if (m_mboolShouldIgnore == value) return;
                m_mboolShouldIgnore = value;
                if (IgnoreChanged != null)
                {
                    IgnoreChanged(this, null);
                }
            }
        }

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

        /// <summary>
        /// Gets or sets the time the file was last written
        /// </summary>
        /// <remarks>
        /// Pulled in to stop compile time errors.
        /// </remarks>
        public DateTime LastWrite
        {
            get
            {
                return m_lastWrite;
            }
            set
            {

                if (value.CompareTo(m_lastWrite) != 0)
                {
                    m_lastWrite = value;
                    if (LastWriteChanged != null)
                    {
                        LastWriteChanged(this, null);
                    }
                }
            }
        }

        ///// <summary>
        ///// Gets or sets the time until the trigger file should be made.
        ///// </summary>
        ///// <remarks>
        ///// Pulled in to stop compile time errors.
        ///// </remarks>
        //public TimeSpan Duration
        //{
        //    get;
        //    set;
        //}
        #endregion
        #endregion
    }
}
