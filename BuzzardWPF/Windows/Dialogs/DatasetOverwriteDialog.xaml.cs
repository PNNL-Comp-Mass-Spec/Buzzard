using System.ComponentModel;
using System.Windows;

namespace BuzzardWPF.Windows.Dialogs
{
	/// <summary>
	/// Interaction logic for DatasetOverwriteDialog.xaml
	/// </summary>
	public partial class DatasetOverwriteDialog 
		: Window, INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Attributes
		private string	m_fileToRenamePath;
		private string	m_fileInWayPath;
		private bool	m_doSameToOtherConflicts;
		private bool	m_skipRename;
		#endregion


		#region Initialize
		public DatasetOverwriteDialog()
		{
			InitializeComponent();
			DataContext = this;

			DoSameToOtherConflicts	= false;
			FileToRenamePath		= null;
			FileInWayPath			= null;
			SkipDatasetRename		= false;
		}
		#endregion


		#region Properties
		public string FileToRenamePath
		{
			get { return m_fileToRenamePath; }
			set
			{
				if (m_fileToRenamePath != value)
				{
					m_fileToRenamePath = value;
					OnPropertyChanged("FileToRenamePath");

					m_sourcePathDataViewer.PathName = value;
				}
			}
		}

		public string FileInWayPath
		{
			get { return m_fileInWayPath; }
			set
			{
				if (m_fileInWayPath != value)
				{
					m_fileInWayPath = value;
					OnPropertyChanged("FileInWayPath");

					m_destinationPathDataViewer.PathName = value;
				}
			}
		}

		public bool DoSameToOtherConflicts
		{
			get { return m_doSameToOtherConflicts; }
			set
			{
				if (m_doSameToOtherConflicts != value)
				{
					m_doSameToOtherConflicts = value;
					OnPropertyChanged("DoSameToOtherConflicts");
				}
			}
		}

		public bool SkipDatasetRename
		{
			get { return m_skipRename; }
			private set
			{
                if (m_skipRename != value)
				{
                    m_skipRename = value;
					OnPropertyChanged("SkipDatasetRename");
				}
			}
		}
		#endregion


		#region Event Handlers
        private void Replace_Click(object sender, RoutedEventArgs e)
		{
			SkipDatasetRename = false;
			DialogResult	= true;
		}

		private void SkipDataset_Click(object sender, RoutedEventArgs e)
		{
			SkipDatasetRename = true;
			DialogResult	= true;
		}
		#endregion


		#region Methods
		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
