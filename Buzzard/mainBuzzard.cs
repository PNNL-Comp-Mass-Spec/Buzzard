using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LcmsNetDataClasses.Logging;
using Buzzard.Data;
using System.IO;
using System.Diagnostics;
using LcmsNetDataClasses;
using LcmsNetDmsTools;

namespace Buzzard
{
    public partial class mainBuzzard : Form
    {
        #region Members
        /// <summary>
        /// Maps an instrument name to it's related data.
        /// </summary>
        private Dictionary<string, classInstrumentInfo> mdict_instrumentMap;
        private Dictionary<string, string> mdict_instrumentCommonName;
        /// <summary>
        /// Flag indicating whether the timer is started to watch for new files.
        /// </summary>
        private bool mbool_started;
        /// <summary>
        /// List of buzzards
        /// </summary>
        private List<Image> mlist_buzzards;
        /// <summary>
        /// Value indicating what image to use based on the timer.
        /// </summary>
        private int mint_timer;
        /// <summary>
        /// Object that manages the queue of datasets.
        /// </summary>
        private classDatasetManager mobj_manager;
        /// <summary>
        /// Maps a dataset to a control.
        /// </summary>
        private Dictionary<classDataset, DatasetControl> mdict_datasetControlMap;
        /// <summary>
        /// List of users
        /// </summary>
        private Dictionary<string, classUserInfo> mobj_Users;        
        private bool mbool_loading = false;
        #endregion

        FolderBrowserDialog mdialog_folder;

        /// <summary>
        /// Constructor.
        /// </summary>
        public mainBuzzard()
        {
            InitializeComponent();

            mdict_instrumentMap         = new Dictionary<string, classInstrumentInfo>();
            mdict_instrumentCommonName  = new Dictionary<string, string>();
            mbool_started               = false;
            mobj_manager                = new classDatasetManager();

            mdialog_folder = new FolderBrowserDialog();
            mdialog_folder.ShowNewFolderButton = true;
            mdialog_folder.SelectedPath = Environment.SpecialFolder.Desktop.ToString();


            // User interface updates
            mlist_buzzards                   = new List<Image>() {Properties.Resources.buzzards1, Properties.Resources.buzzards2, Properties.Resources.buzzards3, Properties.Resources.buzzards4};
            if (Properties.Settings.Default.TurdAlert)
            {
                mlist_buzzards.Add(Properties.Resources.buzzards5);
            }

            mcombo_searchType.Items.AddRange(new object [] {System.IO.SearchOption.AllDirectories, 
                                                            System.IO.SearchOption.TopDirectoryOnly});
            // Configure flags and UI items.
            mcombo_searchType.SelectedIndex  = 1;
            mobj_Users                       = new Dictionary<string, classUserInfo>();
            LoadSettings();

            classApplicationLogger.LogMessage(0, "Gathering beats from DMS");
            Application.DoEvents();
            mobj_manager.LoadDMSCache();
            
            // Maps the data to user controls.
            mdict_datasetControlMap = new Dictionary<classDataset, DatasetControl>();

            // Event Synching 
            mtimer_modifiedTimer.Tick       += new EventHandler(mtimer_modifiedTimer_Tick);
            classApplicationLogger.Message  += new classApplicationLogger.DelegateMessageHandler(classApplicationLogger_Message);
            classApplicationLogger.Error    += new classApplicationLogger.DelegateErrorHandler(classApplicationLogger_Error);
            mobj_manager.DatasetPending     += new EventHandler<classDatasetQueueEventArgs>(mobj_manager_DatasetPending);
            mobj_manager.DatasetsLoaded     += new EventHandler(mobj_manager_DatasetsLoaded);
            mobj_manager.DatasetSent        += new EventHandler<classDatasetQueueEventArgs>(mobj_manager_DatasetSent);
            mobj_manager.DatasetFailed      += new EventHandler<classDatasetQueueEventArgs>(mobj_manager_DatasetFailed);
            mobj_manager.DatasetCleared += new EventHandler<classDatasetQueueEventArgs>(mobj_manager_DatasetCleared);
            mobj_manager.DatasetFailedCleared += new EventHandler<classDatasetQueueEventArgs>(mobj_manager_DatasetFailedCleared);
            mwatcher_files.Created          += new FileSystemEventHandler(mwatcher_files_Created);
            mwatcher_files.Renamed          += new RenamedEventHandler(mwatcher_files_Renamed);
            mwatcher_files.Deleted          += new FileSystemEventHandler(mwatcher_files_Deleted);

            this.mcombo_SepType.SelectedIndexChanged += new System.EventHandler(this.mcombo_SepType_SelectedIndexChanged);

            if (mcheckBox_autoStartup.Checked)
            {
                StartStop();
            }


            Assembly assem          = Assembly.GetEntryAssembly();
            AssemblyName assemName  = assem.GetName();
            Version ver             = assemName.Version;
            string version          = string.Format("v. {0}", ver.ToString());

            string instName = "";

            try
            {
                instName = mdict_instrumentCommonName[classLCMSSettings.GetParameter("InstName")];
            }
            catch
            {
            }

            Text = string.Format("Buzzard - [{0}] - {1}",
                                        version,
                                        instName);
        }

