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
        public const int DEFAULT_MINIMUM_FILE_SIZE_KB = 100;
        public const string DEFAULT_FILE_EXTENSION = ".raw";
        public const SearchOption DEFAULT_SEARCH_DEPTH = SearchOption.TopDirectoryOnly;
        public const bool DEFAULT_MATCH_FOLDERS = true;

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

        private bool useDateRange = false;
        private DateTime mStartDate;
        private DateTime mEndDate;

        // Do not save this option to the registry / settings; always keep it off when the program starts
        private bool mDisableBaseFolderValidation;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SearchConfig()
        {
            ResetToDefaults(true);
        }

        public bool SettingsChanged { get; set; }

        /// <summary>
        /// The share name that corresponds to Directory Path. Empty string when the share is the default specified in DMS.
        /// </summary>
        public string ShareName { get; set; } = string.Empty;

        /// <summary>
        /// Any components of the searched path that are not
        /// </summary>
        public string BaseCaptureSubdirectory { get; set; } = string.Empty;

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
            set
            {
                // Strip any invalid characters from the provided value
                var changed = false;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var value2 = string.Concat(value.Split(Path.GetInvalidFileNameChars()));
                    if (!value2.StartsWith("."))
                    {
                        value2 = "." + value2;
                    }

                    if (!value.Equals(value2))
                    {
                        changed = true;
                        value = value2;
                    }
                }

                if (!this.RaiseAndSetIfChangedMonitoredBool(ref mFileExtension, value) && changed)
                {
                    // if we cleaned the value, we need to report that the value changed to remove the invalid characters from the TextBox.
                    this.RaisePropertyChanged();
                }
            }
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
        /// If the search will use the date range to limit results. This is not stored to the saved settings.
        /// </summary>
        public bool UseDateRange
        {
            get => useDateRange;
            set => this.RaiseAndSetIfChanged(ref useDateRange, value);
        }

        /// <summary>
        /// The start of the search range. This is not stored to the saved settings.
        /// </summary>
        public DateTime StartDate
        {
            get => mStartDate;
            set => this.RaiseAndSetIfChanged(ref mStartDate, value);
        }

        /// <summary>
        /// The end of the search range. This is not stored to the saved settings.
        /// </summary>
        public DateTime EndDate
        {
            get => mEndDate;
            set => this.RaiseAndSetIfChanged(ref mEndDate, value);
        }

        /// <summary>
        /// Set to True to allow folders to be selected as Datasets
        /// </summary>
        public bool MatchFolders
        {
            get => mMatchFolders;
            set => this.RaiseAndSetIfChangedMonitored(ref mMatchFolders, value);
        }

        /// <summary>
        /// Gets or sets the minimum file size (in KB) for permitting trigger file creation
        /// Monitor: This is the minimum size for a dataset to be considered for trigger file creation
        /// </summary>
        public int MinimumSizeKB
        {
            get => mMinimumSizeKB;
            set => this.RaiseAndSetIfChangedMonitored(ref mMinimumSizeKB, value);
        }

        public void ResetDateRange()
        {
            StartDate = DateTime.Now.Date.AddYears(-3);
            EndDate = DateTime.Now.Date.AddDays(1).AddYears(1);
        }

        public void ResetToDefaults(bool resetDirectoryPath)
        {
            if (resetDirectoryPath)
            {
                DirectoryPath = @"c:\";
            }

            FileExtension = DEFAULT_FILE_EXTENSION;
            SearchDepth = DEFAULT_SEARCH_DEPTH;
            MatchFolders = DEFAULT_MATCH_FOLDERS;
            MinimumSizeKB = DEFAULT_MINIMUM_FILE_SIZE_KB;
            ResetDateRange();
        }

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.Search_MatchFolders = MatchFolders;

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
            FileExtension = Settings.Default.SearchExtension;
            DirectoryPath = Settings.Default.SearchPath;
            SearchDepth = Settings.Default.SearchDirectoryOptions;
            MinimumSizeKB = Settings.Default.SearchMinimumSizeKB;

            SettingsChanged = false;
        }
    }
}
