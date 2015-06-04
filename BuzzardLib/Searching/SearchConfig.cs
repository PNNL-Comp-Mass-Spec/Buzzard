using System;
using System.ComponentModel;
using System.IO;

namespace BuzzardLib.Searching
{
    /// <summary>
    /// Class that holds the information from the user interface about how to find data files.
    /// </summary>
    public class SearchConfig
		: INotifyPropertyChanged
    {
        #region "Constants"

        public const int DEFAULT_MINIMUM_FILE_SIZE_KB = 100;
        public const string DEFAULT_FILE_EXTENSION = ".raw";
        public const SearchOption DEFAULT_SEARCH_DEPTH = SearchOption.TopDirectoryOnly;
        public const bool DEFAULT_MATCH_FOLDERS = true;

        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
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

		#endregion

		/// <summary>
        /// Default constructor.
        /// </summary>
        public SearchConfig()
		{
		    ResetToDefaults(true);
		}

        #region "Properties"

        /// <summary>
        /// Gets or sets the path to search in.
        /// </summary>
        public string DirectoryPath
        {
            get { return mDirectoryPath; }
            set
            {
                if (mDirectoryPath != value)
                {
                    mDirectoryPath = value;
					OnPropertyChanged("DirectoryPath");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the file extension to look for.
        /// </summary>
        public string FileExtension
        {
            get { return mFileExtension; }
            set
            {
                if (mFileExtension != value)
                {
                    mFileExtension = value;
					OnPropertyChanged("FileExtension");
                }
            }
        }

        /// <summary>
        /// Gets or sets the folder name to filter on (partial match)
        /// </summary>
        public string FolderNameFilter
        {
            get { return mFolderNameFilter; }
            set
            {
                if (mFolderNameFilter != value)
                {
                    mFolderNameFilter = value;
                    OnPropertyChanged("FolderNameFilter");
                }
            }
        }

        /// <summary>
        /// Gets or sets the filename to filter on (partial match)
        /// </summary>
        public string FilenameFilter
        {
            get { return mFilenameFilter; }
            set
            {
                if (mFilenameFilter != value)
                {
                    mFilenameFilter = value;
                    OnPropertyChanged("FilenameFilter");
                }
            }
        }

        /// <summary>
        /// Gets or sets the way to search for files in a directory
        /// </summary>
        public SearchOption SearchDepth
        {
            get { return mSearchDepth; }
            set
            {
                if (mSearchDepth != value)
                {
                    mSearchDepth = value;
                    OnPropertyChanged("SearchDepth");
                }
            }
        }                
        
		/// <summary>
        /// Gets or sets the start of the search range
        /// </summary>
        public DateTime? StartDate
        {
			get { return mStartDate; }
			set
			{
				if (mStartDate != value)
				{
					mStartDate = value;
					OnPropertyChanged("StartDate");
				}
			}
        }
        
		/// <summary>
        /// Gets or sets the end of the search range
        /// </summary>
        public DateTime? EndDate
        {
			get { return mEndDate; }
			set
			{
				if (mEndDate != value)
				{
					mEndDate = value;
					OnPropertyChanged("EndDate");
				}
			}
        }
        
        public bool MatchFolders
        {
            get { return mMatchFolders; }
            set
            {
                if (mMatchFolders != value)
                {
                    mMatchFolders = value;
                    OnPropertyChanged("MatchFolders");
                }
            }
        }
        
        public int MinimumSizeKB
        {
            get { return mMinimumSizeKB; }
            set
            {
                if (mMinimumSizeKB != value)
                {
                    mMinimumSizeKB = value;
                    OnPropertyChanged("MinimumSizeKB");
                }
            }
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

        private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}
