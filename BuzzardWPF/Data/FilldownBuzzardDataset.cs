using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.Data
{
    public class FilldownBuzzardDataset : BuzzardDataset, IStoredSettingsMonitor
    {
        #region Attributes
        private bool m_shouldUseOperator;
        private bool m_shouldUseDatasetType;
        private bool m_shouldUseSeperationType;
        private bool m_shouldUseInstrumentType;

        private bool m_shouldUseCart;
        private bool m_shouldUseEmslProposalID;
        private bool m_shouldUseEmslUsageType;
        private bool m_shouldUseExperimentName;

        private bool m_shouldUseLCColumn;
        private bool m_shouldUseComment;
        private bool m_shouldUseInterestRating;
        private bool m_shouldUseEMSLProposalUsers;
        #endregion

        #region Initialize
        public FilldownBuzzardDataset()
        {
            ShouldUseCart = true;
            ShouldUseDatasetType = true;
            ShouldUseEMSLProposalID = true;
            ShouldUseEMSLUsageType = true;

            ShouldUseInstrumentType = true;
            ShouldUseOperator = true;
            ShouldUseSeparationType = true;
            ShouldUseExperimentName = true;

            ShouldUseLCColumn = true;
            ShouldUseInterestRating = true;
            ShouldUseEMSLProposalUsers = true;
            ShouldUseComment = true;

            DmsData = new DMSData();

            // Monitors for propertyChanged events
            this.WhenAnyValue(x => x.DmsData, x => x.DmsData.DatasetType, x => x.DmsData.EMSLUsageType, x => x.DmsData.EMSLProposalID)
                .Subscribe(x => SettingsChanged = true);
            this.WhenAnyValue(x => x.Comment, x => x.Operator, x => x.SeparationType, x => x.ColumnName, x => x.InstrumentName)
                .Subscribe(x => SettingsChanged = true);
            this.WhenAnyValue(x => x.DmsData.CartName, x => x.DmsData.CartConfigName, x => x.InterestRating, x => x.DmsData.Experiment)
                .Subscribe(x => SettingsChanged = true);

            LoadSettings();
        }
        #endregion

        #region Properties
        public bool ShouldUseLCColumn
        {
            get => m_shouldUseLCColumn;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseLCColumn, value);
        }

        public bool ShouldUseExperimentName
        {
            get => m_shouldUseExperimentName;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseExperimentName, value);
        }

        public bool ShouldUseComment
        {
            get => m_shouldUseComment;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseComment, value);
        }

        public bool ShouldUseOperator
        {
            get => m_shouldUseOperator;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseOperator, value);
        }

        public bool ShouldUseDatasetType
        {
            get => m_shouldUseDatasetType;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseDatasetType, value);
        }

        public bool ShouldUseSeparationType
        {
            get => m_shouldUseSeperationType;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseSeperationType, value);
        }

        public bool ShouldUseInstrumentType
        {
            get => m_shouldUseInstrumentType;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseInstrumentType, value);
        }

        public bool ShouldUseCart
        {
            get => m_shouldUseCart;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseCart, value);
        }

        public bool ShouldUseEMSLProposalID
        {
            get => m_shouldUseEmslProposalID;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseEmslProposalID, value);
        }

        public bool ShouldUseEMSLUsageType
        {
            get => m_shouldUseEmslUsageType;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseEmslUsageType, value);
        }

        public bool ShouldUseInterestRating
        {
            get => m_shouldUseInterestRating;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseInterestRating, value);
        }

        public bool ShouldUseEMSLProposalUsers
        {
            get => m_shouldUseEMSLProposalUsers;
            set => this.RaiseAndSetIfChanged(ref m_shouldUseEMSLProposalUsers, value);
        }

        public bool SettingsChanged { get; set; }

        #endregion

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.FilldownComment = Comment;
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
            if (DmsData == null)
            {
                DmsData = new DMSData();
            }

            Comment = Settings.Default.FilldownComment;
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
