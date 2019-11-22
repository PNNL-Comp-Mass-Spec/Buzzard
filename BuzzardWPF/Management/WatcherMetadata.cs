using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using BuzzardWPF.Properties;
using BuzzardWPF.ViewModels;
using DynamicData.Binding;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class WatcherMetadata : ReactiveObject, IStoredSettingsMonitor, IEmslUsageData
    {
        private string cartName;
        private string lcColumn;
        private string experimentName;
        private string workPackage;
        private string instrument;
        private string instrumentOperator;
        private string cartConfigName;
        private string separationType;
        private string datasetType;
        private string emslUsageType;
        private string emslProposalId;
        private string userComments;
        private string interestRating;
        private IReadOnlyList<string> cartConfigNameListForCart = new List<string>();

        public WatcherMetadata()
        {
            CartName = null;
            CartConfigName = null;
            DatasetType = null;
            Instrument = null;
            InstrumentOperator = null;
            SeparationType = null;
            EMSLUsageType = null;
            EMSLProposalID = null;
            WorkPackage = "none";

            this.WhenAnyValue(x => x.EMSLProposalUsers.Count).Subscribe(_ => SettingsChanged = true);

            this.WhenAnyValue(x => x.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                {
                    CartConfigNameListForCart = new List<string>();
                    return;
                }

                CartConfigNameListForCart = DMS_DataAccessor.Instance.GetCartConfigNamesForCart(x);
            });
        }

        /// <summary>
        /// This value tells the DatasetManager which LC Column to
        /// use for datasets that were found by the File Watcher.
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public string LCColumn
        {
            get => lcColumn;
            set => this.RaiseAndSetIfChangedMonitored(ref lcColumn, value);
        }

        /// <summary>
        /// This values tells the DatasetManager what experiment name to use
        /// for datasets that were found by the File Watcher, with no matching run request
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public string ExperimentName
        {
            get => experimentName;
            set => this.RaiseAndSetIfChangedMonitored(ref experimentName, value);
        }

        /// <summary>
        /// This values tells the DatasetManager what work package to use
        /// for datasets that were found by the File Watcher, with no matching run request
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public string WorkPackage
        {
            get => workPackage;
            set => this.RaiseAndSetIfChangedMonitored(ref workPackage, value);
        }

        /// <summary>
        /// This item contains a copy of the SelectedInstrument value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string Instrument
        {
            get => instrument;
            set => this.RaiseAndSetIfChangedMonitored(ref instrument, value);
        }

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via the WatcherConfigSelectedCartName setter</remarks>
        public IReadOnlyList<string> CartConfigNameListForCart
        {
            get => cartConfigNameListForCart;
            private set => this.RaiseAndSetIfChanged(ref cartConfigNameListForCart, value);
        }

        /// <summary>
        /// This item contains a copy of the SelectedCartName value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string CartName
        {
            get => cartName;
            set => this.RaiseAndSetIfChangedMonitored(ref cartName, value);
        }

        /// <summary>
        /// This item contains a copy of the SelectedCartConfigName value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string CartConfigName
        {
            get => cartConfigName;
            set => this.RaiseAndSetIfChangedMonitored(ref cartConfigName, value);
        }

        /// <summary>
        /// This item contains a copy of the SelectedSeperationType value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string SeparationType
        {
            get => separationType;
            set => this.RaiseAndSetIfChangedMonitored(ref separationType, value);
        }

        /// <summary>
        /// This item contains a copy of the SelectedDatasetType value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string DatasetType
        {
            get => datasetType;
            set => this.RaiseAndSetIfChangedMonitored(ref datasetType, value);
        }

        /// <summary>
        /// This item contains a copy of the SelectedOperator value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string InstrumentOperator
        {
            get => instrumentOperator;
            set => this.RaiseAndSetIfChangedMonitored(ref instrumentOperator, value);
        }

        public string EMSLUsageType
        {
            get => emslUsageType;
            set => this.RaiseAndSetIfChangedMonitored(ref emslUsageType, value);
        }

        public string EMSLProposalID
        {
            get => emslProposalId;
            set => this.RaiseAndSetIfChangedMonitored(ref emslProposalId, value);
        }

        public string UserComments
        {
            get => userComments;
            set => this.RaiseAndSetIfChangedMonitored(ref userComments, value);
        }

        public string InterestRating
        {
            get => interestRating;
            set => this.RaiseAndSetIfChangedMonitored(ref interestRating, value);
        }

        public ObservableCollectionExtended<ProposalUser> EMSLProposalUsers { get; } = new ObservableCollectionExtended<ProposalUser>();

        public bool SettingsChanged { get; set; }

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.WatcherCartName = CartName;
            Settings.Default.WatcherCartConfigName = CartConfigName;
            Settings.Default.WatcherDatasetType = DatasetType;
            Settings.Default.WatcherInstrument = Instrument;
            Settings.Default.WatcherOperator = InstrumentOperator;
            Settings.Default.WatcherExperimentName = ExperimentName;
            Settings.Default.WatcherColumn = LCColumn;
            Settings.Default.WatcherComment = UserComments;
            Settings.Default.WatcherSeparationType = SeparationType;
            Settings.Default.WatcherEMSLUsageType = EMSLUsageType;
            Settings.Default.WatcherEMSLProposalID = EMSLProposalID;
            Settings.Default.WatcherInterestRating = InterestRating;
            Settings.Default.WatcherWorkPackage = WorkPackage;

            var selectedEmslUsers = new StringCollection();
            foreach (var user in EMSLProposalUsers)
                selectedEmslUsers.Add(user.UserID.ToString());

            Settings.Default.WatcherEMSLUsers = selectedEmslUsers;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.WatcherExperimentName;
            EMSLUsageType = Settings.Default.WatcherEMSLUsageType;
            EMSLProposalID = Settings.Default.WatcherEMSLProposalID;

            List<string> selectedUsers;
            if (Settings.Default.WatcherEMSLUsers == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.WatcherEMSLUsers.Cast<string>().ToList();

            EMSLProposalUsers.Load(DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers));

            UserComments = Settings.Default.WatcherComment;
            InterestRating = Settings.Default.WatcherInterestRating;

            /*
             * The following settings need to be checked before being applied
             * due to the fact that they need to be valid options within the
             * collections that act as their sources.
             */
            CartName = CheckSetting(
                Settings.Default.WatcherCartName,
                DMS_DataAccessor.Instance.CartNamesItems,
                "Cart");

            CartConfigName = CheckSetting(
                Settings.Default.WatcherCartConfigName,
                DMS_DataAccessor.Instance.GetCartConfigNamesForCart(CartName),
                "CartConfig");

            DatasetType = CheckSetting(
                Settings.Default.WatcherDatasetType,
                DMS_DataAccessor.Instance.DatasetTypesItems,
                "Column Type");

            Instrument = CheckSetting(
                Settings.Default.WatcherInstrument,
                DMS_DataAccessor.Instance.InstrumentDataItems,
                "Instrument");

            InstrumentOperator = CheckSetting(
                Settings.Default.WatcherOperator,
                DMS_DataAccessor.Instance.OperatorDataItems,
                "Operator");

            SeparationType = CheckSetting(
                Settings.Default.WatcherSeparationType,
                DMS_DataAccessor.Instance.SeparationTypesItems,
                "Separation Type");

            LCColumn = CheckSetting(
                Settings.Default.WatcherColumn,
                DMS_DataAccessor.Instance.ColumnDataItems,
                "LC Column");

            WorkPackage = CheckSetting(
                Settings.Default.WatcherWorkPackage,
                DMS_DataAccessor.Instance.WorkPackages.Items.Select(x => x.ChargeCode),
                "Work Package");

            if (string.IsNullOrWhiteSpace(WorkPackage))
            {
                WorkPackage = "none";
            }

            SettingsChanged = false;
        }

        /// <summary>
        /// This method makes sure that the loading setting is still valid. If it's
        /// valid, it will be returned. If not, an error message will be logged and
        /// a null value will be returned in place of the setting.
        /// </summary>
        /// <remarks>
        /// A setting can become invalid when it's removed as an option from the
        /// database.
        /// </remarks>
        private string CheckSetting(string setting, IEnumerable<string> options, string errorIntro)
        {
            var s = " was not found when restoring settings for the File Watcher Configuration.";

            if (string.IsNullOrWhiteSpace(setting))
            {
                // there is no setting, so return something
                // that will make sure that nothing is selected
                // in the UI.
                setting = null;
            }
            else if (!options.Contains(setting))
            {
                // The setting is not valid. Log the error
                // and return something that will make sure
                // the UI doesn't select anything for this
                // setting.
                ApplicationLogger.LogError(
                    0,
                    string.Format(
                        "{2} {0}{1}",
                        setting,
                        s,
                        errorIntro));
                setting = null;
            }

            return setting;
        }
    }
}
