using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
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
            CartName = string.Empty;
            CartConfigName = string.Empty;
            Comment = string.Empty;
            CommentAddition = string.Empty;
            DatasetName = string.Empty;
            DatasetType = string.Empty;
            Experiment = string.Empty;
            EMSLProposalID = string.Empty;
            RequestID = 0;
            InstrumentGroup = string.Empty;
            RequestName = string.Empty;
            EMSLUsageType = string.Empty;
            EMSLProposalUser = string.Empty;
            WorkPackage = string.Empty;
        }

        /// <summary>
        /// Unlock the object and reset all properties to default values.
        /// </summary>
        public void Reset()
        {
            LockData = false;
            CartName = string.Empty;
            CartConfigName = string.Empty;
            Comment = string.Empty;
            CommentAddition = string.Empty;
            DatasetName = string.Empty;
            DatasetType = string.Empty;
            Experiment = string.Empty;
            EMSLProposalID = string.Empty;
            RequestID = 0;
            InstrumentGroup = string.Empty;
            RequestName = string.Empty;
            EMSLUsageType = string.Empty;
            EMSLProposalUser = string.Empty;
            WorkPackage = string.Empty;
        }

        /// <summary>
        /// Copies the data and locks this object, and sets the dataset name to the filename in <paramref name="filePath"/>
        /// Used in Buzzard
        /// </summary>
        /// <param name="other"></param>
        /// <param name="filePath"></param>
        public void CopyValuesAndLockWithNewPath(DMSData other, string filePath)
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
            InstrumentGroup = other.InstrumentGroup;
            RequestName = other.RequestName;
            EMSLUsageType = other.EMSLUsageType;
            EMSLProposalUser = other.EMSLProposalUser;
            WorkPackage = other.WorkPackage;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                LockData = other.LockData;
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                DatasetName = fileName;
                LockData = true;
            }
            else
            {
                LockData = other.LockData;
            }
        }

        private int requestId;
        private string instrumentGroup;
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
        /// Instrument group specified in DMS for the request (only used when <see cref="RequestID"/> &gt; 0)
        /// </summary>
        public string InstrumentGroup
        {
            get => instrumentGroup;
            set => this.RaiseAndSetIfChangedLockCheck(ref instrumentGroup, value, LockData, nameof(InstrumentGroup));
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

    public static class DmsPropertyChangedExtensions
    {
        /// <summary>
        /// If isLocked is false and the newValue is not equal to the backingField value (using default EqualityComparer), sets backingField and raises OnPropertyChanged
        /// </summary>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="obj"></param>
        /// <param name="backingField"></param>
        /// <param name="newValue"></param>
        /// <param name="isLocked"></param>
        /// <param name="propertyName"></param>
        /// <returns>final value of backingField</returns>
        public static TRet RaiseAndSetIfChangedLockCheck<TRet>(this INotifyPropertyChangedExt obj,
            ref TRet backingField, TRet newValue, bool isLocked, [CallerMemberName] string propertyName = null)
        {
            if (isLocked)
            {
                obj.OnPropertyChanged(propertyName);
                return backingField;
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return newValue;
            }

            backingField = newValue;
            obj.OnPropertyChanged(propertyName);
            return newValue;
        }

        /// <summary>
        /// If isLocked is false and the newValue is not equal to the backingField value (using default EqualityComparer), sets backingField and raises OnPropertyChanged
        /// </summary>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="obj"></param>
        /// <param name="backingField"></param>
        /// <param name="newValue"></param>
        /// <param name="isLocked"></param>
        /// <param name="propertyName"></param>
        /// <returns>true if changed, false if not</returns>
        public static bool RaiseAndSetIfChangedLockCheckRetBool<TRet>(this INotifyPropertyChangedExt obj,
            ref TRet backingField, TRet newValue, bool isLocked, [CallerMemberName] string propertyName = null)
        {
            if (isLocked)
            {
                obj.OnPropertyChanged(propertyName);
                return false;
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return false;
            }

            backingField = newValue;
            obj.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
