using ReactiveUI;

namespace BuzzardWPF.Data
{
    public class FilldownBuzzardDataset : BuzzardDataset
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
        #endregion
    }
}
