using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LcmsNetDataClasses;

namespace Buzzard.Data
{
    /// <summary>
    /// Class that holds 
    /// </summary>
    public class classDataset
    {
        #region Events 
        /// <summary>
        /// Fired when the status changes.
        /// </summary>
        public event EventHandler StatusChanged;
        /// <summary>
        /// Fired when the dataset has been written to.
        /// </summary>
        public event EventHandler LastWriteChanged;
        /// <summary>
        /// Fired when the dataset was told to be ignored for creating a trigger file.
        /// </summary>
        public event EventHandler IgnoreChanged;
        /// <summary>
        /// Fired when a dataset has been resolved by DMS.
        /// </summary>
        public event EventHandler DMSResolved;
        #endregion

        #region Members
        /// <summary>
        /// Last time that the file was written to.
        /// </summary>
        private DateTime m_lastWrite;
        /// <summary>
        /// Status of the dataset.
        /// </summary>
        private DatasetStatus menum_status;
        /// <summary>
        /// Flag indicating whether a dataset should be ignored.
        /// </summary>
        private bool mbool_shouldIgnore;
        /// <summary>
        /// Object holding data about the dataset and request for a given sample.
        /// </summary>
        private classDMSData mobj_dmsData;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public classDataset()
        {
            Name            = null;
            DatasetPath     = null;
            LastWrite       = DateTime.MinValue;
            Duration        = TimeSpan.MinValue;
            DMSData         = null;
            TriggerPath     = null;
            ShouldIgnore    = false;
            DatasetStatus   = DatasetStatus.Pending;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the flag if the dataset should be ignored.
        /// </summary>
        public bool ShouldIgnore
        {
            get
            {
                return mbool_shouldIgnore;
            }
            set
            {
                if (mbool_shouldIgnore != value)
                {
                    mbool_shouldIgnore = value;
                    if (IgnoreChanged != null)
                    {
                        IgnoreChanged(this, null);
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets the name of the dataset
        /// </summary>
        public string Name
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the name of the dataset path.
        /// </summary>
        public string DatasetPath
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the time the file was last written
        /// </summary>
        public DateTime LastWrite
        {
            get
            {
                return m_lastWrite;
            }
            set
            {

                if (value.CompareTo(m_lastWrite) != 0)
                {
                    m_lastWrite = value;
                    if (this.LastWriteChanged != null)
                    {
                        this.LastWriteChanged(this, null);
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets the time until the trigger file should be made.
        /// </summary>
        public TimeSpan Duration
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the DMS data associated with this dataset
        /// </summary>
        public classDMSData DMSData
        {
            get
            {
                return mobj_dmsData;
            }
            set
            {
                if (mobj_dmsData != value)
                {
                    mobj_dmsData = value;
                    if (DMSResolved != null && value != null)
                    {
                        DMSResolved(this, null);
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets the trigger path where the file was created
        /// </summary>
        public string TriggerPath
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the status of the trigger file.
        /// </summary>
        public DatasetStatus DatasetStatus
        {
            get
            {
                return menum_status;
            }
            set
            {
                if (menum_status != value)
                {
                    menum_status = value;
                    if (StatusChanged != null)
                    {
                        StatusChanged(this, null);
                    }
                }
            }
        }
        #endregion
    }
}