        void mobj_manager_DatasetFailedCleared(object sender, classDatasetQueueEventArgs e)
        {
            mpanel_failedDatasets.Controls.Clear();
            UpdatePanelCounts();
        }

        void mobj_manager_DatasetCleared(object sender, classDatasetQueueEventArgs e)
        {
            mpanel_completedDatasets.Controls.Clear();
            UpdatePanelCounts();
        }


        #region Settings
        private void LoadSettings()
        {
            mcheckBox_autoStartup.Checked   = Convert.ToBoolean(classLCMSSettings.GetParameter("AutoMonitor"));
            mnum_minutesToWait.Value        = Convert.ToDecimal(classLCMSSettings.GetParameter("Duration"));
            mtextBox_extension.Text         = classLCMSSettings.GetParameter("WatchExtension");
            mtextBox_path.Text              = classLCMSSettings.GetParameter("WatchDirectory");            
            mcombo_searchType.SelectedItem  = (SearchOption)Enum.Parse(typeof(SearchOption), 
                                                    classLCMSSettings.GetParameter("SearchType"));


            string folderName = mtextBox_path.Text;
            if (Directory.Exists(folderName))
            {
                mdialog_folder.SelectedPath = folderName;
            }

            mtextbox_triggerLocation.Text = classLCMSSettings.GetParameter("TriggerFileFolder");
            
            LoadDmsInformation();
            LoadApplicationSettings();
        }
        /// <summary>
        /// Sets the selected index in a combo box.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="box"></param>
        private void SetComboName(string name, ComboBox box)
        {
            int  indx  = 0;
            bool found = false;
            
            foreach (object data in box.Items)
            {
                string itemName = data.ToString();

                if (itemName == name)
                {
                    found = true;
                    break;
                }
                indx++;
            }

            if (found)
            {
                box.SelectedIndex = indx;
            }
            else if (box.Items.Count > 0)
            {
                box.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// Adds data to a combobox.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="box"></param>
        private void AddDataToCombobox(List<string> data, ComboBox box)
        {
            box.Items.Clear();
            foreach (string name in data)
            {
                box.Items.Add(name);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void LoadDmsInformation()
        {

            // Cart name
            List<string> carts   = LcmsNetDmsTools.classSQLiteTools.GetCartNameList(true);            
            if (carts == null)
            {
                classApplicationLogger.LogError(0, "Cart list is not available.");
                return;
            }

            List<string> columns = LcmsNetDmsTools.classSQLiteTools.GetColumnList(true);
            if (columns == null)
            {
                classApplicationLogger.LogError(0, "Column list is not available.");
                return;
            }
                       
            // Load combo box
            List<classInstrumentInfo> instList = LcmsNetDmsTools.classSQLiteTools.GetInstrumentList(false);
            if (instList == null || instList.Count < 1)
            {
                classApplicationLogger.LogError(0, "Instrument list is not available.");
                return;
            }

            List<string> separations = classSQLiteTools.GetSepTypeList(false); ;
            if (separations == null || separations.Count < 1)
            {
                classApplicationLogger.LogError(0, "Separation types list is not available.");
                return;
            }

            List<classUserInfo> userList = classSQLiteTools.GetUserList(false);
            if (userList == null)
            {
                classApplicationLogger.LogError(0, "User list is not available.");
                return;
            }

            // Handle users differently.
            mobj_Users.Clear();         
            classUserInfo tmpUser   = new classUserInfo();
            tmpUser.PayrollNum      = "None";
            tmpUser.UserName        = "(None)";
            mobj_Users.Add(tmpUser.UserName, tmpUser);            
            foreach (classUserInfo currUser in userList)
            {
                mobj_Users.Add(currUser.UserName, currUser);
            }

            // Now add data back to the combo boxes.
            List<string> users          = userList.ConvertAll<string>(x => x.UserName);            
            List<string> instruments    = instList.ConvertAll<string>(x => x.CommonName);
            // Map the instrument name to the appropiate data.
            instList.ForEach(x => mdict_instrumentMap.Add(x.CommonName, x));
            instList.ForEach(x => mdict_instrumentCommonName.Add(x.DMSName, x.CommonName));

            carts.Add("No_Cart");
            carts.Sort();
            
            AddDataToCombobox(columns,          mcomboBox_columnData);
            AddDataToCombobox(instruments,      comboBoxAvailInstruments);
            AddDataToCombobox(separations,      mcombo_SepType);
            AddDataToCombobox(users,            mcombo_Operator);
            AddDataToCombobox(carts,            mcombo_cartName);            

            string currentName = classLCMSSettings.GetParameter("InstName");   
            try
            {
                currentName = mdict_instrumentCommonName[currentName];
            }
            catch
            {
            }
            SetComboName(currentName, comboBoxAvailInstruments);

            currentName = classLCMSSettings.GetParameter("SeparationType");
            SetComboName(currentName, mcombo_SepType);

            currentName = classLCMSSettings.GetParameter("ColumnData");
            SetComboName(currentName, mcomboBox_columnData);
            
            currentName = classLCMSSettings.GetParameter("CartName");
            SetComboName(currentName, mcombo_cartName);

            currentName = classLCMSSettings.GetParameter("Operator");
            SetComboName(currentName, mcombo_Operator);
        }
        /// <summary>
        /// Loads the application settings to the user interface.
        /// </summary>
        private void LoadApplicationSettings()
        {
            mcheckBox_copyTriggerFiles.Checked = Convert.ToBoolean(classLCMSSettings.GetParameter("CopyTriggerFiles"));            
        }
        /// <summary>
        /// Saves the settings.
        /// </summary>
        private void SaveSettings()
        {            
            classLCMSSettings.SetParameter("AutoMonitor"      , mcheckBox_autoStartup.Checked.ToString());   
            classLCMSSettings.SetParameter("Duration"         , mnum_minutesToWait.Value.ToString());        
            classLCMSSettings.SetParameter("WatchExtension"   , mtextBox_extension.Text);         
            classLCMSSettings.SetParameter("WatchDirectory"   , mtextBox_path.Text);
            classLCMSSettings.SetParameter("SearchType"       , mcombo_searchType.SelectedItem.ToString());                        
        }
        #endregion

        #region File Watcher Events
        void mwatcher_files_Renamed(object sender, RenamedEventArgs e)
        {
            string extension = Path.GetExtension(e.FullPath).ToLower();

            if (extension == mtextBox_extension.Text.ToLower())
            {
                mobj_manager.CreateDataset(e.FullPath,
                                    new TimeSpan(0, Convert.ToInt32(mnum_minutesToWait.Value), 0),
                                    DateTime.Now);
            }
        }
        void mwatcher_files_Deleted(object sender, FileSystemEventArgs e)
        {
            //TODO: Handle these.       
        }
        /// <summary>
        /// Helps tell the manager to create new files if it needs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mwatcher_files_Created(object sender, FileSystemEventArgs e)
        {
            string extension = Path.GetExtension(e.FullPath).ToLower();

            if (extension == mtextBox_extension.Text.ToLower())
            {
                mobj_manager.CreateDataset(e.FullPath,
                                    new TimeSpan(0, Convert.ToInt32(mnum_minutesToWait.Value), 0),
                                    DateTime.Now);
            }
        }
        #endregion

        #region Application Logging Event Handlers and Status Methods
        void classApplicationLogger_Error(int errorLevel, classErrorLoggerArgs args)
        {
            UpdateStatus(args.Message);
        }
        void classApplicationLogger_Message(int messageLevel, classMessageLoggerArgs args)
        {
            UpdateStatus(args.Message);
        }
        private void UpdateStatus(string message)
        {
            mlabel_status.Text = message;
        }
        #endregion

        #region Dataset Control Event Handlers
        /// <summary>
        /// Updates the status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void control_ResolvedInDMSHover(object sender, EventArgs e)
        {
            UpdateStatus("Resolved in DMS.");
        }/// <summary>
        /// Updates the status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void control_NotResolvedInDMSHover(object sender, EventArgs e)
        {
            UpdateStatus("Not resolved in DMS.");
        }/// <summary>
        /// Updates the status bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void control_NoIndicatorHover(object sender, EventArgs e)
        {
            UpdateStatus("");
        }
        #endregion

        #region Dataset Manager Event Handlers
        /// <summary>
        /// Tells the indicator light to turn on when synched with DMS.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mobj_manager_DatasetsLoaded(object sender, EventArgs e)
        {
            mlabel_synchedWithDMS.BackColor = Color.Lime;
        }
        /// <summary>
        /// Handles when a dataset trigger file is created.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mobj_manager_DatasetSent(object sender, classDatasetQueueEventArgs e)
        {

            bool didContain = mdict_datasetControlMap.ContainsKey(e.Dataset);
            DatasetControl control = null;
            if (didContain)
            {
                control = mdict_datasetControlMap[e.Dataset];
            }
            else
            {
                classApplicationLogger.LogMessage(0, "Dataset did not exist before in the user interface.  Creating new one.");
                control = new DatasetControl(e.Dataset);
            }
            switch (e.QueueFrom)
            {
                case DatasetQueueType.Pending:
                    if (mpanel_pendingDatasets.Contains(control))
                    {
                        mpanel_pendingDatasets.Controls.Remove(control);
                    }
                    break;
                case DatasetQueueType.Failed:
                    if (mpanel_failedDatasets.Contains(control))
                    {
                        mpanel_failedDatasets.Controls.Remove(control);
                    }
                    break;
            }
            control.CompleteDataset();
            mpanel_completedDatasets.Controls.Add(control);
            UpdatePanelCounts();
        }
        /// <summary>
        /// Handles when a new dataset is added and creates a control for making a trigger file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mobj_manager_DatasetPending(object sender, classDatasetQueueEventArgs e)
        {
            classApplicationLogger.LogMessage(1, string.Format("A new dataset file was found: {0}", e.Dataset.Name));
            DatasetControl control          = new DatasetControl(e.Dataset);
            control.Dock                    = DockStyle.Top;
            control.SendToBack();
            mpanel_pendingDatasets.Controls.Add(control);

            control.NotResolvedInDMSHover   += new EventHandler(control_NotResolvedInDMSHover);
            control.ResolvedInDMSHover      += new EventHandler(control_ResolvedInDMSHover);
            control.NoIndicatorHover        += new EventHandler(control_NoIndicatorHover);

            mdict_datasetControlMap.Add(e.Dataset, control);
            UpdatePanelCounts();
        }
        /// <summary>
        /// Handles when a dataset is failed to have a trigger file created for.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mobj_manager_DatasetFailed(object sender, classDatasetQueueEventArgs e)
        {
            if (e.QueueFrom == DatasetQueueType.Pending)
            {
                bool didContain = mdict_datasetControlMap.ContainsKey(e.Dataset);
                DatasetControl control = null;
                if (didContain)
                {
                    control  = mdict_datasetControlMap[e.Dataset];
                    if (mpanel_pendingDatasets.Contains(control))
                    {
                        mpanel_pendingDatasets.Controls.Remove(control);
                    }
                }   
                else
                {                    
                    classApplicationLogger.LogMessage(0, "Dataset did not exist before in the user interface.  Creating new one.");
                    control = new DatasetControl(e.Dataset);
                }
                control.CompleteDataset();
                mpanel_failedDatasets.Controls.Add(control);
            }
            UpdatePanelCounts();
        }
        private void UpdatePanelCounts()
        {
            mtabPage_failedDatasets.Text    = string.Format("Failed ({0})", mpanel_failedDatasets.Controls.Count);
            mtabPage_pendingDatasets.Text   = string.Format("Pending ({0})", mpanel_pendingDatasets.Controls.Count);
            mtabPage_completedDatasets.Text = string.Format("Completed ({0})", mpanel_completedDatasets.Controls.Count);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Loads new datasets by searching the directory for them.
        /// </summary>
        private void Research()
        {
            string path = mtextBox_path.Text;
            string pattern = "*" + mtextBox_extension.Text;
            TimeSpan span = new TimeSpan(0, Convert.ToInt32(mnum_minutesToWait.Value), 0);

            System.IO.SearchOption searchOption = System.IO.SearchOption.TopDirectoryOnly;
            if (mcombo_searchType.SelectedItem != null)
            {
                searchOption = ((System.IO.SearchOption)mcombo_searchType.SelectedItem);
            }
            mobj_manager.LoadDatasets(path, pattern, span, searchOption);
        }
        /// <summary>
        /// Starts the timer and watcher items for files.
        /// </summary>
        private void StartStop()
        {
            if (mbool_started)
            {
                mbool_started                       = false;
                mbutton_control.Text                = "Monitor";                
                mwatcher_files.EnableRaisingEvents  = false;
                classApplicationLogger.LogMessage(0, "Stopped.");
                classApplicationLogger.LogMessage(0, "Ready.");
                mgroupBox_watcher.Enabled           = true;
            }
            else
            {
                string path = mtextBox_path.Text;
                if (Directory.Exists(path))
                {

                    mbool_started                       = true;
                    mtimer_modifiedTimer.Enabled        = true;
                    mbutton_control.Text                = "Stop";
                    mint_timer                          = 0;

                    bool shouldSearchSubDirectories = false;
                    SearchOption option = SearchOption.TopDirectoryOnly;
                    if (mcombo_searchType.SelectedItem != null)
                    {
                        option = ((SearchOption)mcombo_searchType.SelectedItem);
                    }
                    if (option == SearchOption.AllDirectories)
                    {
                        shouldSearchSubDirectories = true;
                    }

                    mwatcher_files.Path                     = path;
                    mwatcher_files.IncludeSubdirectories    = shouldSearchSubDirectories; 
                    mwatcher_files.Filter                   = "*.*";                    
                    mwatcher_files.EnableRaisingEvents      = true;
                    classApplicationLogger.LogMessage(0, "Monitoring.");
                    mgroupBox_watcher.Enabled = false;
                }
                else
                {
                    classApplicationLogger.LogError(0, "Could not start the monitor.  The supplied path does not exist.");
                }
            }
        }
        /// <summary>
        /// Searches a list of currently pending files.
        /// </summary>
        private void PerformSearch()
        {
            // Update user interface
            if (mbool_started)
            {
                mint_timer = (++mint_timer) % mlist_buzzards.Count;
                mpictureBox_buzzards.BackgroundImage = mlist_buzzards[mint_timer];
            }
            else
            {
                mpictureBox_buzzards.BackgroundImage = Properties.Resources.buzzards;
            }

            // Search
            mobj_manager.UpdateAllDatasets();

            foreach (DatasetControl control in mdict_datasetControlMap.Values)
            {
                control.UpdateWriteSpan();
            }
        }
        #endregion

        #region Form Event Handlers
        void mtimer_modifiedTimer_Tick(object sender, EventArgs e)
        {
            PerformSearch();
        }
        private void mbutton_control_Click(object sender, EventArgs e)
        {
            StartStop();
        }        
        private void mutton_browse_Click(object sender, EventArgs e)
        {
            DialogResult result = mdialog_folder.ShowDialog();
            if (result == DialogResult.OK)
            {
                mtextBox_path.Text = mdialog_folder.SelectedPath;
            }
        }                
        /// <summary>
        /// Opens a windows explorer window to the path in the text box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mbutton_explorer_Click(object sender, EventArgs e)
        {
            string path = mtextBox_path.Text;
            if (Directory.Exists(path))
            {                
                try
                {
                    Process.Start(path);
                }
                catch(Exception ex)
                {
                    classApplicationLogger.LogError(0, "Could not open an Explorer window to that path.", ex);
                }
            }
        }
        void mbutton_reloadDMSInformation_Click(object sender, System.EventArgs e)
        {
            LoadDmsInformation();
        }
        private void mbutton_reloadDMS_Click(object sender, EventArgs e)
        {
            mlabel_synchedWithDMS.BackColor = Color.Maroon;
            mobj_manager.LoadDMSCache();
        }        
        private void mcombo_SepType_SelectedIndexChanged(object sender, EventArgs e)
        {
            classLCMSSettings.SetParameter("SeparationType", mcombo_SepType.Text);
        }
        private void mbutton_exploreDMSTrigger_Click(object sender, EventArgs e)
        {
            string path = mtextbox_triggerLocation.Text;
            if (Directory.Exists(path))
            {
                try
                {
                    Process.Start(path);
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(0, "Could not open an Explorer window to that path.", ex);
                }
            }
        }
        #endregion
        
        private void comboBoxAvailInstruments_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string commonName = comboBoxAvailInstruments.SelectedItem.ToString();

                mobj_manager.InstrumentData = mdict_instrumentMap[commonName];
                classLCMSSettings.SetParameter("InstName", mobj_manager.InstrumentData.DMSName);
            }
            catch
            {

            }
        }
        private void mcomboBox_columnData_SelectedIndexChanged(object sender, EventArgs e)
        {
            classLCMSSettings.SetParameter("ColumnData", mcomboBox_columnData.SelectedItem.ToString());
        }
        private void mcombo_Operator_SelectedIndexChanged(object sender, EventArgs e)
        {
            classLCMSSettings.SetParameter("Operator", mcombo_Operator.SelectedItem.ToString());
        }
        private void mcombo_cartName_SelectedIndexChanged(object sender, EventArgs e)
        {
            classLCMSSettings.SetParameter("CartName", mcombo_cartName.SelectedItem.ToString());
        }
        private void mtimer_reloadDMSData_Tick(object sender, EventArgs e)
        {
            classApplicationLogger.LogMessage(0, "Auto-updating dataset data from DMS.");
            mlabel_synchedWithDMS.BackColor = Color.Maroon;
            mobj_manager.LoadDMSCache();
        }

        private void mbutton_clearFinished_Click(object sender, EventArgs e)
        {
            mobj_manager.ClearCompleted();
        }
    }
}
