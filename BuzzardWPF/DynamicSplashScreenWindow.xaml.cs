﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            var ver = assemName.Version + "; " + AppInitializer.AssemblyDate;

            if (Properties.Settings.Default.IsTestVersion)
            {
                ver += "    TEST VERSION";
            }

            Version = ver;
        }

        private string lastLoggedItem;
        private string logFilePath;
        private string version;
        private string instrumentName;

        #region Properties
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

        public string InstrumentName
        {
            get => instrumentName;
            private set => this.RaiseAndSetIfChanged(ref instrumentName, value);
        }

        public bool IsComplete { get; private set; }
        #endregion

        #region Event Handlers
        void ApplicationLogger_ItemLogged(int messageLevel, MessageLoggerArgs args)
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

        public void SetInstrumentName(string name)
        {
            Dispatcher.Invoke(() => InstrumentName = name, DispatcherPriority.Send);
        }

        void ApplicationLogger_LogFilePathDefined(MessageLoggerArgs args)
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
        #endregion

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
            Dispatcher.Invoke(Close);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
