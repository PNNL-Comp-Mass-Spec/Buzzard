using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Utility;
using DynamicData;
using DynamicData.Binding;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class ExperimentsViewerViewModel : ReactiveObject, IDisposable
    {
        private string filterText;
        private ExperimentData selectedExperiment;

        private FilterOption selectedFilterOption;
        private List<string> autoCompleteBoxItems;
        private readonly ObservableAsPropertyHelper<bool> experimentSelected;
        private Func<ExperimentData, string> fieldSelector = x => x.Experiment;
        private readonly IDisposable connectionDisposable;

        public ExperimentsViewerViewModel()
        {
            FilterText = string.Empty;

            var filter = this.WhenValueChanged(x => x.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler).Select(x =>
                    new Func<ExperimentData, bool>(
                        y =>
                        {
                            if (string.IsNullOrWhiteSpace(x))
                            {
                                return true;
                            }
                            var value = fieldSelector(y);
                            return value?.StartsWith(x, StringComparison.OrdinalIgnoreCase) == true;
                        }));

            connectionDisposable = DMS_DataAccessor.Instance.Experiments.Connect().Filter(filter).ObserveOn(RxApp.MainThreadScheduler).Bind(out var filteredExperiments).Subscribe();
            Experiments = filteredExperiments;

            FilterOptions = Enum.GetValues(typeof(FilterOption)).Cast<FilterOption>().Where(x => x != FilterOption.None).ToArray();
            SelectedFilterOption = FilterOption.Experiment;

            experimentSelected = this.WhenAnyValue(x => x.SelectedExperiment).Select(x => x != null).ToProperty(this, x => x.ExperimentSelected, scheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.SelectedFilterOption).Subscribe(_ => SetAutoCompleteList());
            SetAutoCompleteList();
        }

        public void Dispose()
        {
            experimentSelected?.Dispose();
            connectionDisposable?.Dispose();
        }

        public bool ExperimentSelected => experimentSelected.Value;

        public IReadOnlyList<FilterOption> FilterOptions { get; }

        public ReadOnlyObservableCollection<ExperimentData> Experiments { get; }

        public FilterOption SelectedFilterOption
        {
            get => selectedFilterOption;
            set => this.RaiseAndSetIfChanged(ref selectedFilterOption, value);
        }

        public ExperimentData SelectedExperiment
        {
            get => selectedExperiment;
            set => this.RaiseAndSetIfChanged(ref selectedExperiment, value);
        }

        public string FilterText
        {
            get => filterText;
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                this.RaiseAndSetIfChanged(ref filterText, value);
            }
        }

        public List<string> AutoCompleteBoxItems
        {
            get => autoCompleteBoxItems;
            private set => this.RaiseAndSetIfChanged(ref autoCompleteBoxItems, value);
        }

        /// <summary>
        /// Gives the AutoFill box a list of times to use as its source based on the selected filter to use.
        /// </summary>
        private void SetAutoCompleteList()
        {
            var filterOption = SelectedFilterOption;
            var showAutoComplete = true;

            switch (filterOption)
            {
                case FilterOption.Researcher:
                    fieldSelector = exp => exp.Researcher;
                    break;

                case FilterOption.Experiment:
                    showAutoComplete = false;
                    fieldSelector = exp => exp.Experiment;
                    break;

                case FilterOption.Organism:
                    fieldSelector = exp => exp.Organism;
                    break;

                case FilterOption.Reason:
                    fieldSelector = exp => exp.Reason;
                    break;

                default:
                    showAutoComplete = false;
                    break;
            }

            if (!showAutoComplete)
            {
                AutoCompleteBoxItems = new List<string>();
            }
            else
            {
                var fieldItemList = DMS_DataAccessor.Instance.Experiments.Items.Select(fieldSelector).Distinct(new IgnoreCaseStringComparison()).ToList();
                fieldItemList.Sort();
                AutoCompleteBoxItems = fieldItemList;
            }
        }

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
