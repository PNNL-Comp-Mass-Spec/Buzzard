using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;
using LcmsNetSQLiteTools;

namespace BuzzardWPF.Management
{
	public class DMS_DataAccessor
		: INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Initialize
		private DMS_DataAccessor()
		{
			m_proposalUserCollections = new Dictionary<string, ObservableCollection<classProposalUser>>();
			LoadProposalUsers();


			InstrumentData		= new ObservableCollection<string>();
			OperatorData		= new ObservableCollection<string>();
			DatasetTypes		= new ObservableCollection<string>();
			SeparationTypes	= new ObservableCollection<string>();

			CartNames			= new ObservableCollection<string>();
			ColumnData			= new ObservableCollection<string>();
			Experiments		= new List<classExperimentData>();
		}

		static DMS_DataAccessor()
		{
			Instance = new DMS_DataAccessor();
		}

		public void Initialize()
		{
			//
			// Load Instrument Data
			//
			var tempInstrumentData = classSQLiteTools.GetInstrumentList(false);
		    if (tempInstrumentData == null)
		    {
		        classApplicationLogger.LogError(0, "Instrument list retrieval returned null.");
		        InstrumentData = new ObservableCollection<string>();
		    }
		    else
		    {
		        if (tempInstrumentData.Count == 0)
		            classApplicationLogger.LogError(0, "No instruments found.");
		    }

		    if (tempInstrumentData != null && tempInstrumentData.Count != 0)
				InstrumentData = new ObservableCollection<string>(tempInstrumentData.Select(instDatum => instDatum.DMSName));

			//
			// Load Operator Data
			//
			var tempUserList = classSQLiteTools.GetUserList(false);
			if (tempUserList == null)
				classApplicationLogger.LogError(0, "User retrieval returned null.");
			else
				OperatorData = new ObservableCollection<string>(tempUserList.Select(userDatum => userDatum.UserName));


			//
			// Load Dataset Types
			//
			var tempDatasetTypesList = classSQLiteTools.GetDatasetTypeList(false);
			if (tempDatasetTypesList == null)
				classApplicationLogger.LogError(0, "Dataset Types retrieval returned null.");
			else
				DatasetTypes = new ObservableCollection<string>(tempDatasetTypesList);


			//
			// Load Separation Types
			//
			var tempSeparationTypesList = classSQLiteTools.GetSepTypeList(false);
			if (tempSeparationTypesList == null)
				classApplicationLogger.LogError(0, "Separation types retrieval returned null.");
			else
				SeparationTypes = new ObservableCollection<string>(tempSeparationTypesList);


			//
			// Load Cart Names
			//
			var tempCartsList = classSQLiteTools.GetCartNameList(false);
			if (tempCartsList == null)
				classApplicationLogger.LogError(0, "Cart names list retrieval returned null.");
			else
				CartNames = new ObservableCollection<string>(tempCartsList);


			//
			// Load column data
			//
			var tempColumnData = classSQLiteTools.GetColumnList(false);
			if (tempColumnData == null)
				classApplicationLogger.LogError(0, "Column data list retrieval returned null.");
			else
				ColumnData = new ObservableCollection<string>(tempColumnData);

			//
			// Load Experiments
			//
			var tempExperimentsList = classSQLiteTools.GetExperimentList();
			if (tempExperimentsList == null)
				classApplicationLogger.LogError(0, "Experiment list retrieval returned null.");
			else
				Experiments = new List<classExperimentData>(tempExperimentsList);
		}
		#endregion


		public static DMS_DataAccessor Instance
		{
			get;
			private set;
		}


