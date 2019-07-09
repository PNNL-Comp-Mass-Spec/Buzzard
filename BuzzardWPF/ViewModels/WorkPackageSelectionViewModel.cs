using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using DynamicData;
using DynamicData.Binding;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WorkPackageSelectionViewModel : ReactiveObject
    {
        public WorkPackageSelectionViewModel()
        {
            FilterText = string.Empty;

            FilterOptions = new List<WorkPackageFilterOption>(Enum.GetValues(typeof(WorkPackageFilterOption)).Cast<WorkPackageFilterOption>());
            SelectedFilterOption = WorkPackageFilterOption.ChargeCode;

            var workPackagesObservable = new SourceList<WorkPackageInfo>(DMS_DataAccessor.Instance.WorkPackages.AsObservableChangeSet());
            workPackageSelected = this.WhenAnyValue(x => x.SelectedWorkPackage).Select(x => x != null).ToProperty(this, x => x.WorkPackageSelected, scheduler: RxApp.MainThreadScheduler);

            //var wpFilter = this.WhenAnyValue(x => x.FilterText, x => x.SelectedFilterOption).Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler).Select(x => new )
            var wpFilter = this.WhenValueChanged(x => x.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .Select(x => new Func<WorkPackageInfo, bool>(y =>
                {
                    var value = fieldSelector(y);
                    return value != null && value.StartsWith(x, StringComparison.OrdinalIgnoreCase);
                }));

            //WorkPackagesFiltered = workPackagesObservable.Connect().Filter(wpFilter).ObserveOn(RxApp.MainThreadScheduler).AsObservableList();
            var loader = workPackagesObservable.Connect().Filter(wpFilter).ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out var workPackagesFiltered).Subscribe();

            WorkPackagesFiltered = workPackagesFiltered;

            AutoCompleteBoxItems = new List<string>();
            this.WhenAnyValue(x => x.SelectedFilterOption).Subscribe(_ => SetAutoCompleteList());
            SetAutoCompleteList();
        }

        private string filterText;
        private WorkPackageInfo selectedWorkPackage;

        private Func<WorkPackageInfo, string> fieldSelector = x => x.ChargeCode;

        private WorkPackageFilterOption selectedFilterOption;
        private List<string> autoCompleteBoxItems;
        private readonly ObservableAsPropertyHelper<bool> workPackageSelected;

        public bool WorkPackageSelected => workPackageSelected.Value;

        public IReadOnlyList<WorkPackageFilterOption> FilterOptions { get; }
        public ReadOnlyObservableCollection<WorkPackageInfo> WorkPackagesFiltered { get; }

        public WorkPackageFilterOption SelectedFilterOption
        {
            get => selectedFilterOption;
            set => this.RaiseAndSetIfChanged(ref selectedFilterOption, value);
        }

        public WorkPackageInfo SelectedWorkPackage
        {
            get => selectedWorkPackage;
            set => this.RaiseAndSetIfChanged(ref selectedWorkPackage, value);
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
        /// Gives the autofill box a list of times to use as its source based
        /// on the selected filter to use.
        /// </summary>
        private void SetAutoCompleteList()
        {
            var stringEqualityDeterminer = new StringComparision();
            var filterOption = SelectedFilterOption;
            var showAutoComplete = true;

            switch (filterOption)
            {
                case WorkPackageFilterOption.ChargeCode:
                    fieldSelector = x => x.ChargeCode;
                    showAutoComplete = false;
                    break;

                case WorkPackageFilterOption.Title:
                    fieldSelector = x => x.Title;
                    showAutoComplete = false;
                    break;

                case WorkPackageFilterOption.SubAccount:
                    fieldSelector = x => x.SubAccount;
                    break;

                case WorkPackageFilterOption.WorkBreakdownStructure:
                    fieldSelector = x => x.WorkBreakdownStructure;
                    break;

                case WorkPackageFilterOption.OwnerUserName:
                    fieldSelector = x => x.OwnerUserName;
                    break;

                case WorkPackageFilterOption.OwnerName:
                    fieldSelector = x => x.OwnerName;
                    break;

                default:
                    AutoCompleteBoxItems = new List<string>();
                    return;
            }

            if (!showAutoComplete)
            {
                AutoCompleteBoxItems.Clear();
            }
            else
            {
                var fieldItemList = DMS_DataAccessor.Instance.WorkPackages.Select(fieldSelector).Distinct(stringEqualityDeterminer).ToList();
                fieldItemList.Sort();
                AutoCompleteBoxItems = fieldItemList;
            }
        }

        private class StringComparision
            : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.ToUpper().GetHashCode();
            }
        }
    }

    public enum WorkPackageFilterOption
    {
        [Description("Charge Code")]
        ChargeCode,

        [Description("SubAccount")]
        SubAccount,

        [Description("Work Breakdown Structure")]
        WorkBreakdownStructure,

        [Description("Title")]
        Title,

        [Description("Owner Username")]
        OwnerUserName,

        [Description("Owner Name (Last, First)")]
        OwnerName,
    }
}
