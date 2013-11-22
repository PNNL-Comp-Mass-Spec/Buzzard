namespace Buzzard
{
    partial class DatasetControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mlabel_directoryPath = new System.Windows.Forms.Label();
            this.mlabel_status = new System.Windows.Forms.Label();
            this.mlabel_lastwrite = new System.Windows.Forms.Label();
            this.mlabel_timeUntilTrigger = new System.Windows.Forms.Label();
            this.mbutton_ignore = new System.Windows.Forms.Button();
            this.mprogressBar_progress = new System.Windows.Forms.ProgressBar();
            this.mlabel_dmsIndicator = new System.Windows.Forms.Label();
            this.mlabel_requestName = new System.Windows.Forms.Label();
            this.mgroupbox_timer = new System.Windows.Forms.GroupBox();
            this.mtimer_writeTimer = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.mgroupbox_timer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // mlabel_directoryPath
            // 
            this.mlabel_directoryPath.AutoSize = true;
            this.mlabel_directoryPath.Location = new System.Drawing.Point(74, 69);
            this.mlabel_directoryPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mlabel_directoryPath.Name = "mlabel_directoryPath";
            this.mlabel_directoryPath.Size = new System.Drawing.Size(74, 20);
            this.mlabel_directoryPath.TabIndex = 0;
            this.mlabel_directoryPath.Text = "Full path:";
            // 
            // mlabel_status
            // 
            this.mlabel_status.AutoSize = true;
            this.mlabel_status.Location = new System.Drawing.Point(74, 11);
            this.mlabel_status.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mlabel_status.Name = "mlabel_status";
            this.mlabel_status.Size = new System.Drawing.Size(56, 20);
            this.mlabel_status.TabIndex = 2;
            this.mlabel_status.Text = "Status";
            // 
            // mlabel_lastwrite
            // 
            this.mlabel_lastwrite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.mlabel_lastwrite.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mlabel_lastwrite.Location = new System.Drawing.Point(142, 100);
            this.mlabel_lastwrite.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mlabel_lastwrite.Name = "mlabel_lastwrite";
            this.mlabel_lastwrite.Size = new System.Drawing.Size(434, 25);
            this.mlabel_lastwrite.TabIndex = 3;
            this.mlabel_lastwrite.Text = "Last Write";
            this.mlabel_lastwrite.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // mlabel_timeUntilTrigger
            // 
            this.mlabel_timeUntilTrigger.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.mlabel_timeUntilTrigger.Location = new System.Drawing.Point(306, 35);
            this.mlabel_timeUntilTrigger.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mlabel_timeUntilTrigger.Name = "mlabel_timeUntilTrigger";
            this.mlabel_timeUntilTrigger.Size = new System.Drawing.Size(247, 21);
            this.mlabel_timeUntilTrigger.TabIndex = 4;
            this.mlabel_timeUntilTrigger.Text = "Time Until Trigger File";
            this.mlabel_timeUntilTrigger.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // mbutton_ignore
            // 
            this.mbutton_ignore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.mbutton_ignore.Location = new System.Drawing.Point(476, 11);
            this.mbutton_ignore.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mbutton_ignore.Name = "mbutton_ignore";
            this.mbutton_ignore.Size = new System.Drawing.Size(92, 32);
            this.mbutton_ignore.TabIndex = 5;
            this.mbutton_ignore.Text = "Ignore";
            this.mbutton_ignore.UseVisualStyleBackColor = true;
            this.mbutton_ignore.Click += new System.EventHandler(this.mbutton_ignore_Click);
            // 
            // mprogressBar_progress
            // 
            this.mprogressBar_progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.mprogressBar_progress.Location = new System.Drawing.Point(17, 29);
            this.mprogressBar_progress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mprogressBar_progress.Name = "mprogressBar_progress";
            this.mprogressBar_progress.Size = new System.Drawing.Size(281, 32);
            this.mprogressBar_progress.TabIndex = 6;
            // 
            // mlabel_dmsIndicator
            // 
            this.mlabel_dmsIndicator.BackColor = System.Drawing.Color.Maroon;
            this.mlabel_dmsIndicator.Location = new System.Drawing.Point(13, 100);
            this.mlabel_dmsIndicator.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mlabel_dmsIndicator.Name = "mlabel_dmsIndicator";
            this.mlabel_dmsIndicator.Size = new System.Drawing.Size(28, 12);
            this.mlabel_dmsIndicator.TabIndex = 7;
            // 
            // mlabel_requestName
            // 
            this.mlabel_requestName.AutoSize = true;
            this.mlabel_requestName.Location = new System.Drawing.Point(74, 40);
            this.mlabel_requestName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mlabel_requestName.Name = "mlabel_requestName";
            this.mlabel_requestName.Size = new System.Drawing.Size(74, 20);
            this.mlabel_requestName.TabIndex = 8;
            this.mlabel_requestName.Text = "Request:";
            // 
            // mgroupbox_timer
            // 
            this.mgroupbox_timer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.mgroupbox_timer.Controls.Add(this.mprogressBar_progress);
            this.mgroupbox_timer.Controls.Add(this.mlabel_timeUntilTrigger);
            this.mgroupbox_timer.Location = new System.Drawing.Point(15, 136);
            this.mgroupbox_timer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mgroupbox_timer.Name = "mgroupbox_timer";
            this.mgroupbox_timer.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mgroupbox_timer.Size = new System.Drawing.Size(561, 72);
            this.mgroupbox_timer.TabIndex = 9;
            this.mgroupbox_timer.TabStop = false;
            this.mgroupbox_timer.Text = "Trigger File Creation";
            // 
            // mtimer_writeTimer
            // 
            this.mtimer_writeTimer.Interval = 2000;
            this.mtimer_writeTimer.Tick += new System.EventHandler(this.mtimer_writeTimer_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(44, 100);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Resolved in DMS";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Buzzard.Properties.Resources.single;
            this.pictureBox1.Location = new System.Drawing.Point(15, 5);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(51, 78);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            // 
            // DatasetControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.mlabel_dmsIndicator);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.mbutton_ignore);
            this.Controls.Add(this.mlabel_lastwrite);
            this.Controls.Add(this.mgroupbox_timer);
            this.Controls.Add(this.mlabel_status);
            this.Controls.Add(this.mlabel_requestName);
            this.Controls.Add(this.mlabel_directoryPath);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "DatasetControl";
            this.Size = new System.Drawing.Size(580, 215);
            this.mgroupbox_timer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label mlabel_directoryPath;
        private System.Windows.Forms.Label mlabel_status;
        private System.Windows.Forms.Label mlabel_lastwrite;
        private System.Windows.Forms.Label mlabel_timeUntilTrigger;
        private System.Windows.Forms.Button mbutton_ignore;
        private System.Windows.Forms.ProgressBar mprogressBar_progress;
        private System.Windows.Forms.Label mlabel_dmsIndicator;
        private System.Windows.Forms.Label mlabel_requestName;
        private System.Windows.Forms.GroupBox mgroupbox_timer;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer mtimer_writeTimer;
        private System.Windows.Forms.Label label1;
    }
}
