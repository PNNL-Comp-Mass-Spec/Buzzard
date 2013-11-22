using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using BuzzardWPF.Data;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;

using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;


namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for SearchConfigView.xaml
    /// </summary>
    public partial class SearchConfigView 
		: UserControl, INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Fired when a search is triggered to start.
		/// </summary>
		public event EventHandler<SearchEventArgs> SearchStart;
		#endregion


		#region Attibutes
        /// <summary>
        /// Configuration for searching for files.
        /// </summary>
        private SearchConfig m_config;

        System.Windows.Forms.FolderBrowserDialog m_dialog;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public SearchConfigView()
        {            
			InitializeComponent();
			this.DataContext = this;
			
			m_dialog = new System.Windows.Forms.FolderBrowserDialog();
			m_config = new SearchConfig();   
			
			// Combo box for the search types.
			ObservableCollection<SearchOption> options = new ObservableCollection<SearchOption>();
			options.Add(SearchOption.AllDirectories);
			options.Add(SearchOption.TopDirectoryOnly);
			
			// Establishing data contexts
			m_searchTypes.ItemsSource   = options;
			m_searchTypes.DataContext   = m_config;
			m_extension.DataContext     = m_config;
        }


		#region Properties
		public SearchConfig Config
        {
            get { return m_config; }
            set
            {
                if (m_config != value)
                {
                    m_config = value;
					OnPropertyChanged("Config");
                }
            }
        }

		public bool IncludedArchivedItems
		{
			get { return m_includedArchivedItems; }
			set
			{
				if (m_includedArchivedItems != value)
				{
					m_includedArchivedItems = value;
					OnPropertyChanged("IncludedArchivedItems");
				}

				DatasetManager.Manager.IncludedArchivedItems = value;
			}
		}
		private bool m_includedArchivedItems;
		#endregion


		#region Event Handlers
		/// <summary>
        /// Handles opening a Windows Explorer window for browsing the folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
			if (Config == null)
				return;

            string path = Config.DirectoryPath;
            if (Directory.Exists(path))
            {
                try
                {
                    Process.Start(path);
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(0, "Could not open an Explorer window to that path.", ex);
                }
            }
        }
        
		/// <summary>
        /// Handles fill down for the paths
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirectoryPath_Populating(object sender, PopulatingEventArgs e)
        {
			string text = m_directoryPath.Text;
			string dirname;
			
			try
			{
				dirname  = Path.GetDirectoryName(text);
			}
			catch
			{
				dirname = null;
			}

			try
			{
				if (string.IsNullOrWhiteSpace(dirname))
				{
					DriveInfo[] drives = DriveInfo.GetDrives();
					string[] driveNames = drives.Select(drive => { return drive.Name; }).ToArray();
					m_directoryPath.ItemsSource = driveNames;
				}
				else if (Directory.Exists(dirname))
				{
					string[] subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
					m_directoryPath.ItemsSource = subFolders;
				}
			}
			catch
			{
			}

			m_directoryPath.PopulateComplete();
        }
        
		/// <summary>
        /// Handles when the user wants to start searching.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_monitor_Click(object sender, RoutedEventArgs e)
        {
            if (SearchStart != null)
            {
                SearchStart(this, new SearchEventArgs(m_config));
            }
        }
        
		private void m_buttonBrowseForPath_Click(object sender, RoutedEventArgs e)
        {
			if (Config == null)
				return;

            if (Config.DirectoryPath != null)
            {
				if (Directory.Exists(Config.DirectoryPath))
                {
					m_dialog.SelectedPath = Config.DirectoryPath;
                }
            }
            System.Windows.Forms.DialogResult result        = m_dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Config.DirectoryPath = m_dialog.SelectedPath;
            }
        }
        #endregion


		#region Methods
		public void SaveSettings()
		{
			Settings.Default.Searcher_IncludedArchivedItems = this.IncludedArchivedItems;

            if (Config.StartDate.HasValue)            
                Settings.Default.SearchDateFrom =  Config.StartDate.Value;

            if (Config.EndDate.HasValue)           
                Settings.Default.SearchDateTo       = Config.EndDate.Value;
            Settings.Default.SearchExtension        = Config.FileExtension;
            Settings.Default.SearchPath             = Config.DirectoryPath;
            Settings.Default.SearchDirectoryOptions = Config.Option;
            Settings.Default.Save();
		}

		public void LoadSettings()
		{
			this.IncludedArchivedItems	            = Settings.Default.Searcher_IncludedArchivedItems;

            Config.StartDate = Settings.Default.SearchDateFrom; 
            Config.EndDate          = Settings.Default.SearchDateTo;
            Config.FileExtension    = Settings.Default.SearchExtension        ;
            Config.DirectoryPath    = Settings.Default.SearchPath             ;
            Config.Option           = Settings.Default.SearchDirectoryOptions ;
            
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}