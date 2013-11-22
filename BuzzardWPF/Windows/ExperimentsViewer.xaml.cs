using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using BuzzardWPF.Data;

using LcmsNetDataClasses;


namespace BuzzardWPF.Windows
{
	/// <summary>
	/// Interaction logic for ExperimentsViewer.xaml
	/// </summary>
	public partial class ExperimentsViewer 
		: UserControl, INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler	PropertyChanged;
		#endregion


		#region Attributes
		private string						m_filterText;
		private classExperimentData			m_selectedExperiment;

		private List<classExperimentData>	m_experimentList;
		private List<string>				m_experimentNameList;
		private List<string>				m_organismNameList;
		private List<string>				m_researcherList;
		private List<string>				m_reason;

		private ObservableCollection<classExperimentData> m_experiments;
		#endregion


		#region Initialization
		public ExperimentsViewer()
		{
			InitializeComponent();
			this.DataContext = this;

			FilterText = string.Empty;

			Action loadExperiments = delegate
			{
				LoadExperiments();
			};
			this.Dispatcher.BeginInvoke(loadExperiments, DispatcherPriority.Render);
		}

		private void LoadExperiments()
		{
			// Get the list of experiments.
			// I don't know why I can't just put the entire list
			// right into an observable collection and throw it
			// into the Experiments property. What I do know is
			// that, if we do that at this time, the memory usage
			// will grow by over a GB, and we'll be waiting forever
			// for the UI thread to unfreeze. We can throw everything
			// into Experiments when we run a search, but we can't
			// do it here.
			// -FC
			m_experimentList = DMS_DataAccessor.Instance.Experiments;
			StringComparision stringEqualityDeterminer = new StringComparision();

			// Building lists that we can use to narrow down the
			// number of experiments to insert into the UI
			var x = (from classExperimentData exp in m_experimentList
					 select exp.Researcher.Trim()).Distinct(stringEqualityDeterminer);
			m_researcherList = new List<string>(x);

			x = (from classExperimentData exp in m_experimentList
				 select exp.Organism.Trim()).Distinct(stringEqualityDeterminer);
			m_organismNameList = new List<string>(x);

			x = (from classExperimentData exp in m_experimentList
				 select exp.Experiment.Trim()).Distinct(stringEqualityDeterminer);
			m_experimentNameList = new List<string>(x);

			x = (from classExperimentData exp in m_experimentList
				 select exp.Reason).Distinct(stringEqualityDeterminer);
			m_reason = new List<string>(x);

			m_experimentNameList.Sort();
			m_organismNameList.Sort();
			m_reason.Sort();
			m_researcherList.Sort();
		}

		private class StringComparision 
			: IEqualityComparer<string>
		{
			public bool Equals(string x, string y)
			{
				bool areTheyEqual;

				if (x == null && y == null)
					areTheyEqual = true;
				else if (x == null || y == null)
					areTheyEqual = false;
				else
					areTheyEqual = x.Equals(y, StringComparison.OrdinalIgnoreCase);

				return areTheyEqual;
			}

			public int GetHashCode(string obj)
			{
				if (obj != null)
					return obj.ToUpper().GetHashCode();
				
				throw new Exception();
			}
		}
		#endregion


		#region Properties
		public classExperimentData SelectedExperiment
		{
			get { return m_selectedExperiment; }
			set
			{
				if (m_selectedExperiment != value)
				{
					m_selectedExperiment = value;
					OnPropertyChanged("SelectedExperiment");
				}
			}
		}

		public string FilterText
		{
			get { return m_filterText; }
			set
			{
				if (m_filterText != value)
				{
					m_filterText = value;

					if (value == null)
						m_filterText = string.Empty;

					OnPropertyChanged("FilterText");
				}
			}
		}

		public ObservableCollection<classExperimentData> Experiments
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
		#endregion


		#region Event Handlers
		/// <summary>
		/// Gives the autofill box a list of times to use as its source based
		/// on the selected filter to use.
		/// </summary>
		private void FilterBox_Populating(object sender, PopulatingEventArgs e)
		{
			FilterOption filterOption = GetSelectedFilter();

			switch (filterOption)
			{
			case FilterOption.Researcher:
				m_filterBox.ItemsSource = m_researcherList;
				break;

			case FilterOption.Experiment:
				m_filterBox.ItemsSource = m_experimentNameList;
				break;

			case FilterOption.Organism:
				m_filterBox.ItemsSource = m_organismNameList;
				break;

			case FilterOption.Reason:
				m_filterBox.ItemsSource = m_reason;
				break;

			default:
				m_filterBox.ItemsSource = new string[] { };
				break;
			}

			m_filterBox.PopulateComplete();
		}

		/// <summary>
		/// Searches for experiments that meet-up to the selected filter and
		/// filter screen.
		/// </summary>
		private void Search_Click(object sender, RoutedEventArgs e)
		{
			Experiments = null;
			FilterOption filterOption = GetSelectedFilter();

			IEnumerable<classExperimentData> x = null;

			switch (filterOption)
			{
			case FilterOption.Researcher:
				x = from classExperimentData exp in m_experimentList
					where exp.Researcher.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase)
					select exp;
				break;

			case FilterOption.Experiment:
				x = from classExperimentData exp in m_experimentList
					where exp.Experiment.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase)
					select exp;
				break;

			case FilterOption.Organism:
				x = from classExperimentData exp in m_experimentList
					where exp.Organism.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase)
					select exp;
				break;

			case FilterOption.Reason:
				x = from classExperimentData exp in m_experimentList
					where exp.Reason.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase)
					select exp;
				break;

			default:
				break;
			}

			if (x == null)
				return;

			ObservableCollection<classExperimentData> tempRef = new ObservableCollection<classExperimentData>(x);
			Experiments = tempRef;
		}
		#endregion


		#region Methods
		/// <summary>
		/// Gets the filter that was selected from the drop down box.
		/// </summary>
		private FilterOption GetSelectedFilter()
		{
			FilterOption result = FilterOption.None;

			ComboBoxItem selectedItem = m_comboBox.SelectedItem as ComboBoxItem;
			if (selectedItem == null)
				return result;

			string tag = selectedItem.Tag as string;

			switch (tag)
			{
			case "rs":
				result = FilterOption.Researcher;
				break;

			case "ex":
				result = FilterOption.Experiment;
				break;

			case "or":
				result = FilterOption.Organism;
				break;

			case "ra":
				result = FilterOption.Reason;
				break;

			default:
				result = FilterOption.None;
				break;
			}

			return result;
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		private enum FilterOption
		{
			Researcher, 
			Experiment, 
			Organism, 
			Reason, 
			None
		}
	}
}
