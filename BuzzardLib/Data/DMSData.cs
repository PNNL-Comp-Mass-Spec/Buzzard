using System.ComponentModel;
using System.IO;
using LcmsNetDataClasses;

namespace BuzzardLib.Data
{
	/// <summary>
	/// 
	/// </summary>
	public class DMSData
		: INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Attributes
		private bool	m_selectedToRun;
		private string	m_requestName;
		private string	m_datasetName;
		private int		m_requestID;

		private string	m_experiment;
		private string	m_datasetType;
		private string	m_usageType;
		private string	m_proposalID;

		private string	m_userList;
		private string	m_cartName;
		private string	m_comment;
		private int		m_mrmFileID;

		private int		m_block;
		private int		m_runOrder;
		private int		m_batch;

		private bool	m_lockData;
		#endregion


		#region Initialization
		public DMSData()
		{
			LockData		= false;

			Batch			= -1;
			Block			= -1;
			CartName		= null;
			Comment			= null;

			DatasetName		= null;
			DatasetType		= null;
			Experiment		= null;
			MRMFileID		= -1;

			EMSLProposalID	= null;
			RequestID		= 0;
			RequestName		= null;
			RunOrder		= -1;

			SelectedToRun	= false;
			EMSLUsageType	= null;
			UserList		= null;
		}

		public DMSData(classDMSData other)
			: this()
		{
			Batch			= other.Batch;
			Block			= other.Block;
			CartName		= other.CartName;
			Comment		= other.Comment;

			DatasetName	= other.DatasetName;
			DatasetType	= other.DatasetType;
			Experiment		= other.Experiment;
			MRMFileID		= other.MRMFileID;

			EMSLProposalID = other.ProposalID;
			RequestID		= other.RequestID;
			RequestName	= other.RequestName;
			RunOrder		= other.RunOrder;

			SelectedToRun	= other.SelectedToRun;
			EMSLUsageType	= other.UsageType;
			UserList		= other.UserList;

			LockData		= true;
		}

		public DMSData(classDMSData other, string filePath)
			: this(other)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return;

			var fileName = Path.GetFileNameWithoutExtension(filePath);
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				LockData	= false;
				DatasetName		= fileName;
				LockData	= true;
			}
		}
		#endregion


		#region Properties
		/// <summary>
		/// When the data comes from DMS, it will be
		/// locked. This is meant to stop the user
		/// from alterning it.
		/// </summary>
		public bool LockData
		{
			get { return m_lockData; }
			private set
			{
				if (m_lockData != value)
				{
					m_lockData = value;
					OnPropertyChanged("LockData");
				}
			}
		}

		/// <summary>
		/// Flag for determining if request from DMS has been selected for running
		/// </summary>
		public bool SelectedToRun
		{
			get { return m_selectedToRun; }
			set
			{
				if (m_selectedToRun != value)
				{
					if (!LockData)
						m_selectedToRun = value;
					OnPropertyChanged("SelectedToRun");
				}
			}
		}

		/// <summary>
		/// Name of request in DMS. Becomes sample name in LCMS and forms part
		/// of dataset name sample after run
		/// </summary>
		public string RequestName
		{
			get { return m_requestName; }
			set
			{
				if (m_requestName != value)
				{
					if (!LockData)
						m_requestName = value;
					OnPropertyChanged("RequestName");

					//if (string.IsNullOrWhiteSpace(DatasetName))
					//    DatasetName = RequestName;
				}
			}
		}

		/// <summary>
		/// Gets or sets the name of the sample after editing the request name.
		/// </summary>
		public string DatasetName
		{
			get { return m_datasetName; }
			set
			{
				if (m_datasetName != value)
				{
					if (!LockData)
						m_datasetName = value;
					OnPropertyChanged("DatasetName");
				}
			}
		}
		
		/// <summary>
		/// Numeric ID of request in DMS
		/// </summary>
		public int RequestID
		{
			get { return m_requestID; }
			set
			{
				if (m_requestID != value)
				{
					if (!LockData)
						m_requestID = value;
					OnPropertyChanged("RequestID");
				}
			}
		}

		/// <summary>
		/// Experiment name
		/// </summary>
		public string Experiment
		{
			get { return m_experiment; }
			set
			{
				if (m_experiment != value)
				{
					if (!LockData)
						m_experiment = value;
					OnPropertyChanged("Experiment");
				}
			}
		}

		/// <summary>
		/// Dataset type (ie, HMS-MSn, HMS, etc)
		/// </summary>
		public string DatasetType
		{
			get { return m_datasetType; }
			set
			{
				if (m_datasetType != value)
				{
					if (!LockData)
						m_datasetType = value;
					OnPropertyChanged("DatasetType");
				}
			}
		}

		/// <summary>
		/// EMSL usage type
		/// </summary>
		public string EMSLUsageType
		{
			get { return m_usageType; }
			set
			{
				if (m_usageType != value)
				{
					if (!LockData)
						m_usageType = value;
					OnPropertyChanged("EMSLUsageType");
				}
			}
		}

		/// <summary>
		/// EUS sser proposal ID
		/// </summary>
		public string EMSLProposalID
		{
			get { return m_proposalID; }
			set
			{
				if (m_proposalID != value)
				{
					if (!LockData)
						m_proposalID = value;
					OnPropertyChanged("EMSLProposalID");
				}
			}
		}

		/// <summary>
		/// EUS user list
		/// </summary>
		public string UserList
		{
			get { return m_userList; }
			set
			{
				if (m_userList != value)
				{
					if (!LockData)
						m_userList = value;
					OnPropertyChanged("UserList");
				}
			}
		}

		/// <summary>
		/// Name of cart used for sample run
		/// </summary>
		public string CartName
		{
			get { return m_cartName; }
			set
			{
				if (m_cartName != value)
				{
					// This is an editable field even if the DMS Request has been resolved.
					m_cartName = value;
					OnPropertyChanged("CartName");
				}
			}
		}

		/// <summary>
		/// Comment field
		/// </summary>
		public string Comment
		{
			get { return m_comment; }
			set
			{
				if (m_comment != value)
				{
					if (!LockData)
						m_comment = value;
					OnPropertyChanged("Comment");
				}
			}
		}

		/// <summary>
		/// File ID for locating MRM file to download
		/// </summary>
		public int MRMFileID
		{
			get { return m_mrmFileID; }
			set
			{
				if (m_mrmFileID != value)
				{
					if (!LockData)
						m_mrmFileID = value;
					OnPropertyChanged("MRMFileID");
				}
			}
		}

		/// <summary>
		/// Block ID for blocking/randomizing
		/// </summary>
		public int Block
		{
			get { return m_block; }
			set
			{
				if (m_block != value)
				{
					if (!LockData)
						m_block = value;
					OnPropertyChanged("Block");
				}
			}
		}

		/// <summary>
		/// Run order for blocking/randomizing
		/// </summary>
		public int RunOrder
		{
			get { return m_runOrder; }
			set
			{
				if (m_runOrder != value)
				{
					if (!LockData)
						m_runOrder = value;
					OnPropertyChanged("RunOrder");
				}
			}
		}

		/// <summary>
		/// Batch number for blocking/randomizing
		/// </summary>
		public int Batch
		{
			get { return m_batch; }
			set
			{
				if (m_batch != value)
				{
					if (!LockData)
						m_batch = value;
					OnPropertyChanged("Batch");
				}
			}
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
