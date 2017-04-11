using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for DynamicSplash.xaml
    /// </summary>
    public partial class DynamicSplash
        : Window, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        #region Attributes
        private BackgroundWorker m_backgroundWorker;
        private bool m_openMainWindow;
        #endregion


        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public DynamicSplash()
        {
            InitializeComponent();
            DataContext = this;

            //
            // Bind logo_2017 to the image on the splash screen
            //
            var ms = new MemoryStream();
            Properties.Resources.logo_2017.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            LogoImageSource = bi;

            m_backgroundWorker = new BackgroundWorker();
            m_backgroundWorker.DoWork += BackgroundWorker_DoWork;
            m_backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            classApplicationLogger.Message += ApplicationLogger_ItemLogged;
            classApplicationLogger.Error += ApplicationLogger_ItemLogged;
            classFileLogging.LogFilePathDefined += ApplicationLogger_LogFilePathDefined;

            var assem = Assembly.GetEntryAssembly();
            var assemName = assem.GetName();
            var ver = assemName.Version + "; " + AppInitializer.PROGRAM_DATE;

            Version = ver;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~DynamicSplash()
        {
            classApplicationLogger.Message -= ApplicationLogger_ItemLogged;
            classApplicationLogger.Error -= ApplicationLogger_ItemLogged;

            m_backgroundWorker.DoWork -= BackgroundWorker_DoWork;
            m_backgroundWorker.RunWorkerCompleted -= BackgroundWorker_RunWorkerCompleted;
            m_backgroundWorker = null;
        }
        #endregion


        #region Properties
        public string LastLoggedItem
        {
            get { return m_lastLoggedItem; }
            set
            {
                if (m_lastLoggedItem != value)
                {
                    m_lastLoggedItem = value;
                    OnPropertyChanged("LastLoggedItem");
                }
            }
        }
        private string m_lastLoggedItem;

        public string LogFilePath
        {
            get { return m_logFilePath; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    m_logFilePath = value;
                    OnPropertyChanged("LogFilePath");
                }
            }
        }
        private string m_logFilePath;

        public BitmapImage LogoImageSource
        {
            get { return m_logoImageSource; }
            set
            {
                if (!Equals(m_logoImageSource, value))
                {
                    m_logoImageSource = value;
                    OnPropertyChanged("LogoImageSource");
                }
            }
        }
        private BitmapImage m_logoImageSource;

        public string Version
        {
            get { return m_version; }
            set
            {
                if (m_version != value)
                {
                    m_version = value;
                    OnPropertyChanged("Version");
                }
            }
        }
        private string m_version;

        public string InstrumentName
        {
            get { return m_instrumentName; }
            set
            {
                if (m_instrumentName != value)
                {
                    m_instrumentName = value;
                    OnPropertyChanged("InstrumentName");
                }
            }
        }
        private string m_instrumentName;
        #endregion


        #region Event Handlers
        void ApplicationLogger_ItemLogged(int messageLevel, classMessageLoggerArgs args)
        {
            LastLoggedItem = args.Message;
        }

        void ApplicationLogger_LogFilePathDefined(classMessageLoggerArgs args)
        {
            LogFilePath = "Log file: " + args.Message;
        }   
   
        void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            m_openMainWindow = AppInitializer.InitializeApplication();
        }

        void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (m_openMainWindow)
            {
                var mainWindow = new Main();
                Application.Current.MainWindow = mainWindow;
                try
                {
                    mainWindow.Show();
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(0, "Buzzard quit unexpectedly. " + ex.Message, ex);
                }
            }

            Close();
        }
        #endregion


        #region Methods
        public void InitializeInBackground()
        {
            m_backgroundWorker.RunWorkerAsync();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
