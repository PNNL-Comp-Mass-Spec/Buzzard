namespace Buzzard
{
    partial class mainBuzzard
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
            try
            {
                SaveSettings();
            }
            catch
            {
            }

            

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mainBuzzard));
			this.label9 = new System.Windows.Forms.Label();
			this.mlabel_synchedWithDMS = new System.Windows.Forms.Label();
			this.mwatcher_files = new System.IO.FileSystemWatcher();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.mlabel_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.mtimer_modifiedTimer = new System.Windows.Forms.Timer(this.components);
			this.mpictureBox_buzzards = new System.Windows.Forms.PictureBox();
			this.mtabPage_config = new System.Windows.Forms.TabPage();
			this.mbutton_reloadDMSInformation = new System.Windows.Forms.Button();
			this.mbutton_reloadDMS = new System.Windows.Forms.Button();
			this.mgroupBox_autoUploads = new System.Windows.Forms.GroupBox();
			this.mbutton_exploreDMSTrigger = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.mtextbox_triggerLocation = new System.Windows.Forms.TextBox();
			this.mcheckBox_copyTriggerFiles = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.mcombo_Operator = new System.Windows.Forms.ComboBox();
			this.mgroupBox_instrument = new System.Windows.Forms.GroupBox();
			this.comboBoxAvailInstruments = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.mgroupBox_cart = new System.Windows.Forms.GroupBox();
			this.mcombo_cartName = new System.Windows.Forms.ComboBox();
			this.mcomboBox_columnData = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.mlabel_cartName = new System.Windows.Forms.Label();
			this.mcombo_SepType = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.mtabPage_failedDatasets = new System.Windows.Forms.TabPage();
			this.mbutton_clearFailed = new System.Windows.Forms.Button();
			this.mpanel_failedDatasets = new System.Windows.Forms.Panel();
			this.mtabPage_completedDatasets = new System.Windows.Forms.TabPage();
			this.mbutton_clearFinished = new System.Windows.Forms.Button();
			this.mpanel_completedDatasets = new System.Windows.Forms.Panel();
			this.mtabPage_pendingDatasets = new System.Windows.Forms.TabPage();
			this.mpanel_pendingDatasets = new System.Windows.Forms.Panel();
			this.mtabpage_watcherConfig = new System.Windows.Forms.TabPage();
			this.mgroupBox_watcher = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.mnum_minutesToWait = new System.Windows.Forms.NumericUpDown();
			this.mbutton_explorer = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.mcheckBox_autoStartup = new System.Windows.Forms.CheckBox();
			this.mutton_browse = new System.Windows.Forms.Button();
			this.mcombo_searchType = new System.Windows.Forms.ComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.mtextBox_path = new System.Windows.Forms.TextBox();
			this.mtextBox_extension = new System.Windows.Forms.TextBox();
			this.mbutton_control = new System.Windows.Forms.Button();
			this.mtabPages = new System.Windows.Forms.TabControl();
			this.mtimer_reloadDMSData = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.mwatcher_files)).BeginInit();
			this.statusStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mpictureBox_buzzards)).BeginInit();
			this.mtabPage_config.SuspendLayout();
			this.mgroupBox_autoUploads.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.mgroupBox_instrument.SuspendLayout();
			this.mgroupBox_cart.SuspendLayout();
			this.mtabPage_failedDatasets.SuspendLayout();
			this.mtabPage_completedDatasets.SuspendLayout();
			this.mtabPage_pendingDatasets.SuspendLayout();
			this.mtabpage_watcherConfig.SuspendLayout();
			this.mgroupBox_watcher.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mnum_minutesToWait)).BeginInit();
			this.mtabPages.SuspendLayout();
			this.SuspendLayout();
			// 
			// label9
			// 
			this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.label9.AutoSize = true;
			this.label9.BackColor = System.Drawing.Color.Transparent;
			this.label9.Location = new System.Drawing.Point(556, 748);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(143, 13);
			this.label9.TabIndex = 42;
			this.label9.Text = "Datasets Synched with DMS";
			// 
			// mlabel_synchedWithDMS
			// 
			this.mlabel_synchedWithDMS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.mlabel_synchedWithDMS.AutoSize = true;
			this.mlabel_synchedWithDMS.BackColor = System.Drawing.Color.Maroon;
			this.mlabel_synchedWithDMS.Location = new System.Drawing.Point(705, 748);
			this.mlabel_synchedWithDMS.Name = "mlabel_synchedWithDMS";
			this.mlabel_synchedWithDMS.Size = new System.Drawing.Size(19, 13);
			this.mlabel_synchedWithDMS.TabIndex = 8;
			this.mlabel_synchedWithDMS.Text = "    ";
			// 
			// mwatcher_files
			// 
			this.mwatcher_files.EnableRaisingEvents = true;
			this.mwatcher_files.SynchronizingObject = this;
			// 
			// statusStrip1
			// 
			this.statusStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mlabel_status});
			this.statusStrip1.Location = new System.Drawing.Point(0, 774);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.statusStrip1.Size = new System.Drawing.Size(740, 22);
			this.statusStrip1.TabIndex = 2;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// mlabel_status
			// 
			this.mlabel_status.Name = "mlabel_status";
			this.mlabel_status.Size = new System.Drawing.Size(47, 17);
			this.mlabel_status.Text = "Ready.";
			// 
			// mtimer_modifiedTimer
			// 
			this.mtimer_modifiedTimer.Interval = 1000;
			// 
			// mpictureBox_buzzards
			// 
			this.mpictureBox_buzzards.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mpictureBox_buzzards.BackgroundImage = global::Buzzard.Properties.Resources.buzzards;
			this.mpictureBox_buzzards.Location = new System.Drawing.Point(14, 12);
			this.mpictureBox_buzzards.Name = "mpictureBox_buzzards";
			this.mpictureBox_buzzards.Size = new System.Drawing.Size(714, 135);
			this.mpictureBox_buzzards.TabIndex = 0;
			this.mpictureBox_buzzards.TabStop = false;
			// 
			// mtabPage_config
			// 
			this.mtabPage_config.Controls.Add(this.mbutton_reloadDMSInformation);
			this.mtabPage_config.Controls.Add(this.mbutton_reloadDMS);
			this.mtabPage_config.Controls.Add(this.mgroupBox_autoUploads);
			this.mtabPage_config.Controls.Add(this.groupBox1);
			this.mtabPage_config.Controls.Add(this.mgroupBox_instrument);
			this.mtabPage_config.Controls.Add(this.mgroupBox_cart);
			this.mtabPage_config.Location = new System.Drawing.Point(4, 29);
			this.mtabPage_config.Name = "mtabPage_config";
			this.mtabPage_config.Padding = new System.Windows.Forms.Padding(3);
			this.mtabPage_config.Size = new System.Drawing.Size(706, 559);
			this.mtabPage_config.TabIndex = 1;
			this.mtabPage_config.Text = "Instrument / DMS Config";
			this.mtabPage_config.UseVisualStyleBackColor = true;
			// 
			// mbutton_reloadDMSInformation
			// 
			this.mbutton_reloadDMSInformation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_reloadDMSInformation.Location = new System.Drawing.Point(231, 509);
			this.mbutton_reloadDMSInformation.Name = "mbutton_reloadDMSInformation";
			this.mbutton_reloadDMSInformation.Size = new System.Drawing.Size(163, 54);
			this.mbutton_reloadDMSInformation.TabIndex = 42;
			this.mbutton_reloadDMSInformation.Text = "Refresh DMS Instrument Data";
			this.mbutton_reloadDMSInformation.UseVisualStyleBackColor = true;
			this.mbutton_reloadDMSInformation.Click += new System.EventHandler(this.mbutton_reloadDMSInformation_Click);
			// 
			// mbutton_reloadDMS
			// 
			this.mbutton_reloadDMS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_reloadDMS.Location = new System.Drawing.Point(8, 509);
			this.mbutton_reloadDMS.Name = "mbutton_reloadDMS";
			this.mbutton_reloadDMS.Size = new System.Drawing.Size(217, 54);
			this.mbutton_reloadDMS.TabIndex = 34;
			this.mbutton_reloadDMS.Text = "Refresh Dataset Information from DMS";
			this.mbutton_reloadDMS.UseVisualStyleBackColor = true;
			this.mbutton_reloadDMS.Click += new System.EventHandler(this.mbutton_reloadDMS_Click);
			// 
			// mgroupBox_autoUploads
			// 
			this.mgroupBox_autoUploads.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mgroupBox_autoUploads.Controls.Add(this.mbutton_exploreDMSTrigger);
			this.mgroupBox_autoUploads.Controls.Add(this.label3);
			this.mgroupBox_autoUploads.Controls.Add(this.mtextbox_triggerLocation);
			this.mgroupBox_autoUploads.Controls.Add(this.mcheckBox_copyTriggerFiles);
			this.mgroupBox_autoUploads.Location = new System.Drawing.Point(4, 334);
			this.mgroupBox_autoUploads.Name = "mgroupBox_autoUploads";
			this.mgroupBox_autoUploads.Size = new System.Drawing.Size(659, 154);
			this.mgroupBox_autoUploads.TabIndex = 41;
			this.mgroupBox_autoUploads.TabStop = false;
			this.mgroupBox_autoUploads.Text = "Data Auto-Upload DMS";
			// 
			// mbutton_exploreDMSTrigger
			// 
			this.mbutton_exploreDMSTrigger.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_exploreDMSTrigger.Location = new System.Drawing.Point(575, 105);
			this.mbutton_exploreDMSTrigger.Name = "mbutton_exploreDMSTrigger";
			this.mbutton_exploreDMSTrigger.Size = new System.Drawing.Size(72, 31);
			this.mbutton_exploreDMSTrigger.TabIndex = 32;
			this.mbutton_exploreDMSTrigger.Text = "Explore";
			this.mbutton_exploreDMSTrigger.UseVisualStyleBackColor = true;
			this.mbutton_exploreDMSTrigger.Click += new System.EventHandler(this.mbutton_exploreDMSTrigger_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(2, 81);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(193, 20);
			this.label3.TabIndex = 7;
			this.label3.Text = "Trigger file upload location";
			// 
			// mtextbox_triggerLocation
			// 
			this.mtextbox_triggerLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mtextbox_triggerLocation.Enabled = false;
			this.mtextbox_triggerLocation.Location = new System.Drawing.Point(5, 109);
			this.mtextbox_triggerLocation.Name = "mtextbox_triggerLocation";
			this.mtextbox_triggerLocation.Size = new System.Drawing.Size(564, 26);
			this.mtextbox_triggerLocation.TabIndex = 6;
			// 
			// mcheckBox_copyTriggerFiles
			// 
			this.mcheckBox_copyTriggerFiles.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.mcheckBox_copyTriggerFiles.Location = new System.Drawing.Point(6, 41);
			this.mcheckBox_copyTriggerFiles.Name = "mcheckBox_copyTriggerFiles";
			this.mcheckBox_copyTriggerFiles.Size = new System.Drawing.Size(243, 24);
			this.mcheckBox_copyTriggerFiles.TabIndex = 1;
			this.mcheckBox_copyTriggerFiles.Text = "Copy Trigger Files To DMS";
			this.mcheckBox_copyTriggerFiles.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.mcombo_Operator);
			this.groupBox1.Location = new System.Drawing.Point(7, 265);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(659, 63);
			this.groupBox1.TabIndex = 40;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Operator";
			// 
			// mcombo_Operator
			// 
			this.mcombo_Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.mcombo_Operator.FormattingEnabled = true;
			this.mcombo_Operator.Location = new System.Drawing.Point(233, 25);
			this.mcombo_Operator.Name = "mcombo_Operator";
			this.mcombo_Operator.Size = new System.Drawing.Size(275, 28);
			this.mcombo_Operator.TabIndex = 33;
			this.mcombo_Operator.SelectedIndexChanged += new System.EventHandler(this.mcombo_Operator_SelectedIndexChanged);
			// 
			// mgroupBox_instrument
			// 
			this.mgroupBox_instrument.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mgroupBox_instrument.Controls.Add(this.comboBoxAvailInstruments);
			this.mgroupBox_instrument.Controls.Add(this.label2);
			this.mgroupBox_instrument.Location = new System.Drawing.Point(10, 28);
			this.mgroupBox_instrument.Name = "mgroupBox_instrument";
			this.mgroupBox_instrument.Size = new System.Drawing.Size(659, 87);
			this.mgroupBox_instrument.TabIndex = 39;
			this.mgroupBox_instrument.TabStop = false;
			this.mgroupBox_instrument.Text = "Mass Spectrometer Instrument";
			// 
			// comboBoxAvailInstruments
			// 
			this.comboBoxAvailInstruments.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAvailInstruments.FormattingEnabled = true;
			this.comboBoxAvailInstruments.Location = new System.Drawing.Point(230, 34);
			this.comboBoxAvailInstruments.Name = "comboBoxAvailInstruments";
			this.comboBoxAvailInstruments.Size = new System.Drawing.Size(275, 28);
			this.comboBoxAvailInstruments.TabIndex = 33;
			this.comboBoxAvailInstruments.SelectedIndexChanged += new System.EventHandler(this.comboBoxAvailInstruments_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(14, 30);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(161, 20);
			this.label2.TabIndex = 34;
			this.label2.Text = "Available Instruments";
			// 
			// mgroupBox_cart
			// 
			this.mgroupBox_cart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mgroupBox_cart.Controls.Add(this.mcombo_cartName);
			this.mgroupBox_cart.Controls.Add(this.mcomboBox_columnData);
			this.mgroupBox_cart.Controls.Add(this.label7);
			this.mgroupBox_cart.Controls.Add(this.mlabel_cartName);
			this.mgroupBox_cart.Controls.Add(this.mcombo_SepType);
			this.mgroupBox_cart.Controls.Add(this.label1);
			this.mgroupBox_cart.Location = new System.Drawing.Point(10, 121);
			this.mgroupBox_cart.Name = "mgroupBox_cart";
			this.mgroupBox_cart.Size = new System.Drawing.Size(659, 138);
			this.mgroupBox_cart.TabIndex = 38;
			this.mgroupBox_cart.TabStop = false;
			this.mgroupBox_cart.Text = "Separation Instrument ";
			// 
			// mcombo_cartName
			// 
			this.mcombo_cartName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.mcombo_cartName.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.mcombo_cartName.FormattingEnabled = true;
			this.mcombo_cartName.Location = new System.Drawing.Point(230, 26);
			this.mcombo_cartName.Name = "mcombo_cartName";
			this.mcombo_cartName.Size = new System.Drawing.Size(275, 28);
			this.mcombo_cartName.TabIndex = 41;
			this.mcombo_cartName.SelectedIndexChanged += new System.EventHandler(this.mcombo_cartName_SelectedIndexChanged);
			// 
			// mcomboBox_columnData
			// 
			this.mcomboBox_columnData.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.mcomboBox_columnData.FormattingEnabled = true;
			this.mcomboBox_columnData.Location = new System.Drawing.Point(230, 97);
			this.mcomboBox_columnData.Name = "mcomboBox_columnData";
			this.mcomboBox_columnData.Size = new System.Drawing.Size(275, 28);
			this.mcomboBox_columnData.TabIndex = 39;
			this.mcomboBox_columnData.SelectedIndexChanged += new System.EventHandler(this.mcomboBox_columnData_SelectedIndexChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(14, 100);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(102, 20);
			this.label7.TabIndex = 40;
			this.label7.Text = "Column Data";
			// 
			// mlabel_cartName
			// 
			this.mlabel_cartName.AutoSize = true;
			this.mlabel_cartName.Location = new System.Drawing.Point(14, 26);
			this.mlabel_cartName.Name = "mlabel_cartName";
			this.mlabel_cartName.Size = new System.Drawing.Size(85, 20);
			this.mlabel_cartName.TabIndex = 26;
			this.mlabel_cartName.Text = "Cart Name";
			// 
			// mcombo_SepType
			// 
			this.mcombo_SepType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.mcombo_SepType.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.mcombo_SepType.FormattingEnabled = true;
			this.mcombo_SepType.Location = new System.Drawing.Point(230, 63);
			this.mcombo_SepType.Name = "mcombo_SepType";
			this.mcombo_SepType.Size = new System.Drawing.Size(275, 28);
			this.mcombo_SepType.TabIndex = 31;
			this.mcombo_SepType.SelectedIndexChanged += new System.EventHandler(this.mcombo_SepType_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(14, 65);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(210, 20);
			this.label1.TabIndex = 30;
			this.label1.Text = "Separation/Acquisition Type:";
			// 
			// mtabPage_failedDatasets
			// 
			this.mtabPage_failedDatasets.Controls.Add(this.mbutton_clearFailed);
			this.mtabPage_failedDatasets.Controls.Add(this.mpanel_failedDatasets);
			this.mtabPage_failedDatasets.Location = new System.Drawing.Point(4, 29);
			this.mtabPage_failedDatasets.Name = "mtabPage_failedDatasets";
			this.mtabPage_failedDatasets.Size = new System.Drawing.Size(706, 537);
			this.mtabPage_failedDatasets.TabIndex = 4;
			this.mtabPage_failedDatasets.Text = "Failed";
			this.mtabPage_failedDatasets.UseVisualStyleBackColor = true;
			// 
			// mbutton_clearFailed
			// 
			this.mbutton_clearFailed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_clearFailed.Location = new System.Drawing.Point(586, 541);
			this.mbutton_clearFailed.Name = "mbutton_clearFailed";
			this.mbutton_clearFailed.Size = new System.Drawing.Size(95, 33);
			this.mbutton_clearFailed.TabIndex = 44;
			this.mbutton_clearFailed.Text = "Clear";
			this.mbutton_clearFailed.UseVisualStyleBackColor = true;
			// 
			// mpanel_failedDatasets
			// 
			this.mpanel_failedDatasets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mpanel_failedDatasets.AutoScroll = true;
			this.mpanel_failedDatasets.Location = new System.Drawing.Point(0, 0);
			this.mpanel_failedDatasets.Name = "mpanel_failedDatasets";
			this.mpanel_failedDatasets.Size = new System.Drawing.Size(681, 535);
			this.mpanel_failedDatasets.TabIndex = 30;
			// 
			// mtabPage_completedDatasets
			// 
			this.mtabPage_completedDatasets.Controls.Add(this.mbutton_clearFinished);
			this.mtabPage_completedDatasets.Controls.Add(this.mpanel_completedDatasets);
			this.mtabPage_completedDatasets.Location = new System.Drawing.Point(4, 29);
			this.mtabPage_completedDatasets.Name = "mtabPage_completedDatasets";
			this.mtabPage_completedDatasets.Size = new System.Drawing.Size(706, 537);
			this.mtabPage_completedDatasets.TabIndex = 2;
			this.mtabPage_completedDatasets.Text = "Completed";
			this.mtabPage_completedDatasets.UseVisualStyleBackColor = true;
			// 
			// mbutton_clearFinished
			// 
			this.mbutton_clearFinished.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_clearFinished.Location = new System.Drawing.Point(577, 541);
			this.mbutton_clearFinished.Name = "mbutton_clearFinished";
			this.mbutton_clearFinished.Size = new System.Drawing.Size(95, 33);
			this.mbutton_clearFinished.TabIndex = 43;
			this.mbutton_clearFinished.Text = "Clear";
			this.mbutton_clearFinished.UseVisualStyleBackColor = true;
			this.mbutton_clearFinished.Click += new System.EventHandler(this.mbutton_clearFinished_Click);
			// 
			// mpanel_completedDatasets
			// 
			this.mpanel_completedDatasets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mpanel_completedDatasets.AutoScroll = true;
			this.mpanel_completedDatasets.Location = new System.Drawing.Point(0, 0);
			this.mpanel_completedDatasets.Name = "mpanel_completedDatasets";
			this.mpanel_completedDatasets.Size = new System.Drawing.Size(681, 535);
			this.mpanel_completedDatasets.TabIndex = 30;
			// 
			// mtabPage_pendingDatasets
			// 
			this.mtabPage_pendingDatasets.Controls.Add(this.mpanel_pendingDatasets);
			this.mtabPage_pendingDatasets.Location = new System.Drawing.Point(4, 29);
			this.mtabPage_pendingDatasets.Name = "mtabPage_pendingDatasets";
			this.mtabPage_pendingDatasets.Size = new System.Drawing.Size(706, 537);
			this.mtabPage_pendingDatasets.TabIndex = 3;
			this.mtabPage_pendingDatasets.Text = "Pending";
			this.mtabPage_pendingDatasets.UseVisualStyleBackColor = true;
			// 
			// mpanel_pendingDatasets
			// 
			this.mpanel_pendingDatasets.AutoScroll = true;
			this.mpanel_pendingDatasets.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mpanel_pendingDatasets.Location = new System.Drawing.Point(0, 0);
			this.mpanel_pendingDatasets.Name = "mpanel_pendingDatasets";
			this.mpanel_pendingDatasets.Size = new System.Drawing.Size(706, 537);
			this.mpanel_pendingDatasets.TabIndex = 29;
			// 
			// mtabpage_watcherConfig
			// 
			this.mtabpage_watcherConfig.Controls.Add(this.mgroupBox_watcher);
			this.mtabpage_watcherConfig.Controls.Add(this.mbutton_control);
			this.mtabpage_watcherConfig.Location = new System.Drawing.Point(4, 29);
			this.mtabpage_watcherConfig.Name = "mtabpage_watcherConfig";
			this.mtabpage_watcherConfig.Padding = new System.Windows.Forms.Padding(3);
			this.mtabpage_watcherConfig.Size = new System.Drawing.Size(706, 537);
			this.mtabpage_watcherConfig.TabIndex = 0;
			this.mtabpage_watcherConfig.Text = "Watcher";
			this.mtabpage_watcherConfig.UseVisualStyleBackColor = true;
			// 
			// mgroupBox_watcher
			// 
			this.mgroupBox_watcher.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mgroupBox_watcher.Controls.Add(this.label4);
			this.mgroupBox_watcher.Controls.Add(this.mnum_minutesToWait);
			this.mgroupBox_watcher.Controls.Add(this.mbutton_explorer);
			this.mgroupBox_watcher.Controls.Add(this.label6);
			this.mgroupBox_watcher.Controls.Add(this.label5);
			this.mgroupBox_watcher.Controls.Add(this.mcheckBox_autoStartup);
			this.mgroupBox_watcher.Controls.Add(this.mutton_browse);
			this.mgroupBox_watcher.Controls.Add(this.mcombo_searchType);
			this.mgroupBox_watcher.Controls.Add(this.label8);
			this.mgroupBox_watcher.Controls.Add(this.mtextBox_path);
			this.mgroupBox_watcher.Controls.Add(this.mtextBox_extension);
			this.mgroupBox_watcher.Location = new System.Drawing.Point(6, 6);
			this.mgroupBox_watcher.Name = "mgroupBox_watcher";
			this.mgroupBox_watcher.Size = new System.Drawing.Size(694, 340);
			this.mgroupBox_watcher.TabIndex = 34;
			this.mgroupBox_watcher.TabStop = false;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 22);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(144, 20);
			this.label4.TabIndex = 33;
			this.label4.Text = "Directory To Watch";
			// 
			// mnum_minutesToWait
			// 
			this.mnum_minutesToWait.Location = new System.Drawing.Point(343, 160);
			this.mnum_minutesToWait.Name = "mnum_minutesToWait";
			this.mnum_minutesToWait.Size = new System.Drawing.Size(193, 26);
			this.mnum_minutesToWait.TabIndex = 19;
			this.mnum_minutesToWait.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.mnum_minutesToWait.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
			// 
			// mbutton_explorer
			// 
			this.mbutton_explorer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_explorer.Location = new System.Drawing.Point(612, 40);
			this.mbutton_explorer.Name = "mbutton_explorer";
			this.mbutton_explorer.Size = new System.Drawing.Size(72, 31);
			this.mbutton_explorer.TabIndex = 31;
			this.mbutton_explorer.Text = "Explore";
			this.mbutton_explorer.UseVisualStyleBackColor = true;
			this.mbutton_explorer.Click += new System.EventHandler(this.mbutton_explorer_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(6, 162);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(335, 20);
			this.label6.TabIndex = 18;
			this.label6.Text = "Minutes to wait after file size has not changed.";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 121);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(189, 20);
			this.label5.TabIndex = 22;
			this.label5.Text = "File extension for data file";
			// 
			// mcheckBox_autoStartup
			// 
			this.mcheckBox_autoStartup.AutoSize = true;
			this.mcheckBox_autoStartup.Location = new System.Drawing.Point(343, 202);
			this.mcheckBox_autoStartup.Name = "mcheckBox_autoStartup";
			this.mcheckBox_autoStartup.Size = new System.Drawing.Size(150, 24);
			this.mcheckBox_autoStartup.TabIndex = 29;
			this.mcheckBox_autoStartup.Text = "Watch on startup";
			this.mcheckBox_autoStartup.UseVisualStyleBackColor = true;
			// 
			// mutton_browse
			// 
			this.mutton_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.mutton_browse.Location = new System.Drawing.Point(543, 40);
			this.mutton_browse.Name = "mutton_browse";
			this.mutton_browse.Size = new System.Drawing.Size(63, 31);
			this.mutton_browse.TabIndex = 17;
			this.mutton_browse.Text = "...";
			this.mutton_browse.UseVisualStyleBackColor = true;
			this.mutton_browse.Click += new System.EventHandler(this.mutton_browse_Click);
			// 
			// mcombo_searchType
			// 
			this.mcombo_searchType.FormattingEnabled = true;
			this.mcombo_searchType.Location = new System.Drawing.Point(343, 77);
			this.mcombo_searchType.Name = "mcombo_searchType";
			this.mcombo_searchType.Size = new System.Drawing.Size(194, 28);
			this.mcombo_searchType.TabIndex = 28;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 85);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(98, 20);
			this.label8.TabIndex = 29;
			this.label8.Text = "Search Type";
			// 
			// mtextBox_path
			// 
			this.mtextBox_path.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mtextBox_path.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.mtextBox_path.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
			this.mtextBox_path.Location = new System.Drawing.Point(10, 45);
			this.mtextBox_path.Name = "mtextBox_path";
			this.mtextBox_path.Size = new System.Drawing.Size(527, 26);
			this.mtextBox_path.TabIndex = 15;
			this.mtextBox_path.Text = "m:\\data\\test";
			// 
			// mtextBox_extension
			// 
			this.mtextBox_extension.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.mtextBox_extension.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
			this.mtextBox_extension.Location = new System.Drawing.Point(343, 121);
			this.mtextBox_extension.Name = "mtextBox_extension";
			this.mtextBox_extension.Size = new System.Drawing.Size(194, 26);
			this.mtextBox_extension.TabIndex = 21;
			this.mtextBox_extension.Text = ".ms";
			this.mtextBox_extension.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// mbutton_control
			// 
			this.mbutton_control.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.mbutton_control.Location = new System.Drawing.Point(541, 463);
			this.mbutton_control.Name = "mbutton_control";
			this.mbutton_control.Size = new System.Drawing.Size(159, 58);
			this.mbutton_control.TabIndex = 30;
			this.mbutton_control.Text = "Monitor";
			this.mbutton_control.UseVisualStyleBackColor = true;
			this.mbutton_control.Click += new System.EventHandler(this.mbutton_control_Click);
			// 
			// mtabPages
			// 
			this.mtabPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mtabPages.Controls.Add(this.mtabpage_watcherConfig);
			this.mtabPages.Controls.Add(this.mtabPage_pendingDatasets);
			this.mtabPages.Controls.Add(this.mtabPage_failedDatasets);
			this.mtabPages.Controls.Add(this.mtabPage_completedDatasets);
			this.mtabPages.Controls.Add(this.mtabPage_config);
			this.mtabPages.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.mtabPages.Location = new System.Drawing.Point(14, 153);
			this.mtabPages.Name = "mtabPages";
			this.mtabPages.SelectedIndex = 0;
			this.mtabPages.Size = new System.Drawing.Size(714, 592);
			this.mtabPages.TabIndex = 1;
			// 
			// mtimer_reloadDMSData
			// 
			this.mtimer_reloadDMSData.Enabled = true;
			this.mtimer_reloadDMSData.Interval = 3600000;
			this.mtimer_reloadDMSData.Tick += new System.EventHandler(this.mtimer_reloadDMSData_Tick);
			// 
			// mainBuzzard
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(740, 796);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.mlabel_synchedWithDMS);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.mtabPages);
			this.Controls.Add(this.mpictureBox_buzzards);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "mainBuzzard";
			this.Text = "Buzzard";
			((System.ComponentModel.ISupportInitialize)(this.mwatcher_files)).EndInit();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.mpictureBox_buzzards)).EndInit();
			this.mtabPage_config.ResumeLayout(false);
			this.mgroupBox_autoUploads.ResumeLayout(false);
			this.mgroupBox_autoUploads.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.mgroupBox_instrument.ResumeLayout(false);
			this.mgroupBox_instrument.PerformLayout();
			this.mgroupBox_cart.ResumeLayout(false);
			this.mgroupBox_cart.PerformLayout();
			this.mtabPage_failedDatasets.ResumeLayout(false);
			this.mtabPage_completedDatasets.ResumeLayout(false);
			this.mtabPage_pendingDatasets.ResumeLayout(false);
			this.mtabpage_watcherConfig.ResumeLayout(false);
			this.mgroupBox_watcher.ResumeLayout(false);
			this.mgroupBox_watcher.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.mnum_minutesToWait)).EndInit();
			this.mtabPages.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.PictureBox mpictureBox_buzzards;
        private System.IO.FileSystemWatcher mwatcher_files;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel mlabel_status;
        private System.Windows.Forms.Timer mtimer_modifiedTimer;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label mlabel_synchedWithDMS;
        private System.Windows.Forms.TabControl mtabPages;
        private System.Windows.Forms.TabPage mtabpage_watcherConfig;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button mbutton_explorer;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button mutton_browse;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox mtextBox_extension;
        private System.Windows.Forms.TextBox mtextBox_path;
        private System.Windows.Forms.ComboBox mcombo_searchType;
        private System.Windows.Forms.Button mbutton_control;
        private System.Windows.Forms.CheckBox mcheckBox_autoStartup;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown mnum_minutesToWait;
        private System.Windows.Forms.TabPage mtabPage_pendingDatasets;
        private System.Windows.Forms.Panel mpanel_pendingDatasets;
        private System.Windows.Forms.TabPage mtabPage_completedDatasets;
        private System.Windows.Forms.Panel mpanel_completedDatasets;
        private System.Windows.Forms.TabPage mtabPage_failedDatasets;
        private System.Windows.Forms.Panel mpanel_failedDatasets;
        private System.Windows.Forms.TabPage mtabPage_config;
        private System.Windows.Forms.Button mbutton_reloadDMS;
        private System.Windows.Forms.GroupBox mgroupBox_autoUploads;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox mtextbox_triggerLocation;
        private System.Windows.Forms.CheckBox mcheckBox_copyTriggerFiles;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox mcombo_Operator;
        private System.Windows.Forms.GroupBox mgroupBox_instrument;
        private System.Windows.Forms.ComboBox comboBoxAvailInstruments;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox mgroupBox_cart;
        private System.Windows.Forms.Label mlabel_cartName;
        private System.Windows.Forms.ComboBox mcombo_SepType;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox mgroupBox_watcher;
        private System.Windows.Forms.Button mbutton_exploreDMSTrigger;
        private System.Windows.Forms.ComboBox mcomboBox_columnData;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button mbutton_reloadDMSInformation;
        private System.Windows.Forms.ComboBox mcombo_cartName;
        private System.Windows.Forms.Timer mtimer_reloadDMSData;
        private System.Windows.Forms.Button mbutton_clearFinished;
        private System.Windows.Forms.Button mbutton_clearFailed;
    }
}

