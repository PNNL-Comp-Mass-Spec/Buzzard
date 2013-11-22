﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using BuzzardWPF.Data;

using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;
using BuzzardWPF.LcmsNetTemp;


namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for Filldown.xaml
    /// </summary>
    public partial class FilldownWindow 
		: Window, INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Attributes
		private FilldownBuzzardDataset			m_dataset;

		private ObservableCollection<string>	m_operatorsSource;
		private ObservableCollection<string>	m_instrumentsSource;
		private ObservableCollection<string>	m_datasetTypesSource;
		private ObservableCollection<string>	m_separationTypeSource;
		private ObservableCollection<string>	m_cartNameListSource;
		private ObservableCollection<string>	m_emslUsageTypeSource;
		private ObservableCollection<string>	m_lcColumnSource;
		#endregion


		public FilldownWindow()
        {
            InitializeComponent();
			DataContext = this;

			OperatorsSource			= new ObservableCollection<string>();
			InstrumentSource		= new ObservableCollection<string>();
			DatasetTypesSource		= new ObservableCollection<string>();
			SeparationTypeSource	= new ObservableCollection<string>();

			CartNameListSource		= new ObservableCollection<string>();
			EmslUsageTypeSource		= new ObservableCollection<string>();
			InterestRatingSource	= DatasetManager.INTEREST_RATINGS_COLLECTION;
			EMSLProposalIDs			= DMS_DataAccessor.Instance.ProposalIDs;

			EMSLProposalUsersSource = new ObservableCollection<classProposalUser>();
            Dataset					= new FilldownBuzzardDataset(); 
        }


		#region Properties
		public ObservableCollection<string> EMSLProposalIDs
		{
			get { return m_emslProposalIDs; }
			set
			{
				if (m_emslProposalIDs != value)
				{
					m_emslProposalIDs = value;
					OnPropertyChanged("EMSLProposalIDs");
				}
			}
		}
		private ObservableCollection<string> m_emslProposalIDs;

		public ObservableCollection<string> LCColumnSource
		{
			get { return m_lcColumnSource; }
			set
			{
				if (m_lcColumnSource != value)
				{
					m_lcColumnSource = value;
					OnPropertyChanged("LCColumnSource");
				}
			}
		}

		public FilldownBuzzardDataset Dataset
		{
			get { return m_dataset; }
			set
			{
				if (m_dataset != value)
				{
					if (m_dataset != null)
					{
						DatasetDMS = null;
						m_dataset.PropertyChanged -= DatasetPropertyChanged;
					}

					m_dataset = value;
					OnPropertyChanged("Dataset");

					if (m_dataset != null)
					{
						m_dataset.PropertyChanged += DatasetPropertyChanged;
						DatasetDMS = m_dataset.DMSData;
					}

					FillInEMSLProposalStuff();
				}
			}
		}

		private DMSData DatasetDMS
		{
			get { return m_datasetDMS; }
			set
			{
				if (m_datasetDMS != value)
				{
					if (m_datasetDMS != null)
						m_datasetDMS.PropertyChanged -= DMSDataPropertyChanged;

					m_datasetDMS = value;

					if (m_datasetDMS != null)
						m_datasetDMS.PropertyChanged += DMSDataPropertyChanged;
				}
			}
		}
		private DMSData m_datasetDMS;

		public ObservableCollection<string> OperatorsSource
		{
			get { return m_operatorsSource; }
			set
			{
				if (m_operatorsSource != value)
				{
					m_operatorsSource = value;
					OnPropertyChanged("OperatorsSource");
				}
			}
		}

		public ObservableCollection<string> InstrumentSource
		{
			get { return m_instrumentsSource; }
			set
			{
				if (m_instrumentsSource != value)
				{
					m_instrumentsSource = value;
					OnPropertyChanged("InstrumentSource");
				}
			}
		}

		public ObservableCollection<string> DatasetTypesSource
		{
			get { return m_datasetTypesSource; }
			set
			{
				if (m_datasetTypesSource != value)
				{
					m_datasetTypesSource = value;
					OnPropertyChanged("DatasetTypesSource");
				}
			}
		}

		public ObservableCollection<string> SeparationTypeSource
		{
			get { return m_separationTypeSource; }
			set
			{
				if (m_separationTypeSource != value)
				{
					m_separationTypeSource = value;
					OnPropertyChanged("SeparationTypeSource");
				}
			}
		}

		public ObservableCollection<string> CartNameListSource
		{
			get { return m_cartNameListSource; }
			set
			{
				if (m_cartNameListSource != value)
				{
					m_cartNameListSource = value;
					OnPropertyChanged("CartNameListSource");
				}
			}
		}

		public ObservableCollection<string> EmslUsageTypeSource
		{
			get { return m_emslUsageTypeSource; }
			set
			{
				if (m_emslUsageTypeSource != value)
				{
					m_emslUsageTypeSource = value;
					OnPropertyChanged("EmslUsageTypeSource");
				}
			}
		}

		public ObservableCollection<string> InterestRatingSource
		{
			get { return m_interestRatingSource; }
			set
			{
				if (m_interestRatingSource != value)
				{
					m_interestRatingSource = value;
					OnPropertyChanged("InterestRatingSource");
				}
			}
		}
		private ObservableCollection<string> m_interestRatingSource;

		public ObservableCollection<classProposalUser> EMSLProposalUsersSource
		{
			get { return m_EMSLProposalUsersSource; }
			set
			{
				if (m_EMSLProposalUsersSource != value)
				{
					m_EMSLProposalUsersSource = value;
					OnPropertyChanged("EMSLProposalUsersSource");
				}
			}
		}
		private ObservableCollection<classProposalUser> m_EMSLProposalUsersSource;
		#endregion


		#region Event Handlers
		private void DatasetPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "DMSData")
			{
				DatasetDMS = Dataset.DMSData;
			}
		}

		private void DMSDataPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "EMSLProposalID")
			{
				EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(DatasetDMS.EMSLProposalID);

				this.PUserSelector.Text = string.Empty;
				this.PUserSelector.SelectedItems.Clear();
			}
		}

		private void m_okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

		private void UseAll_Click(object sender, RoutedEventArgs e)
		{
			ShouldUseAllSettings(true);
		}

		private void UseNone_Click(object sender, RoutedEventArgs e)
		{
			ShouldUseAllSettings(false);
		}

		private void PickExperiment_Click(object sender, RoutedEventArgs e)
		{
			ExperimentsDialog dialog = new ExperimentsDialog();

			bool stop = dialog.ShowDialog() != true;
			if (stop)
				return;

			var selectedExperiment = dialog.SelectedExperiment;
			this.Dataset.ExperimentName = selectedExperiment.Experiment;
		}
		
		private void ProposalIDSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems != null && e.AddedItems.Count > 0)
			{
				Dataset.DMSData.EMSLProposalID = e.AddedItems[0].ToString();
				ProposalIDSelector.SelectedIndex = -1;
			}
		}
		#endregion


		#region Methods
		private void FillInEMSLProposalStuff()
		{
			if (Dataset == null)
				return;

			EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(DatasetDMS.EMSLProposalID);

			string selectedText = string.Empty;
			foreach (var user in Dataset.EMSLProposalUsers)
				selectedText += user.UserName + "; ";
			PUserSelector.Text = selectedText;
		}

		private void ShouldUseAllSettings(bool shouldWe)
		{
			if (Dataset == null)
			{
				classApplicationLogger.LogError(0, "Filldown Dataset is missing from Filldown Window.");
				return;
			}

			Dataset.ShouldUseCart				= shouldWe;
			Dataset.ShouldUseDatasetType		= shouldWe;
			Dataset.ShouldUseEMSLProposalID		= shouldWe;
			Dataset.ShouldUseEMSLUsageType		= shouldWe;

			Dataset.ShouldUseInstrumentType		= shouldWe;
			Dataset.ShouldUseOperator			= shouldWe;
			Dataset.ShouldUseSeparationType		= shouldWe;
			Dataset.ShouldUseExperimentName		= shouldWe;

			Dataset.ShouldUseLCColumn			= shouldWe;
			Dataset.ShouldUseInterestRating		= shouldWe;
			Dataset.ShouldUseEMSLProposalUsers	= shouldWe;
            Dataset.ShouldUseComment            = shouldWe;
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
