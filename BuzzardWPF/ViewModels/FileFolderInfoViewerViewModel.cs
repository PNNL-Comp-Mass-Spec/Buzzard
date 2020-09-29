using System;
using System.IO;
using BuzzardWPF.Management;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class FileFolderInfoViewerViewModel : ReactiveObject
    {
        #region Attributes

        private string m_pathName;
        private bool m_itemFound;
        private DateTime m_creationDate;
        private DateTime m_lastModifiedDate;
        private long m_sizeBytes;
        private bool m_isFile;
        private int m_fileCount;
        private int m_folderCount;
        private int selectedTabIndex;

        #endregion

        #region Properties
        public int FileCount
        {
            get => m_fileCount;
            set => this.RaiseAndSetIfChanged(ref m_fileCount, value);
        }

        public int FolderCount
        {
            get => m_folderCount;
            set => this.RaiseAndSetIfChanged(ref m_folderCount, value);
        }

        public bool IsFile
        {
            get => m_isFile;
            private set => this.RaiseAndSetIfChanged(ref m_isFile, value, x => UpdateViewsPage());
        }

        public long SizeBytes
        {
            get => m_sizeBytes;
            private set => this.RaiseAndSetIfChanged(ref m_sizeBytes, value);
        }

        public DateTime CreationDate
        {
            get => m_creationDate;
            private set => this.RaiseAndSetIfChanged(ref m_creationDate, value);
        }

        public DateTime LastModifiedDate
        {
            get => m_lastModifiedDate;
            private set => this.RaiseAndSetIfChanged(ref m_lastModifiedDate, value);
        }

        public bool ItemFound
        {
            get => m_itemFound;
            private set => this.RaiseAndSetIfChanged(ref m_itemFound, value, x => UpdateViewsPage());
        }

        public string PathName
        {
            get => m_pathName;
            set => this.RaiseAndSetIfChanged(ref m_pathName, value, x => GetPathInfo());
        }

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
        }

        #endregion

        #region Methods
        private void UpdateViewsPage()
        {
            if (!ItemFound)
                SelectedTabIndex = 0;
            else if (IsFile)
                SelectedTabIndex = 1;
            else
                SelectedTabIndex = 2;
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
        #endregion
    }
}
