using System;
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

            // Monitors for propertyChanged events
            this.WhenAnyValue(x => x.DmsData, x => x.DmsData.DatasetType, x => x.DmsData.EMSLUsageType, x => x.DmsData.EMSLProposalID)
                .Subscribe(x => SettingsChanged = true);
            this.WhenAnyValue(x => x.Comment, x => x.Operator, x => x.SeparationType, x => x.LCColumn, x => x.Instrument)
                .Subscribe(x => SettingsChanged = true);
            this.WhenAnyValue(x => x.CartName, x => x.CartConfigName, x => x.InterestRating, x => x.ExperimentName)
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
            Settings.Default.FilldownColumn = LCColumn;
            Settings.Default.FilldownInstrument = Instrument;
            Settings.Default.FilldownCart = CartName;
            Settings.Default.FilldownCartConfig = CartConfigName;
            Settings.Default.FilldownInterest = InterestRating;
            Settings.Default.FilldownEMSLUsageType = DmsData.EMSLUsageType;
            Settings.Default.FilldownEMSLProposal = DmsData.EMSLProposalID;
            Settings.Default.FilldownExperimentName = ExperimentName;

            return true;
        }

        public void LoadSettings()
        {
            Comment = Settings.Default.FilldownComment;
            Operator = Settings.Default.FilldownOperator;
            SeparationType = Settings.Default.FilldownSeparationType;
            LCColumn = Settings.Default.FilldownColumn;
            Instrument = Settings.Default.FilldownInstrument;
            CartName = Settings.Default.FilldownCart;
            CartConfigName = Settings.Default.FilldownCartConfig;
            InterestRating = Settings.Default.FilldownInterest;
            ExperimentName = Settings.Default.FilldownExperimentName;

            if (DmsData == null)
            {
                DmsData = new DMSData();
            }

            DmsData.EMSLUsageType = Settings.Default.FilldownEMSLUsageType;
            DmsData.EMSLProposalID = Settings.Default.FilldownEMSLProposal;
            DmsData.DatasetType = Settings.Default.FilldownDatasetType;
        }
    }
}
