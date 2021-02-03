using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public class WorkPackageSelectionViewModel : ReactiveObject, IDisposable
    {
        public WorkPackageSelectionViewModel()
        {
            FilterText = string.Empty;

            FilterOptions = Enum.GetValues(typeof(WorkPackageFilterOption)).Cast<WorkPackageFilterOption>().ToArray();
            SelectedFilterOption = WorkPackageFilterOption.ChargeCode;

            workPackageSelected = this.WhenAnyValue(x => x.SelectedWorkPackage).Select(x => x != null).ToProperty(this, x => x.WorkPackageSelected, scheduler: RxApp.MainThreadScheduler);

            var wpFilter = this.WhenValueChanged(x => x.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .Select(x => new Func<WorkPackageInfo, bool>(y =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                    {
                        return true;
                    }
                    var value = fieldSelector(y);
                    return value?.StartsWith(x, StringComparison.OrdinalIgnoreCase) == true;
                }));

            connectionDisposable = DMSDataAccessor.Instance.WorkPackages.Connect().Filter(wpFilter).ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out var workPackagesFiltered).Subscribe();

            WorkPackagesFiltered = workPackagesFiltered;

            AutoCompleteBoxItems = new List<string>();
            this.WhenAnyValue(x => x.SelectedFilterOption).Subscribe(_ => SetAutoCompleteList());
            SetAutoCompleteList();
        }

        public void Dispose()
        {
            workPackageSelected?.Dispose();
            connectionDisposable?.Dispose();
        }

        private string filterText;
        private WorkPackageInfo selectedWorkPackage;

        private Func<WorkPackageInfo, string> fieldSelector = x => x.ChargeCode;

        private WorkPackageFilterOption selectedFilterOption;
        private List<string> autoCompleteBoxItems;
        private readonly ObservableAsPropertyHelper<bool> workPackageSelected;
        private readonly IDisposable connectionDisposable;

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
        /// Gives the AutoFill box a list of times to use as its source based
        /// on the selected filter to use.
        /// </summary>
        private void SetAutoCompleteList()
        {
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
                AutoCompleteBoxItems = new List<string>();
            }
            else
            {
                var fieldItemList = DMSDataAccessor.Instance.WorkPackages.Items.Select(fieldSelector).Distinct(new IgnoreCaseStringComparison()).ToList();
                fieldItemList.Sort();
                AutoCompleteBoxItems = fieldItemList;
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
