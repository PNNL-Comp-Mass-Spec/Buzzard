using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for FileFolderInfoViewer.xaml
    /// </summary>
    public partial class FileFolderInfoViewer
        : UserControl, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        #region Attributes
        private string m_pathName;
        private bool m_itemFound;
        private DateTime m_creationDate;
        private DateTime m_lastModifiedDate;
        private long m_sizeBytes;
        private bool m_isFile;
        private int m_fileCount;
        private int m_folderCount;
        #endregion


        #region Initialization
        public FileFolderInfoViewer()
        {
            InitializeComponent();
            DataContext = this;
        }
        #endregion


        #region Properties
        public int FileCount
        {
            get { return m_fileCount; }
            set
            {
                if (m_fileCount != value)
                {
                    m_fileCount = value;
                    OnPropertyChanged("FileCount");
                }
            }
        }

        public int FolderCount
        {
            get { return m_folderCount; }
            set
            {
                if (m_folderCount != value)
                {
                    m_folderCount = value;
                    OnPropertyChanged("FolderCount");
                }
            }
        }

        public bool IsFile
        {
            get { return m_isFile; }
            private set
            {
                if (m_isFile != value)
                {
                    m_isFile = value;
                    OnPropertyChanged("IsFile");

                    UpdateViewsPage();
                }
            }
        }

        public long SizeBytes
        {
            get { return m_sizeBytes; }
            private set
            {
                if (m_sizeBytes != value)
                {
                    m_sizeBytes = value;
                    OnPropertyChanged("SizeBytes");
                }
            }
        }

        public DateTime CreationDate
        {
            get { return m_creationDate; }
            private set
            {
                if (m_creationDate != value)
                {
                    m_creationDate = value;
                    OnPropertyChanged("CreationDate");
                }
            }
        }

        public DateTime LastModifiedDate
        {
            get { return m_lastModifiedDate; }
            private set
            {
                if (m_lastModifiedDate != value)
                {
                    m_lastModifiedDate = value;
                    OnPropertyChanged("LastModifiedDate");
                }
            }
        }

        public bool ItemFound
        {
            get { return m_itemFound; }
            private set
            {
                if (m_itemFound != value)
                {
                    m_itemFound = value;
                    OnPropertyChanged("ItemFound");

                    UpdateViewsPage();
                }
            }
        }

        public string PathName
        {
            get { return m_pathName; }
            set
            {
                if (m_pathName != value)
                {
                    m_pathName = value;
                    OnPropertyChanged("PathName");

                    GetPathInfo();
                }
            }
        }
        #endregion


        #region Methods
        private void UpdateViewsPage()
        {
            if (!ItemFound)
                m_tabControl.SelectedIndex = 0;
            else if (IsFile)
                m_tabControl.SelectedIndex = 1;
            else
                m_tabControl.SelectedIndex = 2;
        }

        private void GetPathInfo()
        {
            if (string.IsNullOrWhiteSpace(PathName))
            {
                ItemFound = false;
                IsFile = false;
                SizeBytes = 0;
                CreationDate = DateTime.MinValue;
                LastModifiedDate = DateTime.MinValue;
                FileCount = 0;
                FolderCount = 0;

                return;
            }

            var fiFile = new FileInfo(PathName);
            ItemFound = fiFile.Exists;

            if (ItemFound)
            {
                IsFile = true;
                GetPathInfoForFile(fiFile);
            }
            else
            {
                var diFolder = new DirectoryInfo(PathName);
                ItemFound = diFolder.Exists;

                if (ItemFound)
                {
                    IsFile = false;
                    GetPathInfoForFolder(diFolder);
                }
                else
                {
                    IsFile = false;
                    SizeBytes = 0;
                    CreationDate = DateTime.MinValue;
                    LastModifiedDate = DateTime.MinValue;
                    FileCount = 0;
                    FolderCount = 0;
                }
            }

        }

        private void GetPathInfoForFile(FileInfo fiFile)
        {
            CreationDate = fiFile.CreationTime;
            LastModifiedDate = fiFile.LastWriteTime;
            SizeBytes = fiFile.Length;
            FileCount = 1;
            FolderCount = 0;
        }

        private void GetPathInfoForFolder(DirectoryInfo diFolder)
        {
            CreationDate = diFolder.CreationTime;
            LastModifiedDate = diFolder.LastWriteTime;
            SizeBytes = 0;
            FileCount = 0;
            FolderCount = 0;

            try
            {
                foreach (var file in diFolder.GetFiles("*", SearchOption.AllDirectories))
                {
                    FileCount++;
                    SizeBytes += file.Length;
                }
            }
            catch
            {
                // Ignore errors here
            }

            try
            {
                FolderCount = diFolder.GetDirectories("*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                // Ignore errors here
            }

        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
