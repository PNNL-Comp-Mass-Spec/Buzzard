using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.Logging;
using BuzzardWPF.Properties;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class WatcherMetadata : ReactiveObject, IStoredSettingsMonitor
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
        private EmslUsageType emslUsageType;
        private string emslProposalId;
        private ProposalUser emslProposalUser;
        private string userComments;
        private string interestRating;

        public WatcherMetadata()
        {
            CartName = null;
            CartConfigName = null;
            DatasetType = null;
            Instrument = null;
            InstrumentOperator = null;
            SeparationType = null;
            EMSLUsageType = EmslUsageType.NONE;
            EMSLProposalID = null;
            EMSLProposalUser = null;
            WorkPackage = "none";
            InterestRating = "Unreviewed";

            // Set the instrument name to a value valid for the instrument host.
            DMSDataAccessor.Instance.WhenAnyValue(x => x.InstrumentsMatchingHost, x => x.InstrumentsMatchingHost.Count)
                .Where(x => x.Item2 > 0).Subscribe(x => Instrument = x.Item1.First());
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
        /// This item contains a copy of the SelectedSeparationType value of
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

        public EmslUsageType EMSLUsageType
        {
            get => emslUsageType;
            set => this.RaiseAndSetIfChangedMonitored(ref emslUsageType, value);
        }

        public string EMSLProposalID
        {
            get => emslProposalId;
            set => this.RaiseAndSetIfChangedMonitored(ref emslProposalId, value);
        }

        public ProposalUser EMSLProposalUser
        {
            get => emslProposalUser;
            set => this.RaiseAndSetIfChangedMonitored(ref emslProposalUser, value);
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
            Settings.Default.WatcherEMSLUsageType = EMSLUsageType.ToString();
            Settings.Default.WatcherEMSLProposalID = EMSLProposalID;
            Settings.Default.WatcherInterestRating = InterestRating;
            Settings.Default.WatcherWorkPackage = WorkPackage;
            Settings.Default.WatcherEMSLUser = EMSLProposalUser?.UserID.ToString();

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.WatcherExperimentName;
            EMSLUsageType = Settings.Default.WatcherEMSLUsageType.ToEmslUsageType();
            EMSLProposalID = Settings.Default.WatcherEMSLProposalID;
            EMSLProposalUser = DMSDataAccessor.Instance.FindSavedEMSLProposalUser(EMSLProposalID, Settings.Default.WatcherEMSLUser);

            UserComments = Settings.Default.WatcherComment;
            InterestRating = Settings.Default.WatcherInterestRating;
            InterestRating = "Unreviewed";

            /*
             * The following settings need to be checked before being applied
             * due to the fact that they need to be valid options within the
             * collections that act as their sources.
             */
            CartName = CheckSetting(
                Settings.Default.WatcherCartName,
                DMSDataAccessor.Instance.CartNamesItems,
                "Cart");

            CartConfigName = CheckSetting(
                Settings.Default.WatcherCartConfigName,
                DMSDataAccessor.Instance.GetCartConfigNamesForCart(CartName),
                "CartConfig");

            DatasetType = CheckSetting(
                Settings.Default.WatcherDatasetType,
                DMSDataAccessor.Instance.DatasetTypesItems,
                "Column Type");

            Instrument = CheckSetting(
                Settings.Default.WatcherInstrument,
                DMSDataAccessor.Instance.InstrumentNameItems,
                "Instrument");

            InstrumentOperator = CheckSetting(
                Settings.Default.WatcherOperator,
                DMSDataAccessor.Instance.OperatorDataItems,
                "Operator");

            SeparationType = CheckSetting(
                Settings.Default.WatcherSeparationType,
                DMSDataAccessor.Instance.SeparationTypesItems,
                "Separation Type");

            LCColumn = CheckSetting(
                Settings.Default.WatcherColumn,
                DMSDataAccessor.Instance.ColumnDataItems,
                "LC Column");

            WorkPackage = CheckSetting(
                Settings.Default.WatcherWorkPackage,
                DMSDataAccessor.Instance.WorkPackages.Items.Select(x => x.ChargeCode),
                "Work Package");

            if (string.IsNullOrWhiteSpace(WorkPackage))
            {
                WorkPackage = "none";
            }

            SettingsChanged = false;
        }

        /// <summary>
        /// This method makes sure that the loaded setting is still valid. If it's
        /// valid, it will be returned. If not, an error message will be logged and
        /// a null value will be returned in place of the setting.
        /// </summary>
        /// <remarks>
        /// A setting can become invalid when it's removed as an option from the
        /// database.
        /// </remarks>
        private string CheckSetting(string setting, IEnumerable<string> options, string errorIntro)
        {
            const string suffix = " was not found when restoring settings for the File Watcher Configuration.";

            if (string.IsNullOrWhiteSpace(setting))
            {
                // there is no setting, so return something
                // that will make sure that nothing is selected
                // in the UI.
                return null;
            }

            if (options.Contains(setting))
                return setting;

            // The setting is not valid.
            // Log the error and return something that will make sure
            // the UI doesn't select anything for this setting.
            ApplicationLogger.LogError(
                0,
                string.Format(
                    "{0} {1}{2}",
                    errorIntro,
                    setting,
                    suffix
                ));
            return null;
        }
    }
}
