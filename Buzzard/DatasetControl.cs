using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Buzzard.Data;

namespace Buzzard
{
    /// <summary>
    /// Control for displaying dataset data.
    /// </summary>
    public partial class DatasetControl : UserControl
    {
        #region Events
        /// <summary>
        /// Fired when the dataset is resolved in DMS.
        /// </summary>
        public event EventHandler ResolvedInDMSHover;
        /// <summary>
        /// Fired when the dataset is not resolved in DMS.
        /// </summary>
        public event EventHandler NotResolvedInDMSHover;
        /// <summary>
        /// Fired when the indicator is not hovered about.
        /// </summary>
        public event EventHandler NoIndicatorHover;
        #endregion

        private bool mbool_complete = false;

        #region Construtors
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dataset">Dataset to monitor</param>
        public DatasetControl(classDataset dataset)
        {
            InitializeComponent();
            
            Dataset                   = dataset;

            if (dataset != null)
            {
                dataset.StatusChanged    += new EventHandler(dataset_StatusChanged);
                dataset.IgnoreChanged    += new EventHandler(dataset_IgnoreChanged);
                dataset.LastWriteChanged += new EventHandler(dataset_LastWriteChanged);
                dataset.DMSResolved      += new EventHandler(dataset_DMSResolved);


                if (dataset.DMSData != null)
                {
                    mlabel_requestName.Text = dataset.DMSData.RequestName;
                }
                mlabel_directoryPath.Text    = "Path: " + dataset.DatasetPath;                
                UpdateStatus(Dataset.DatasetStatus);
                UpdateWriteSpan();
            }

            mlabel_dmsIndicator.MouseEnter += new EventHandler(mlabel_dmsIndicator_MouseEnter);
            mlabel_dmsIndicator.MouseLeave += new EventHandler(mlabel_dmsIndicator_MouseLeave);
        }
        /// <summary>
        /// Parameterless Constructor
        /// </summary>
        public DatasetControl(): 
                            this(null)
        {
        }        
        #endregion

        #region Tool Tip Crap!
        /// <summary>
        /// Hides the tool tip window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mlabel_dmsIndicator_MouseLeave(object sender, EventArgs e)
        {
            if (NoIndicatorHover != null)
            {
                NoIndicatorHover(this, null);
            }
        }
        /// <summary>
        /// Creates the tool tip window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mlabel_dmsIndicator_MouseEnter(object sender, EventArgs e)
        {
            bool isResolved = (Dataset.DMSData != null);
            if (isResolved)
            {
                if (ResolvedInDMSHover != null)
                {
                    ResolvedInDMSHover(this, null);
                }
            }
            else
            {
                if (NotResolvedInDMSHover != null)
                {
                    NotResolvedInDMSHover(this, null);
                }
            }           
        }
        #endregion

        public void CompleteDataset()
        {
            mgroupbox_timer.Hide();
            if (Controls.Contains(mgroupbox_timer))
            {
                Controls.Remove(mgroupbox_timer);
                Height -= mgroupbox_timer.Height;                
            }
        }

        #region Dataset Event Handlers
        /// <summary>
        /// Handles changing the indicator if the DMS data has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataset_DMSResolved(object sender, EventArgs e)
        {
            if (Dataset.DMSData != null)
            {
                mlabel_dmsIndicator.BackColor   = Color.Lime;
                mlabel_requestName.Visible      = true;                
                mlabel_requestName.Text         = "Request: " + Dataset.DMSData.RequestName;
            }
            else
            {
                mlabel_dmsIndicator.BackColor   = Color.Maroon;
                mlabel_requestName.Visible      = false;
            }
        }
        /// <summary>
        /// Handles changing the text of the button when the dataset ignore status is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataset_IgnoreChanged(object sender, EventArgs e)
        {
            if (Dataset.ShouldIgnore)
            {
                mbutton_ignore.Text = "Enable";
            }
            else
            {
                mbutton_ignore.Text = "Ignore";
            }
            UpdateStatus(Dataset.DatasetStatus);
        }
        /// <summary>
        /// Handles when the dataset status changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataset_StatusChanged(object sender, EventArgs e)
        {
            UpdateStatus(Dataset.DatasetStatus);
        }
        private void UpdateStatus(DatasetStatus status)
        {
            switch(status)
            {
                case DatasetStatus.Ignored:
                    mlabel_status.Text = "Ignored. Trigger file will not be made unless enabled.";
                    break;
                case DatasetStatus.FailedFileError:
                    mlabel_status.Text = "Failed to create a trigger file.";
                    break;
                case DatasetStatus.FailedNoDMSRequest:
                    mlabel_status.Text = "Failed to create a trigger file.";
                    break;
                case DatasetStatus.FailedUnknown:
                    mlabel_status.Text = "Failed to create a trigger file.";
                    break;
                case DatasetStatus.Pending:
                    if (Dataset.ShouldIgnore)
                    {
                        mlabel_status.Text = "Ignored but pending...";
                    }
                    else
                    {
                        mlabel_status.Text = "Waiting to create trigger file...";
                    }
                    break;
                case DatasetStatus.TriggerCreated:
                    mlabel_status.Text = "Trigger file was made, but awaiting to be copied to DMS.";
                    break;
                case DatasetStatus.TriggerFileSent:
                    mlabel_status.Text      = "Trigger file sent.";
                    mbutton_ignore.Visible  = false;
                    break;
            }
        }
        /// <summary>
        /// Updates the last write label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void  dataset_LastWriteChanged(object sender, EventArgs e)
        {            
            mlabel_lastwrite.Text       = "Last write: " + Dataset.LastWrite.ToString(); 
            mtimer_writeTimer.Enabled   = false;                       
            mlabel_lastwrite.ForeColor  = Color.Red;
            mtimer_writeTimer.Enabled   = true;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the dataset data bound to this control.
        /// </summary>
        public classDataset Dataset
        {
            private set;
            get;
        }
        #endregion
        
        #region Methods
        /// <summary>
        /// Updates the labels for write spans.
        /// </summary>
        public void UpdateWriteSpan()
        {
            DateTime endTime    = Dataset.LastWrite.Add(Dataset.Duration);
            TimeSpan span       = endTime.Subtract(DateTime.Now);
            int totalSeconds    = Convert.ToInt32(span.TotalSeconds);

            if (totalSeconds > 0)
            {
                mprogressBar_progress.Maximum   = Convert.ToInt32(Dataset.Duration.TotalSeconds);
                int seconds = totalSeconds % 60;
                int minutes = (totalSeconds - seconds) / 60;
                mlabel_timeUntilTrigger.Text    = string.Format("{0} minutes {1} seconds until trigger file.", minutes, seconds);
                mprogressBar_progress.Value     = Convert.ToInt32(Dataset.Duration.TotalSeconds) - totalSeconds;
                mprogressBar_progress.Visible   = true;
                mlabel_timeUntilTrigger.Visible = true;
                mlabel_lastwrite.Text           = "Last write: " + Dataset.LastWrite.ToString();                
            }
            else
            {
                mprogressBar_progress.Visible   = false;
                mlabel_timeUntilTrigger.Visible = false;
            }
        }
        #endregion

        #region Form Event Handlers
        /// <summary>
        /// Disables or enables the button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mbutton_ignore_Click(object sender, EventArgs e)
        {
            Dataset.ShouldIgnore = (Dataset.ShouldIgnore == false);
        }
        #endregion

        private void mtimer_writeTimer_Tick(object sender, EventArgs e)
        {
            mlabel_lastwrite.ForeColor = Color.Black;
        }

    }
}
