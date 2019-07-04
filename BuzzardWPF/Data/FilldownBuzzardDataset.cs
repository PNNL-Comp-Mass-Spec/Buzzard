using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using ReactiveUI;

namespace BuzzardWPF.Data
{
    public class FilldownBuzzardDataset : BuzzardDataset, IStoredSettingsMonitor
    {
        #region Attributes
        private bool useOperator;
        private bool useDatasetType;
        private bool useSeperationType;
        private bool useInstrumentType;

        private bool useCart;
        private bool useEmslProposalID;
        private bool useEmslUsageType;
        private bool useExperimentName;

        private bool useLcColumn;
        private bool useComment;
        private bool useInterestRating;
        private bool useEMSLProposalUsers;
        #endregion

        #region Initialize
        public FilldownBuzzardDataset()
        {
            useDatasetType = true;
            UseCart = true;
            UseDatasetType = true;
            UseEMSLProposalID = true;
            UseEMSLUsageType = true;

            UseInstrumentType = true;
            UseOperator = true;
            UseSeparationType = true;
            UseExperimentName = true;

            UseLcColumn = true;
            UseInterestRating = true;
            UseEMSLProposalUsers = true;
            UseComment = true;

            // Monitors for propertyChanged events
            this.WhenAnyValue(x => x.DmsData, x => x.DmsData.DatasetType, x => x.DmsData.EMSLUsageType, x => x.DmsData.EMSLProposalID)
                .Subscribe(x => SettingsChanged = true);
            this.WhenAnyValue(x => x.DmsData.CommentAddition, x => x.Operator, x => x.SeparationType, x => x.ColumnName, x => x.InstrumentName)
                .Subscribe(x => SettingsChanged = true);
            this.WhenAnyValue(x => x.DmsData.CartName, x => x.DmsData.CartConfigName, x => x.InterestRating, x => x.DmsData.Experiment)
                .Subscribe(x => SettingsChanged = true);

            LoadSettings();
        }
        #endregion

        #region Properties
        public bool UseLcColumn
        {
            get => useLcColumn;
            set => this.RaiseAndSetIfChanged(ref useLcColumn, value);
        }

        public bool UseExperimentName
        {
            get => useExperimentName;
            set => this.RaiseAndSetIfChanged(ref useExperimentName, value);
        }

        public bool UseComment
        {
            get => useComment;
            set => this.RaiseAndSetIfChanged(ref useComment, value);
        }

        public bool UseOperator
        {
            get => useOperator;
            set => this.RaiseAndSetIfChanged(ref useOperator, value);
        }

        public bool UseDatasetType
        {
            get => useDatasetType;
            set => this.RaiseAndSetIfChanged(ref useDatasetType, value);
        }

        public bool UseSeparationType
        {
            get => useSeperationType;
            set => this.RaiseAndSetIfChanged(ref useSeperationType, value);
        }

        public bool UseInstrumentType
        {
            get => useInstrumentType;
            set => this.RaiseAndSetIfChanged(ref useInstrumentType, value);
        }

        public bool UseCart
        {
            get => useCart;
            set => this.RaiseAndSetIfChanged(ref useCart, value);
        }

        public bool UseEMSLProposalID
        {
            get => useEmslProposalID;
            set => this.RaiseAndSetIfChanged(ref useEmslProposalID, value);
        }

        public bool UseEMSLUsageType
        {
            get => useEmslUsageType;
            set => this.RaiseAndSetIfChanged(ref useEmslUsageType, value);
        }

        public bool UseInterestRating
        {
            get => useInterestRating;
            set => this.RaiseAndSetIfChanged(ref useInterestRating, value);
        }

        public bool UseEMSLProposalUsers
        {
            get => useEMSLProposalUsers;
            set => this.RaiseAndSetIfChanged(ref useEMSLProposalUsers, value);
        }

        public bool SettingsChanged { get; set; }

        #endregion

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.FilldownComment = DmsData.CommentAddition;
            Settings.Default.FilldownOperator = Operator;
            Settings.Default.FilldownDatasetType = DmsData.DatasetType;
            Settings.Default.FilldownSeparationType = SeparationType;
            Settings.Default.FilldownColumn = ColumnName;
            Settings.Default.FilldownInstrument = InstrumentName;
            Settings.Default.FilldownCart = DmsData.CartName;
            Settings.Default.FilldownCartConfig = DmsData.CartConfigName;
            Settings.Default.FilldownInterest = InterestRating;
            Settings.Default.FilldownEMSLUsageType = DmsData.EMSLUsageType;
            Settings.Default.FilldownEMSLProposal = DmsData.EMSLProposalID;
            Settings.Default.FilldownExperimentName = DmsData.Experiment;

            var selectedEmslUsers = new StringCollection();
            foreach (var user in EMSLProposalUsers)
                selectedEmslUsers.Add(user.UserID.ToString());

            Settings.Default.FilldownEMSLUsers = selectedEmslUsers;

            return true;
        }

        public void LoadSettings()
        {
            DmsData.CommentAddition = Settings.Default.FilldownComment;
            Operator = Settings.Default.FilldownOperator;
            SeparationType = Settings.Default.FilldownSeparationType;
            ColumnName = Settings.Default.FilldownColumn;
            InstrumentName = Settings.Default.FilldownInstrument;
            DmsData.CartName = Settings.Default.FilldownCart;
            DmsData.CartConfigName = Settings.Default.FilldownCartConfig;
            InterestRating = Settings.Default.FilldownInterest;
            DmsData.Experiment = Settings.Default.FilldownExperimentName;

            DmsData.EMSLUsageType = Settings.Default.FilldownEMSLUsageType;
            DmsData.EMSLProposalID = Settings.Default.FilldownEMSLProposal;
            DmsData.DatasetType = Settings.Default.FilldownDatasetType;

            List<string> selectedUsers;
            if (Settings.Default.FilldownEMSLUsers == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.FilldownEMSLUsers.Cast<string>().ToList();

            using (EMSLProposalUsers.SuppressChangeNotifications())
            {
                EMSLProposalUsers.Clear();
                EMSLProposalUsers.AddRange(DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(DmsData.EMSLProposalID, selectedUsers));
            }
        }
    }
}
