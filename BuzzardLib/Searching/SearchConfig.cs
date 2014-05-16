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
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Attributes
		/// <summary>
        /// Path to the directory where the files are to be searched.
        /// </summary>
        private string m_directoryPath;

		private DateTime? m_startDate;
		private DateTime? m_endDate;
		#endregion

		/// <summary>
        /// Default constructor.
        /// </summary>
        public SearchConfig()
        {
            DirectoryPath   = @"c:\";
            FileExtension   = ".raw";
            Option          = SearchOption.TopDirectoryOnly;
			StartDate		= null;
            EndDate         = null;
        }

        /// <summary>
        /// Gets or sets the path to search in.
        /// </summary>
        public string DirectoryPath
        {
            get { return m_directoryPath; }
            set
            {
                if (m_directoryPath != value)
                {
                    m_directoryPath = value;
					OnPropertyChanged("DirectoryPath");
                }
            }
        }
        private string m_fileExtension = "";
        /// <summary>
        /// Gets or sets the file extension to look for.
        /// </summary>
        public string FileExtension
        {
            get { return m_fileExtension; }
            set
            {
                if (m_fileExtension != value)
                {
                    m_fileExtension = value;
					OnPropertyChanged("FileExtension");
                }
            }
        }

        private SearchOption m_option;
        /// <summary>
        /// Gets or sets the way to search for files in a directory
        /// </summary>
        public SearchOption Option
        {
            get { return m_option; }
            set
            {
                if (m_option != value)
                {
                    m_option = value;
                    OnPropertyChanged("Option");
                }
            }
        }                
        
		/// <summary>
        /// Gets or sets the start of the search range
        /// </summary>
        public DateTime? StartDate
        {
			get { return m_startDate; }
			set
			{
				if (m_startDate != value)
				{
					m_startDate = value;
					OnPropertyChanged("StartDate");
				}
			}
        }
        
		/// <summary>
        /// Gets or sets the end of the search range
        /// </summary>
        public DateTime? EndDate
        {
			get { return m_endDate; }
			set
			{
				if (m_endDate != value)
				{
					m_endDate = value;
					OnPropertyChanged("EndDate");
				}
			}
        }

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
    }
}
