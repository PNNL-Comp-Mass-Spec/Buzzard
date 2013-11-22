using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
		private string		m_pathName;
		private bool		m_itemFound;
		private DateTime	m_creationDate;
		private DateTime	m_lastModifiedDate;
		private long		m_sizeBytes;
		private bool		m_isFile;
		private int			m_fileCount;
		private int			m_folderCount;
		#endregion


		#region Initialization
		public FileFolderInfoViewer()
		{
			InitializeComponent();
			this.DataContext = this;
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
				ItemFound			= false;
				IsFile				= false;
				SizeBytes			= 0;
				CreationDate		= DateTime.MinValue;
				LastModifiedDate	= DateTime.MinValue;
				FileCount			= 0;
				FolderCount			= 0;

				return;
			}

			IsFile = File.Exists(PathName);

			if (IsFile)
			{
				ItemFound = true;
			}
			else
			{
				ItemFound = Directory.Exists(PathName);

				if (!ItemFound)
				{
					IsFile				= false;
					SizeBytes			= 0;
					CreationDate		= DateTime.MinValue;
					LastModifiedDate	= DateTime.MinValue;
					FileCount			= 0;
					FolderCount			= 0;

					return;
				}
			}

			if (IsFile)
			{
				FileInfo info = new FileInfo(PathName);

				CreationDate		= info.CreationTime;
				LastModifiedDate	= info.LastWriteTime;
				SizeBytes			= info.Length;
				FileCount			= 1;
				FolderCount			= 0;
			}
			else
			{
				DirectoryInfo info = new DirectoryInfo(PathName);
				
				CreationDate		= info.CreationTime;
				LastModifiedDate	= info.LastWriteTime;
				SizeBytes			= -1;

				int count;
				try
				{
					count = info.GetDirectories().Length;
				}
				catch
				{
					count = 0;
				}
				FolderCount = count;

				try
				{
					count = info.GetFiles().Length;
				}
				catch
				{
					count = 0;
				}
				FileCount = count;
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
