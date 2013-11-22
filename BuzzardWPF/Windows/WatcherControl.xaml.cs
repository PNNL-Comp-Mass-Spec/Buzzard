using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using BuzzardWPF.Data;
using BuzzardWPF.Properties;

using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;

using Forms = System.Windows.Forms;


namespace BuzzardWPF.Windows
{
	/// <summary>
	/// Interaction logic for WatcherControl.xaml
	/// </summary>
	public partial class WatcherControl
        : UserControl, INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Attributes
		private string						m_directoryToWatch;
		private SearchOption				m_watchDepth;
		private bool						m_isWatching;
		private int							m_waitTime;
        private int                         m_minimumFileSize;
		private string						m_extension;
		private bool						m_createTriggerOnDMS_Fail;


		private Forms.FolderBrowserDialog	m_folderDialog;
		private FileSystemWatcher			m_fileSystemWatcher;
		#endregion


		#region Initialization
		public WatcherControl()
		{
			InitializeComponent();
			this.DataContext = this;
			//this.EMSL_DataSelector.BoundContainer = this;

			m_folderDialog = new Forms.FolderBrowserDialog();
			m_folderDialog.ShowNewFolderButton = true;

			m_fileSystemWatcher = new FileSystemWatcher();
			m_fileSystemWatcher.Created += new FileSystemEventHandler(SystemWatcher_FileCreated);
			m_fileSystemWatcher.Renamed += new RenamedEventHandler(SystemWatcher_FileRenamed);
			m_fileSystemWatcher.Deleted += new FileSystemEventHandler(SystemWatcher_FileDeleted);

            MinimumFileSize     = 100;
			IsWatching			= false;

		}
		#endregion


		#region Properties
		public bool CreateTriggerOnDMSFail
		{
			get { return m_createTriggerOnDMS_Fail; }
			set
			{
				if (m_createTriggerOnDMS_Fail != value)
				{
					m_createTriggerOnDMS_Fail = value;
					OnPropertyChanged("CreateTriggerOnDMSFail");
				}

				DatasetManager.Manager.CreateTriggerOnDMSFail = value;
			}
		}

		public string Extension
		{
			get { return m_extension; }
			set
			{
				if (m_extension != value)
				{
					m_extension = value;
					OnPropertyChanged("Extension");
				}
			}
		}

		public SearchOption WatchDepth
		{
			get { return m_watchDepth; }
			set
			{
				if (m_watchDepth != value)
				{
					m_watchDepth = value;
					OnPropertyChanged("WatchDepth");
				}
			}
		}

		public string DirectoryToWatch
		{
			get { return m_directoryToWatch; }
			set
			{
				if (m_directoryToWatch != value)
				{
					m_directoryToWatch = value;
					OnPropertyChanged("DirectoryToWatch");
				}

			}
		}

		public bool IsWatching
		{
			get { return m_isWatching; }
			private set
			{
				if (m_isWatching != value)
				{
					m_isWatching = value;
					OnPropertyChanged("IsWatching");
				}
			}
		}

		/// <summary>
		/// Gets or sets the amount of time to wait for
		/// trigger file creation on files that were
		/// found by the scanner.
		/// </summary>
		/// <remarks>
		/// This is measured in minutes.
		/// </remarks>
		public int WaitTime
		{
			get { return m_waitTime; }
			set
			{
				if (m_waitTime != value)
				{
					m_waitTime = value;
					OnPropertyChanged("WaitTime");
					OnPropertyChanged("WaitTime_MinComp");
					OnPropertyChanged("WaitTime_HrComp");
				}

				DatasetManager.Manager.TriggerFileCreationWaitTime = value;
			}
		}
        /// <summary>
        /// Gets or sets the minimum file size in Kb before starting a trigger creation
        /// </summary>
        public int MinimumFileSize
        {
            get { return m_minimumFileSize; }
            set
            {
                if (m_minimumFileSize != value)
                {
                    m_minimumFileSize = value;
                    OnPropertyChanged("MinimumFileSize");
                }
                DatasetManager.Manager.MinimumFileSize = value;
            }
        }


		


		#endregion


		#region Event Handlers
		void SystemWatcher_FileCreated(object sender, FileSystemEventArgs e)
		{
			string extension = Path.GetExtension(e.FullPath).ToLower();

			if (string.IsNullOrWhiteSpace(e.FullPath) || e.FullPath.Contains('$'))
				return;

			if (extension == Extension.ToLower())
			{
				DatasetManager.Manager.CreatePendingDataset(e.FullPath, DatasetSource.Watcher);
			}
		}