		#region EMSL Proposal User Items
		/// <summary>
		/// This method load Proposal User data from a SQLite cache of DMS data. The data includes
		/// a list of the Proposal Users and a dictionary of UserIDs to ProposalID cross references.
		/// The dictionary is indexed by ProposalID.
		/// </summary>
		private void LoadProposalUsers()
		{
			m_pidIndexedCrossReferenceList =
				new Dictionary<string, List<classUserIDPIDCrossReferenceEntry>>();

			List<classProposalUser> temp0;
			Dictionary<string, List<classUserIDPIDCrossReferenceEntry>> temp1;

			classSQLiteTools.GetProposalUsers(out temp0, out temp1);

			if (temp0.Count == 0)
				classApplicationLogger.LogError(0, "No Proposal Users found");

			foreach (var items in temp1)
			{
				if (items.Value.Count == 0)
					classApplicationLogger.LogError(0, string.Format("PID {0} has no users.", items.Key));


				m_pidIndexedCrossReferenceList.Add(
					items.Key,
					new List<classUserIDPIDCrossReferenceEntry>(items.Value));
			}

			m_proposalUsers = new List<classProposalUser>(temp0);

			ProposalIDs = new ObservableCollection<string>(m_pidIndexedCrossReferenceList.Keys);
		}
		private List<classProposalUser> m_proposalUsers;
		private Dictionary<string, List<classUserIDPIDCrossReferenceEntry>> m_pidIndexedCrossReferenceList;

		/// <summary>
		/// Gets an ObservableCollection of ProposalUsers that are involved with the given PID.
		/// </summary>
		public ObservableCollection<classProposalUser> GetProposalUsers(string proposalID, bool returnAllWhenEmpty = false)
		{
			if (string.IsNullOrWhiteSpace(proposalID))
				proposalID = string.Empty;

			// We haven't built a quick reference collection for this PID
			// yet, so lets do that.
		    if (m_proposalUserCollections.ContainsKey(proposalID))
		    {
		        return m_proposalUserCollections[proposalID];
		    }

		    ObservableCollection<classProposalUser> newUserCollection;

		    // We weren't given a PID to filter out the results, so we are returning every user
		    // (unless told otherwise).
		    if (proposalID == string.Empty)
		    {
		        if (returnAllWhenEmpty)
		        {
		            newUserCollection = new ObservableCollection<classProposalUser>(m_proposalUsers);
		        }
		        else
		        {
		            return new ObservableCollection<classProposalUser>();
		        }
		    }
		    else if (m_pidIndexedCrossReferenceList.ContainsKey(proposalID))
		    {
		        var crossReferenceList = m_pidIndexedCrossReferenceList[proposalID];

		        // This really shouldn't be possible because the PIDs are generated from the
		        // User lists, so if there are no Users list, then there's no PID generated.
		        // Log there error, and hope that the person that reads it realizes that something
		        // is going wrong in the code.
		        if (crossReferenceList.Count == 0)
		        {
		            classApplicationLogger.LogError(
		                0,
		                string.Format(
		                    "Requested Proposal ID '{0}' has no users. Returning empty collection of Proposal Users.",
		                    proposalID));

		            newUserCollection = new ObservableCollection<classProposalUser>();
		        }
		        else
		        {
		            // The dictionary has already grouped the cross references by PID, so we just need
		            // to get the UIDs that are in that group.
		            var uIDs = from classUserIDPIDCrossReferenceEntry xRef in crossReferenceList
		                       select xRef.UserID;
		            var hashedUIDs = new HashSet<int>(uIDs);

		            // Get the users based on the given UIDs.
		            var selectedUsers = from classProposalUser user in m_proposalUsers
		                                where hashedUIDs.Contains(user.UserID)
		                                select user;

		            // Create the user collection and set it for future use.
		            newUserCollection = new ObservableCollection<classProposalUser>(selectedUsers);
		        }
		    }
		    // The given PID wasn't in our cross reference list, log the error
		    // and return insert an empty collection under it. And, don't insert
		    // this into the dictionary of user collections.
		    else
		    {
		        classApplicationLogger.LogMessage(
		            0,
		            string.Format(
		                "Requested Proposal ID '{0}' was not found. Returning empty collection of Proposal Users.",
		                proposalID));

		        // Return the collection before we can insert it into the dictionary.
		        return new ObservableCollection<classProposalUser>();
		    }

		    m_proposalUserCollections.Add(proposalID, newUserCollection);

		    return m_proposalUserCollections[proposalID];
		}
		private readonly Dictionary<string, ObservableCollection<classProposalUser>> m_proposalUserCollections;

