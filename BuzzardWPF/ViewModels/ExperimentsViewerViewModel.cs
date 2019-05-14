using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class ExperimentsViewerViewModel : ReactiveObject
    {
        #region Attributes
        private string m_filterText;
        private ExperimentData m_selectedExperiment;

        private List<ExperimentData> m_experimentList;
        private List<string> m_experimentNameList;
        private List<string> m_organismNameList;
        private List<string> m_researcherList;
        private List<string> m_reason;

        private ReactiveList<ExperimentData> m_experiments;
        private FilterOption selectedFilterOption;
        private List<string> autoCompleteBoxItems;
        private readonly ObservableAsPropertyHelper<bool> experimentSelected;

        #endregion

        #region Initialization
        public ExperimentsViewerViewModel()
        {
            FilterText = string.Empty;

            FilterOptions = new ReactiveList<FilterOption>(Enum.GetValues(typeof(FilterOption)).Cast<FilterOption>().Where(x => x != FilterOption.None));
            SelectedFilterOption = FilterOption.Experiment;
            SearchCommand = ReactiveCommand.Create(Search);

            experimentSelected = this.WhenAnyValue(x => x.SelectedExperiment).Select(x => x != null).ToProperty(this, x => x.ExperimentSelected, scheduler: RxApp.MainThreadScheduler);

            RxApp.MainThreadScheduler.Schedule(LoadExperiments);
            SetAutoCompleteList();
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
            var stringEqualityDeterminer = new StringComparision();

            // Building lists that we can use to narrow down the
            // number of experiments to insert into the UI
            m_researcherList = m_experimentList.Select(x => x.Researcher.Trim()).Distinct(stringEqualityDeterminer).ToList();
            m_organismNameList = m_experimentList.Select(x => x.Organism.Trim()).Distinct(stringEqualityDeterminer).ToList();
            m_experimentNameList = m_experimentList.Select(x => x.Experiment.Trim()).Distinct(stringEqualityDeterminer).ToList();
            m_reason = m_experimentList.Select(x => x.Reason).Distinct(stringEqualityDeterminer).ToList();

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

        public ReactiveCommand<Unit, Unit> SearchCommand { get; }

        public bool ExperimentSelected => experimentSelected.Value;

        public IReadOnlyReactiveList<FilterOption> FilterOptions { get; }

        public FilterOption SelectedFilterOption
        {
            get => selectedFilterOption;
            set => this.RaiseAndSetIfChanged(ref selectedFilterOption, value);
        }

        public ExperimentData SelectedExperiment
        {
            get => m_selectedExperiment;
            set => this.RaiseAndSetIfChanged(ref m_selectedExperiment, value);
        }

        public string FilterText
        {
            get => m_filterText;
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.RaiseAndSetIfChanged(ref m_filterText, value);
            }
        }

        public ReactiveList<ExperimentData> Experiments
        {
            get => m_experiments;
            set => this.RaiseAndSetIfChanged(ref m_experiments, value);
        }

        public List<string> AutoCompleteBoxItems
        {
            get => autoCompleteBoxItems;
            private set => this.RaiseAndSetIfChanged(ref autoCompleteBoxItems, value);
        }

        #endregion

        #region Methods
        /// <summary>
        /// Gives the autofill box a list of times to use as its source based
        /// on the selected filter to use.
        /// </summary>
        private void SetAutoCompleteList()
        {
            var filterOption = SelectedFilterOption;

            switch (filterOption)
            {
                case FilterOption.Researcher:
                    AutoCompleteBoxItems = m_researcherList;
                    break;

                case FilterOption.Experiment:
                    AutoCompleteBoxItems = m_experimentNameList;
                    break;

                case FilterOption.Organism:
                    AutoCompleteBoxItems = m_organismNameList;
                    break;

                case FilterOption.Reason:
                    AutoCompleteBoxItems = m_reason;
                    break;

                default:
                    AutoCompleteBoxItems = new List<string>();
                    break;
            }
        }

        /// <summary>
        /// Searches for experiments that match the selected filter and
        /// filter screen.
        /// </summary>
        private void Search()
        {
            Experiments = null;
            var filterOption = SelectedFilterOption;

            IEnumerable<ExperimentData> matches = null;

            try
            {
                switch (filterOption)
                {
                    case FilterOption.Researcher:
                        matches = m_experimentList.Where(exp => exp.Researcher.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase));
                        break;

                    case FilterOption.Experiment:
                        matches = m_experimentList.Where(exp => exp.Experiment.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase));
                        break;

                    case FilterOption.Organism:
                        matches = m_experimentList.Where(exp => exp.Organism.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase));
                        break;

                    case FilterOption.Reason:
                        matches = m_experimentList.Where(exp => exp.Reason != null && exp.Reason.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase));
                        break;

                    default:
                        break;
                }

                if (matches == null)
                    return;

                var tempRef = new ReactiveList<ExperimentData>(matches);
                Experiments = tempRef;
            }
            catch (Exception ex)
            {
                // Search error; do not update Experiments
                Console.WriteLine("Error ignored in ExperimentsViewerViewModel.Search_Click: " + ex.Message);
            }

        }
        #endregion

        public enum FilterOption
        {
            Researcher,
            Experiment,
            Organism,
            Reason,
            None
        }
    }
}