		void SystemWatcher_FileRenamed(object sender, RenamedEventArgs e)
		{
			string extension = Path.GetExtension(e.FullPath).ToLower();

			if (string.IsNullOrWhiteSpace(e.FullPath) || e.FullPath.Contains('$'))
				return;

			if (extension == Extension.ToLower())
			{
				// Todo: Check to see if this really is the correct behavior.
				DatasetManager.Manager.CreatePendingDataset(e.FullPath, DatasetSource.Watcher);
			}
		}

		void SystemWatcher_FileDeleted(object sender, FileSystemEventArgs e)
		{
			// Todo: Handle these
		}

		private void AutoFillDirectorySelector_Populating(object sender, PopulatingEventArgs e)
		{
			string text = DirectoryToWatch;
			string dirname;

			try
			{
				dirname = System.IO.Path.GetDirectoryName(text);
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
					m_autoFillDirectorySelector.ItemsSource = driveNames;
				}
				else if (Directory.Exists(dirname))
				{
					string[] subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
					m_autoFillDirectorySelector.ItemsSource = subFolders;
				}
			}
			catch
			{
			}

			m_autoFillDirectorySelector.PopulateComplete();
		}

		private void SelectDirectory_Click(object sender, RoutedEventArgs e)
		{
			m_folderDialog.SelectedPath = DirectoryToWatch;

			Forms.DialogResult result = m_folderDialog.ShowDialog();

            if (result == Forms.DialogResult.OK)
            {
                DirectoryToWatch = m_folderDialog.SelectedPath;
            }
		}

		private void OpenExplorerWindow_Click(object sender, RoutedEventArgs e)
		{
			if (Directory.Exists(DirectoryToWatch))
			{
				try
				{
					Process.Start(DirectoryToWatch);
				}
				catch (Exception ex)
				{
					classApplicationLogger.LogError(
						0,
						"Could not open an Explorer window to that path.",
						ex);
				}
			}
		}

		private void MonitorStartStop_Click(object sender, RoutedEventArgs e)
		{
			if (IsWatching)
				StopWatching();
			else
				StartWatching();
		}
		#endregion


		#region Methods
		public void SaveSettings()
		{
			Settings.Default.Watcher_FilePattern		= this.Extension;
			Settings.Default.Watcher_SearchType			= this.WatchDepth;
			Settings.Default.Watcher_WaitTime			= this.WaitTime;
			Settings.Default.Watcher_WatchDir			= this.DirectoryToWatch;
            Settings.Default.Watcher_FileSize           = this.MinimumFileSize;
			Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail = this.CreateTriggerOnDMSFail;

			
		}

		public void LoadSettings()
		{
			this.Extension				= Settings.Default.Watcher_FilePattern;
			this.WatchDepth				= Settings.Default.Watcher_SearchType;
			this.WaitTime				= Settings.Default.Watcher_WaitTime;
            this.DirectoryToWatch = Settings.Default.Watcher_WatchDir;
            this.MinimumFileSize = Settings.Default.Watcher_FileSize;

			this.CreateTriggerOnDMSFail = Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail;

		}

		private void StopWatching()
		{
			m_fileSystemWatcher.EnableRaisingEvents = false;
			IsWatching = false;

			// This may seem a bit strange since I used bindings to 
			// enable and disable the other controls, but for some 
			// reason bindings didn't work for doing that to these 
			// two controls.
			m_dialogButton.IsEnabled = true;
			m_dropDown.IsEnabled = true;

			classApplicationLogger.LogMessage(0, "Watcher stopped.");
			classApplicationLogger.LogMessage(0, "Ready.");
		}

		private void StartWatching()
		{
            
			if (Directory.Exists(DirectoryToWatch))
			{
                DatasetManager.Manager.FileWatchRoot = DirectoryToWatch;

				m_fileSystemWatcher.Path = DirectoryToWatch;
				m_fileSystemWatcher.IncludeSubdirectories = WatchDepth == SearchOption.AllDirectories;
				m_fileSystemWatcher.Filter = "*.*";
				m_fileSystemWatcher.EnableRaisingEvents = true;
				IsWatching = true;

				// This may seem a bit strange since I used bindings to 
				// enable and disable the other controls, but for some 
				// reason bindings didn't work for doing that to these 
				// two controls.
				m_dialogButton.IsEnabled	= false;
				m_dropDown.IsEnabled		= false;

				classApplicationLogger.LogMessage(0, "Watcher is monitoring.");
			}
			else
			{
				classApplicationLogger.LogError(
					0,
					"Could not start the monitor. The supplied path does not exits.");
			}
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
