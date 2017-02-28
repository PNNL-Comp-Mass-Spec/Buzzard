namespace BuzzardLib.Data
{
    public class FilldownBuzzardDataset
        : BuzzardDataset
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
            get { return m_shouldUseLCColumn; }
            set
            {
                if (m_shouldUseLCColumn != value)
                {
                    m_shouldUseLCColumn = value;
                    OnPropertyChanged("ShouldUseLCColumn");
                }
            }
        }

        public bool ShouldUseExperimentName
        {
            get { return m_shouldUseExperimentName; }
            set
            {
                if (m_shouldUseExperimentName != value)
                {
                    m_shouldUseExperimentName = value;
                    OnPropertyChanged("ShouldUseExperimentName");
                }
            }
        }

        public bool ShouldUseComment
        {
            get
            {
                return m_shouldUseComment;
            }
            set
            {
                if (m_shouldUseComment != value)
                {
                    m_shouldUseComment = value;
                    OnPropertyChanged("ShouldUseComment");
                }
            }
        }

        public bool ShouldUseOperator
        {
            get { return m_shouldUseOperator; }
            set
            {
                if (m_shouldUseOperator != value)
                {
                    m_shouldUseOperator = value;
                    OnPropertyChanged("ShouldUseOperator");
                }
            }
        }

        public bool ShouldUseDatasetType
        {
            get { return m_shouldUseDatasetType; }
            set
            {
                if (m_shouldUseDatasetType != value)
                {
                    m_shouldUseDatasetType = value;
                    OnPropertyChanged("ShouldUseDatasetType");
                }
            }
        }

        public bool ShouldUseSeparationType
        {
            get { return m_shouldUseSeperationType; }
            set
            {
                if (m_shouldUseSeperationType != value)
                {
                    m_shouldUseSeperationType = value;
                    OnPropertyChanged("ShouldUseSeparationType");
                }
            }
        }

        public bool ShouldUseInstrumentType
        {
            get { return m_shouldUseInstrumentType; }
            set
            {
                if (m_shouldUseInstrumentType != value)
                {
                    m_shouldUseInstrumentType = value;
                    OnPropertyChanged("ShouldUseInstrumentType");
                }
            }
        }

        public bool ShouldUseCart
        {
            get { return m_shouldUseCart; }
            set
            {
                if (m_shouldUseCart != value)
                {
                    m_shouldUseCart = value;
                    OnPropertyChanged("ShouldUseCart");
                }
            }
        }

        public bool ShouldUseEMSLProposalID
        {
            get { return m_shouldUseEmslProposalID; }
            set
            {
                if (m_shouldUseEmslProposalID != value)
                {
                    m_shouldUseEmslProposalID = value;
                    OnPropertyChanged("ShouldUseEMSLProposalID");
                }
            }
        }

        public bool ShouldUseEMSLUsageType
        {
            get { return m_shouldUseEmslUsageType; }
            set
            {
                if (m_shouldUseEmslUsageType != value)
                {
                    m_shouldUseEmslUsageType = value;
                    OnPropertyChanged("ShouldUseEMSLUsageType");
                }
            }
        }

        public bool ShouldUseInterestRating
        {
            get { return m_shouldUseInterestRating; }
            set
            {
                if (m_shouldUseInterestRating != value)
                {
                    m_shouldUseInterestRating = value;
                    OnPropertyChanged("ShouldUseInterestRating");
                }
            }
        }
        private bool m_shouldUseInterestRating;

        public bool ShouldUseEMSLProposalUsers
        {
            get { return m_shouldUseEMSLProposalUsers; }
            set
            {
                if (m_shouldUseEMSLProposalUsers != value)
                {
                    m_shouldUseEMSLProposalUsers = value;
                    OnPropertyChanged("ShouldUseEMSLProposalUsers");
                }
            }
        }
        private bool m_shouldUseEMSLProposalUsers;
        #endregion
    }
}
