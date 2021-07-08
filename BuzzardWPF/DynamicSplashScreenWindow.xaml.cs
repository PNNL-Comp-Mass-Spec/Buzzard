using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using LcmsNetData;
using LcmsNetData.Logging;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for DynamicSplashScreenWindow.xaml
    /// </summary>
    public partial class DynamicSplashScreenWindow : Window, INotifyPropertyChangedExt
    {
        public DynamicSplashScreenWindow()
        {
            InitializeComponent();
            DataContext = this;

            ApplicationLogger.Message += ApplicationLogger_ItemLogged;
            ApplicationLogger.Error += ApplicationLogger_ItemLogged;
            FileLogger.LogFilePathDefined += ApplicationLogger_LogFilePathDefined;

            var assem = Assembly.GetEntryAssembly();
            var assemName = assem.GetName();
            var ver = assemName.Version.ToString(3) + "; " + AppInitializer.AssemblyDate;

            if (Properties.Settings.Default.IsTestVersion)
            {
                ver += "    TEST VERSION";
            }

            Version = ver;
        }

        private string lastLoggedItem;
        private string logFilePath;
        private string version;
        private string instrumentHostName;

        public string LastLoggedItem
        {
            get => lastLoggedItem;
            private set => this.RaiseAndSetIfChanged(ref lastLoggedItem, value);
        }

        public string LogFilePath
        {
            get => logFilePath;
            private set => this.RaiseAndSetIfChanged(ref logFilePath, value);
        }

        public string Version
        {
            get => version;
            private set => this.RaiseAndSetIfChanged(ref version, value);
        }

        public string InstrumentHostName
        {
            get => instrumentHostName;
            private set => this.RaiseAndSetIfChanged(ref instrumentHostName, value);
        }

        public bool IsComplete { get; private set; }

        private void ApplicationLogger_ItemLogged(int messageLevel, MessageLoggerArgs args)
        {
            try
            {
                Dispatcher.Invoke(() => LastLoggedItem = args.Message, DispatcherPriority.Send);
            }
            catch (Exception)
            {
                ApplicationLogger.LogMessage(0, "Could not update splash screen status. Message: " + args.Message);
            }
        }

        public void SetInstrumentHostName(string name)
        {
            Dispatcher.Invoke(() => InstrumentHostName = name, DispatcherPriority.Send);
        }

        private void ApplicationLogger_LogFilePathDefined(MessageLoggerArgs args)
        {
            try
            {
                Dispatcher.Invoke(() => LogFilePath = "Log file: " + args.Message, DispatcherPriority.Send);
            }
            catch (Exception)
            {
                ApplicationLogger.LogMessage(0, "Could not update splash screen log file path.");
            }
        }

        public async void LoadComplete()
        {
            if (IsComplete)
            {
                return;
            }
            // Wait for any updates to complete, to avoid thread cancellation exceptions
            IsComplete = true;
            if (Dispatcher.CheckAccess())
            {
                await Dispatcher.Yield();
            }
            ApplicationLogger.Message -= ApplicationLogger_ItemLogged;
            ApplicationLogger.Error -= ApplicationLogger_ItemLogged;
            FileLogger.LogFilePathDefined -= ApplicationLogger_LogFilePathDefined;
            if (Dispatcher.CheckAccess())
            {
                await Dispatcher.Yield();
            }
            // Close does not shutdown the dispatcher like it should be.
            //Dispatcher.Invoke(Close);
            Dispatcher.InvokeShutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
