﻿using System.ComponentModel;
using System.IO;
using LcmsNetData;
using LcmsNetData.Data;

namespace BuzzardWPF.Data
{
    /// <summary>
    /// Dataset information supplied by or required by DMS; includes run request information
    /// </summary>
    public class DMSData : IDmsData, INotifyPropertyChangedExt
    {
        public DMSData()
        {
            LockData = false;
            CartName = "";
            CartConfigName = "";
            Comment = "";
            CommentAddition = "";
            DatasetName = "";
            DatasetType = "";
            Experiment = "";
            EMSLProposalID = "";
            RequestID = 0;
            RequestName = "";
            EMSLUsageType = "";
            EMSLProposalUser = "";
            WorkPackage = "";
        }

        /// <summary>
        /// Unlock the object and reset all properties to default values.
        /// </summary>
        public void Reset()
        {
            LockData = false;
            CartName = "";
            CartConfigName = "";
            Comment = "";
            CommentAddition = "";
            DatasetName = "";
            DatasetType = "";
            Experiment = "";
            EMSLProposalID = "";
            RequestID = 0;
            RequestName = "";
            EMSLUsageType = "";
            EMSLProposalUser = "";
            WorkPackage = "";
        }

        public void CopyValuesAndLock(DMSData other)
        {
            LockData = false;
            CartName = other.CartName;
            CartConfigName = other.CartConfigName;
            Comment = other.Comment;
            DatasetName = other.DatasetName;
            DatasetType = other.DatasetType;
            Experiment = other.Experiment;
            EMSLProposalID = other.EMSLProposalID;
            RequestID = other.RequestID;
            RequestName = other.RequestName;
            EMSLUsageType = other.EMSLUsageType;
            EMSLProposalUser = other.EMSLProposalUser;
            WorkPackage = other.WorkPackage;
            LockData = other.LockData;
        }

        /// <summary>
        /// Copies the data and locks this object, and sets the dataset name to the filename in <paramref name="filePath"/>
        /// Used in Buzzard
        /// </summary>
        /// <param name="other"></param>
        /// <param name="filePath"></param>
        public void CopyValuesAndLockWithNewPath(DMSData other, string filePath)
        {
            CopyValuesAndLock(other);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                LockData = false;
                DatasetName = fileName;
                LockData = true;
            }
        }

        private int requestId;
        private string requestName;
        private string datasetName;
        private string datasetType;
        private string cartConfigName;
        private string workPackage;
        private string emslUsageType;
        private string emslProposalId;
        private string emslProposalUser;
        private string experiment;
        private bool lockData = false;
        private string cartName;
        private string comment;
        private string commentAddition;

        /// <summary>
        /// When the data comes from DMS, it will be locked. This is meant to stop the user
        /// from altering it. (this is not used in LCMSNet; it is used in Buzzard)
        /// </summary>
        public bool LockData
        {
            get => lockData;
            private set => this.RaiseAndSetIfChanged(ref lockData, value, nameof(LockData));
        }

        /// <summary>
        /// Name of request in DMS. Becomes sample name in LCMS and forms part
        /// of dataset name sample after run
        /// </summary>
        public string RequestName
        {
            get => requestName;
            set
            {
                if (this.RaiseAndSetIfChangedLockCheckRetBool(ref requestName, value, LockData, nameof(RequestName)))
                {
                    if (string.IsNullOrWhiteSpace(DatasetName))
                    {
                        DatasetName = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the dataset after editing the request name.
        /// </summary>
        public string DatasetName
        {
            get => datasetName;
            set => this.RaiseAndSetIfChangedLockCheck(ref datasetName, value, LockData, nameof(DatasetName));
        }

        /// <summary>
        /// Numeric ID of request in DMS
        /// </summary>
        public int RequestID
        {
            get => requestId;
            set => this.RaiseAndSetIfChangedLockCheck(ref requestId, value, LockData, nameof(RequestID));
        }

        /// <summary>
        /// Experiment name
        /// </summary>
        public string Experiment
        {
            get => experiment;
            set => this.RaiseAndSetIfChangedLockCheck(ref experiment, value, LockData, nameof(Experiment));
        }

        /// <summary>
        /// Dataset type (ie, HMS-MSn, HMS, etc)
        /// </summary>
        public string DatasetType
        {
            get => datasetType;
            set => this.RaiseAndSetIfChangedLockCheck(ref datasetType, value, LockData, nameof(DatasetType));
        }

        /// <summary>
        /// Work Package/charge code
        /// </summary>
        public string WorkPackage
        {
            get => workPackage;
            set => this.RaiseAndSetIfChangedLockCheck(ref workPackage, value, LockData, nameof(WorkPackage));
        }

        /// <summary>
        /// EMSL usage type
        /// </summary>
        public string EMSLUsageType
        {
            get => emslUsageType;
            set => this.RaiseAndSetIfChangedLockCheck(ref emslUsageType, value, LockData, nameof(EMSLUsageType));
        }

        /// <summary>
        /// EUS user proposal ID
        /// </summary>
        public string EMSLProposalID
        {
            get => emslProposalId;
            set => this.RaiseAndSetIfChangedLockCheck(ref emslProposalId, value, LockData, nameof(EMSLProposalID));
        }

        /// <summary>
        /// EUS user list
        /// </summary>
        public string EMSLProposalUser
        {
            get => emslProposalUser;
            set => this.RaiseAndSetIfChangedLockCheck(ref emslProposalUser, value, LockData, nameof(EMSLProposalUser));
        }

        /// <summary>
        /// Name of cart used for sample run
        /// </summary>
        /// <remarks>This is an editable field even if the DMS Request has been resolved.</remarks>
        public string CartName
        {
            get => cartName;
            set => this.RaiseAndSetIfChanged(ref cartName, value, nameof(CartName));
        }

        /// <summary>
        /// Name of cart configuration for the current cart
        /// </summary>
        /// <remarks>This is an editable field even if the DMS Request has been resolved.</remarks>
        public string CartConfigName
        {
            get => cartConfigName;
            set => this.RaiseAndSetIfChanged(ref cartConfigName, value, nameof(CartConfigName));
        }

        /// <summary>
        /// Comment field - includes additions from Buzzard (added by the accessor, not stored in the backing field)
        /// </summary>
        public string Comment
        {
            get => $"{comment} {(string.IsNullOrWhiteSpace(CommentAdditionPrefix) ? string.Empty : CommentAdditionPrefix.Trim() + " ")}{CommentAddition}".Trim();
            set => this.RaiseAndSetIfChangedLockCheck(ref comment, value, LockData, nameof(Comment));
        }

        /// <summary>
        /// Additional comment. Used by Buzzard to add comment information to datasets matched to run requests.
        /// </summary>
        public string CommentAddition
        {
            get => commentAddition;
            set => this.RaiseAndSetIfChanged(ref commentAddition, value);
        }

        /// <summary>
        /// Additional comment prefix. Used by Buzzard, output before CommentAddition.
        /// </summary>
        public string CommentAdditionPrefix { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(DatasetName))
            {
                if (string.Equals(RequestName, DatasetName))
                {
                    return "Request " + RequestName;
                }

                return "Dataset " + DatasetName;
            }

            if (!string.IsNullOrWhiteSpace(Experiment))
                return "Experiment " + Experiment;

            if (!string.IsNullOrWhiteSpace(RequestName))
                return "Request " + RequestName;

            return "RequestID " + RequestID;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
