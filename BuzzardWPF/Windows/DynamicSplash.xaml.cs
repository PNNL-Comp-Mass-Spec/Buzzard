using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

using BuzzardWPF;
using LcmsNetDataClasses.Logging;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;


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
		private BackgroundWorker	m_backgroundWorker;
		private bool				m_openMainWindow;
		#endregion


		#region Constructors
		public DynamicSplash()
		{
			InitializeComponent();
			this.DataContext = this;

			///
			/// I don't know why I need to do this to set the logo image, but
			/// for some reason using a static resource isn't working at run
			/// time. On top of that, using a relative URI wasn't working either.
			/// So I had to settle for a binding.
			/// - FCT
			/// 
			MemoryStream ms = new MemoryStream();
			Properties.Resources.logo.Save(ms, ImageFormat.Png);
			ms.Position = 0;
			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			bi.StreamSource = ms;
			bi.EndInit();

			LogoImageSource = bi;
			///
			/// End set logo image.
			/// 

			m_backgroundWorker = new BackgroundWorker();
			m_backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_DoWork);
			m_backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_RunWorkerCompleted);

			classApplicationLogger.Message	+= ApplicationLogger_ItemLogged;
			classApplicationLogger.Error	+= ApplicationLogger_ItemLogged;

			Assembly		assem		= Assembly.GetEntryAssembly();
			AssemblyName	assemName	= assem.GetName();
			Version			ver			= assemName.Version;

			this.Version = ver.ToString();
		}

		~DynamicSplash()
		{
			classApplicationLogger.Message	-= ApplicationLogger_ItemLogged;
			classApplicationLogger.Error	-= ApplicationLogger_ItemLogged;

			m_backgroundWorker.DoWork				-= BackgroundWorker_DoWork;
			m_backgroundWorker.RunWorkerCompleted	-= BackgroundWorker_RunWorkerCompleted;
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

		public BitmapImage LogoImageSource
		{
			get { return m_logoImageSource; }
			set
			{
				if (m_logoImageSource != value)
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

		void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			m_openMainWindow = AppInitializer.InitializeApplication();
		}

		void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (m_openMainWindow)
			{                
				Main mainWindow = new Main();
                App.Current.MainWindow = mainWindow;
                try
                {
                    mainWindow.Show();
                }
                catch(Exception ex)
                {
                    classApplicationLogger.LogError(0, "Buzzard quit unexpectedly. " + ex.Message, ex);
                }				
			}

			this.Close();
		}
		#endregion


		#region Methods
		public void InitializeInBackground()
		{
			m_backgroundWorker.RunWorkerAsync();
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
