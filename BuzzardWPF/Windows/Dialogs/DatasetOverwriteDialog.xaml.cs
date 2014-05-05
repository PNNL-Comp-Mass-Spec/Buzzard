using System.ComponentModel;
using System.Windows;

namespace BuzzardWPF.Windows
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
		private string	m_fileToMovePath;
		private string	m_fileInWayPath;
		private bool	m_doSameToOtherConflicts;
		private bool	m_skipMove;
		#endregion


		#region Initialize
		public DatasetOverwriteDialog()
		{
			InitializeComponent();
			DataContext = this;

			DoSameToOtherConflicts	= false;
			FileToMovePath			= null;
			FileInWayPath			= null;
			SkipDatasetMove			= false;
		}
		#endregion


		#region Properties
		public string FileToMovePath
		{
			get { return m_fileToMovePath; }
			set
			{
				if (m_fileToMovePath != value)
				{
					m_fileToMovePath = value;
					OnPropertyChanged("FileToMovePath");

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

		public bool SkipDatasetMove
		{
			get { return m_skipMove; }
			private set
			{
				if (m_skipMove != value)
				{
					m_skipMove = value;
					OnPropertyChanged("SkipDatasetMove");
				}
			}
		}
		#endregion


		#region Event Handlers
		private void CopyAndReplace_Click(object sender, RoutedEventArgs e)
		{
			SkipDatasetMove = false;
			DialogResult	= true;
		}

		private void SkipDataset_Click(object sender, RoutedEventArgs e)
		{
			SkipDatasetMove = true;
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
