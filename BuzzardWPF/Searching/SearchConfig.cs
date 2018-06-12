using System;
using System.IO;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using ReactiveUI;

namespace BuzzardWPF.Searching
{
    /// <summary>
    /// Class that holds the information from the user interface about how to find data files.
    /// </summary>
    public class SearchConfig : ReactiveObject, IStoredSettingsMonitor
    {
        #region "Constants"

        public const int DEFAULT_MINIMUM_FILE_SIZE_KB = 100;
        public const string DEFAULT_FILE_EXTENSION = ".raw";
        public const SearchOption DEFAULT_SEARCH_DEPTH = SearchOption.TopDirectoryOnly;
        public const bool DEFAULT_MATCH_FOLDERS = true;

        #endregion

        #region Attributes

        /// <summary>
        /// Path to the directory where the files are to be searched.
        /// </summary>
        private string mDirectoryPath;
        private string mFileExtension;
        private string mFolderNameFilter;
        private string mFilenameFilter;
        private SearchOption mSearchDepth;
        private bool mMatchFolders;
        private int mMinimumSizeKB;

        private DateTime? mStartDate;
        private DateTime? mEndDate;

        // Do not save this option to the registry / settings; always keep it off when the program starts
        private bool mDisableBaseFolderValidation;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SearchConfig()
        {
            ResetToDefaults(true);
        }

        #region "Properties"

        public bool SettingsChanged { get; set; }

        /// <summary>
        /// Gets or sets the path to search in.
        /// </summary>
        public string DirectoryPath
        {
            get => mDirectoryPath;
            set => this.RaiseAndSetIfChangedMonitored(ref mDirectoryPath, value);
        }

        public bool DisableBaseFolderValidation
        {
            get => mDisableBaseFolderValidation;
            set => this.RaiseAndSetIfChanged(ref mDisableBaseFolderValidation, value);
        }

        /// <summary>
        /// Gets or sets the file extension to look for.
        /// </summary>
        public string FileExtension
        {
            get => mFileExtension;
            set => this.RaiseAndSetIfChangedMonitored(ref mFileExtension, value);
        }

        /// <summary>
        /// Gets or sets the folder name to filter on (partial match)
        /// </summary>
        public string FolderNameFilter
        {
            get => mFolderNameFilter;
            set => this.RaiseAndSetIfChanged(ref mFolderNameFilter, value);
        }

        /// <summary>
        /// Gets or sets the filename to filter on (partial match)
        /// </summary>
        public string FilenameFilter
        {
            get => mFilenameFilter;
            set => this.RaiseAndSetIfChanged(ref mFilenameFilter, value);
        }

        /// <summary>
        /// Gets or sets the way to search for files in a directory
        /// </summary>
        public SearchOption SearchDepth
        {
            get => mSearchDepth;
            set => this.RaiseAndSetIfChangedMonitored(ref mSearchDepth, value);
        }

        /// <summary>
        /// Gets or sets the start of the search range
        /// </summary>
        public DateTime? StartDate
        {
            get => mStartDate;
            set => this.RaiseAndSetIfChangedMonitored(ref mStartDate, value);
        }

        /// <summary>
        /// Gets or sets the end of the search range
        /// </summary>
        public DateTime? EndDate
        {
            get => mEndDate;
            set => this.RaiseAndSetIfChangedMonitored(ref mEndDate, value);
        }

        public bool MatchFolders
        {
            get => mMatchFolders;
            set => this.RaiseAndSetIfChangedMonitored(ref mMatchFolders, value);
        }

        public int MinimumSizeKB
        {
            get => mMinimumSizeKB;
            set => this.RaiseAndSetIfChangedMonitored(ref mMinimumSizeKB, value);
        }
        #endregion

        #region "Public Methods"

        public void ResetDateRange()
        {
            StartDate = DateTime.Now.Date.AddYears(-3);
            EndDate = DateTime.Now.Date.AddDays(1).AddYears(1);
        }

        public void ResetToDefaults(bool resetDirectoryPath)
        {
            if (resetDirectoryPath)
                DirectoryPath = @"c:\";

            FileExtension = DEFAULT_FILE_EXTENSION;
            SearchDepth = DEFAULT_SEARCH_DEPTH;
            MatchFolders = DEFAULT_MATCH_FOLDERS;
            MinimumSizeKB = DEFAULT_MINIMUM_FILE_SIZE_KB;
            ResetDateRange();
        }

        #endregion

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.Search_MatchFolders = MatchFolders;

            if (StartDate.HasValue)
                Settings.Default.SearchDateFrom = StartDate.Value;

            if (EndDate.HasValue)
                Settings.Default.SearchDateTo = EndDate.Value;

            Settings.Default.SearchExtension = FileExtension;
            Settings.Default.SearchPath = DirectoryPath;
            Settings.Default.SearchDirectoryOptions = SearchDepth;
            Settings.Default.SearchMinimumSizeKB = MinimumSizeKB;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            MatchFolders = Settings.Default.Search_MatchFolders;
            StartDate = Settings.Default.SearchDateFrom;
            EndDate = Settings.Default.SearchDateTo;
            FileExtension = Settings.Default.SearchExtension;
            DirectoryPath = Settings.Default.SearchPath;
            SearchDepth = Settings.Default.SearchDirectoryOptions;
            MinimumSizeKB = Settings.Default.SearchMinimumSizeKB;

            SettingsChanged = false;
        }
    }
}
