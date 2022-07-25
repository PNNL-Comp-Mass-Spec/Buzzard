using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Searching;
using Ookii.Dialogs.Wpf;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherControlViewModel : ReactiveObject
    {
        private readonly VistaFolderBrowserDialog mFolderDialog;
        private string[] directorySelectorOptionsList;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;

        public WatcherControlViewModel()
        {
            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);

            //this.EMSL_DataSelector.BoundContainer = this;

            // Combo box for the search types.
            SearchDepthOptions = new List<SearchOption>
            {
                SearchOption.AllDirectories,
                SearchOption.TopDirectoryOnly
            };

            mFolderDialog = new VistaFolderBrowserDialog { ShowNewFolderButton = true };

            ResetToDefaults();

            SelectDirectoryCommand = ReactiveCommand.Create(SelectDirectory);
            ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);
            MonitorStartStopCommand = ReactiveCommand.Create(MonitorStartStop);

            this.WhenAnyValue(x => x.Config.DirectoryPath).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => SetDirectorySelectorOptionsList());
        }

        private void ResetToDefaults()
        {
            // Leave this unchanged: m_directoryToWatch;
            Config.ResetToDefaults(false);
            Monitor.ResetToDefaults();
        }

        public ReactiveCommand<Unit, Unit> SelectDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }
        public ReactiveCommand<Unit, Unit> MonitorStartStopCommand { get; }

        public IReadOnlyList<SearchOption> SearchDepthOptions { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;
        public DatasetMonitor Monitor => DatasetMonitor.Monitor;

        public SearchConfig Config => DatasetManager.Config;
        public FileSystemWatcherManager Watcher => FileSystemWatcherManager.Instance;

        public bool IsNotMonitoring => isNotMonitoring.Value;

        public string[] DirectorySelectorOptionsList
        {
            get => directorySelectorOptionsList;
            private set => this.RaiseAndSetIfChanged(ref directorySelectorOptionsList, value);
        }

        private void SetDirectorySelectorOptionsList()
        {
            var text = Config.DirectoryPath;
            string dirname;

            try
            {
                dirname = Path.GetDirectoryName(text);
            }
            catch
            {
                dirname = null;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(dirname))
                {
                    var drives = DriveInfo.GetDrives();
                    var driveNames = drives.Select(drive => drive.Name).ToArray();
                    DirectorySelectorOptionsList = driveNames;
                }
                else if (Directory.Exists(dirname))
                {
                    var subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
                    DirectorySelectorOptionsList = subFolders;
                }
            }
            catch
            {
                // Ignore errors here
            }
        }

        private void SelectDirectory()
        {
            mFolderDialog.SelectedPath = Config.DirectoryPath;

            var result = mFolderDialog.ShowDialog();

            if (result == true)
            {
                Config.DirectoryPath = mFolderDialog.SelectedPath;
            }
        }

        private void MonitorStartStop()
        {
            if (Watcher.IsMonitoring)
            {
                Watcher.StopWatching();
            }
            else
            {
                Watcher.StartWatching();
            }
        }
    }
}