		public ObservableCollection<string> ProposalIDs
		{
			get;
			private set;
		}

		public ObservableCollection<classProposalUser> FindSavedEMSLProposalUsers(string proposalID, StringCollection keys)
		{
			if (string.IsNullOrWhiteSpace(proposalID) || keys == null || keys.Count == 0)
				return new ObservableCollection<classProposalUser>();

			// We wont return this collection because this collection is supposed to be
			// inmutable and the items this method was designed for will be altering their
			// collections.
			var allOfProposal_sUsers = GetProposalUsers(proposalID);

			if (allOfProposal_sUsers == null || allOfProposal_sUsers.Count == 0)
				return new ObservableCollection<classProposalUser>();

			var selectedUsers = from classProposalUser u in allOfProposal_sUsers
								where keys.Contains(u.UserID.ToString())
								select u;

			var result = 
				new ObservableCollection<classProposalUser>(selectedUsers);
			return result;
		}
		#endregion


		#region Data Source Collections
		public ObservableCollection<string> ColumnData
		{
			get { return m_ColumnData; }
			private set
			{
				if (m_ColumnData != value)
				{
					m_ColumnData = value;
					OnPropertyChanged("ColumnData");
				}
			}
		}
		private ObservableCollection<string> m_ColumnData;

		/// <summary>
		/// This is a list of the DMS names of the different intruments.
		/// </summary>
		public ObservableCollection<string> InstrumentData
		{
			get { return m_instrumentData; }
			private set
			{
				if (m_instrumentData != value)
				{
					m_instrumentData = value;
					OnPropertyChanged("InstrumentData");
				}
			}
		}
		private ObservableCollection<string> m_instrumentData;

		/// <summary>
		/// This is a list of the names of the cart Operators.
		/// </summary>
		public ObservableCollection<string> OperatorData
		{
			get { return m_operatorData; }
			private set
			{
				if (m_operatorData != value)
				{
					m_operatorData = value;
					OnPropertyChanged("OperatorData");
				}
			}
		}
		private ObservableCollection<string> m_operatorData;

		public ObservableCollection<string> DatasetTypes
		{
			get { return m_datasetTypes; }
			private set
			{
				if (m_datasetTypes != value)
				{
					m_datasetTypes = value;
					OnPropertyChanged("DatasetTypes");
				}
			}
		}
		private ObservableCollection<string> m_datasetTypes;

		public ObservableCollection<string> SeparationTypes
		{
			get { return m_separationTypes; }
			private set
			{
				if (m_separationTypes != value)
				{
					m_separationTypes = value;
					OnPropertyChanged("SeparationTypes");
				}
			}
		}
		private ObservableCollection<string> m_separationTypes;

		public ObservableCollection<string> CartNames
		{
			get { return m_cartNames; }
			private set
			{
				if (m_cartNames != value)
				{
					m_cartNames = value;
					OnPropertyChanged("CartNames");
				}
			}
		}
		private ObservableCollection<string> m_cartNames;

		/// <summary>
		/// A list experiments from dms.
		/// </summary>
		/// <remarks>
		/// This isn't meant to be bound to directly, which is why it's a 
		/// list and not an ObservableCollection. Due to the large number
		/// of items this tends to hold, I would advise people to try to
		/// filter it down a bit first before inserting it into an
		/// ObservableCollection for binding.
		/// </remarks>
		public List<classExperimentData> Experiments
		{
			get { return m_experiments; }
			set
			{
				if (m_experiments != value)
				{
					m_experiments = value;
					OnPropertyChanged("Experiments");
				}
			}
		}
		private List<classExperimentData> m_experiments;
		#endregion


		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
