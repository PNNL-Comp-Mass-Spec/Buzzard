﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public sealed class TriggerFileCreationManager : ReactiveObject
    {
        public static TriggerFileCreationManager Instance { get; }

        static TriggerFileCreationManager()
        {
            Instance = new TriggerFileCreationManager();
        }

        private TriggerFileCreationManager()
        {
        }

        private bool abortTriggerCreationNow;
        private bool isCreatingTriggerFiles;

        public bool IsCreatingTriggerFiles
        {
            get => isCreatingTriggerFiles;
            private set => this.RaiseAndSetIfChanged(ref isCreatingTriggerFiles, value);
        }

        /// <summary>
        /// Abort for the Trigger Creation Thread.
        /// </summary>
        public void AbortTriggerThread()
        {
            abortTriggerCreationNow = true;
            IsCreatingTriggerFiles = false;
        }

        /// <summary>
        /// This event handler should find the samples we want to make trigger files for
        /// and make them.
        /// </summary>
        public void CreateTriggers(List<BuzzardDataset> selectedDatasets)
        {
            if (selectedDatasets.Count == 0)
            {
                return;
            }

            if (IsCreatingTriggerFiles)
            {
                MessageBox.Show("Already creating trigger files; please wait for the current operation to complete", "Busy",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            RxApp.MainThreadScheduler.Schedule(_ => IsCreatingTriggerFiles = true);

            // Check for a running thread
            abortTriggerCreationNow = false;

            // Create the trigger files, running on a separate thread

            var task1 = Task.Factory.StartNew(() => CreateTriggerFiles(selectedDatasets));

            Task.Factory.ContinueWhenAll(
                new[] { task1 },
                TriggerFilesCreated, // Call this method when all tasks finish.
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext()); // Finish on initial thread.
        }

        private void TriggerFilesCreated(Task[] obj)
        {
            abortTriggerCreationNow = false;
            RxApp.MainThreadScheduler.Schedule(_ => IsCreatingTriggerFiles = false);
        }

        private void CreateTriggerFiles(IReadOnlyCollection<BuzzardDataset> selectedDatasets)
        {
            /*
            // If we're on the wrong thread, then put in
            // a call to this in the correct thread and exit.
            if (!MainWindow.Dispatcher.CheckAccess())
            {
                Action createAction = CreateTriggerFiles;

                MainWindow.Dispatcher.BeginInvoke(createAction, DispatcherPriority.Normal);
                return;
            }
             */

            try
            {
                //
                // From the list of selected Datasets, find
                // the Datasets that didn't get their DMSData
                // from DMS. Then try to resolve it.
                //
                var needsDmsResolved = selectedDatasets.Where(x => !x.DmsData.LockData);

                DatasetManager.Manager.ResolveDms(needsDmsResolved);

                if (abortTriggerCreationNow)
                {
                    MarkAborted(selectedDatasets);
                    return;
                }

                // Update field .IsFile
                foreach (var dataset in selectedDatasets)
                {
                    dataset.TriggerCreationWarning = string.Empty;
                    dataset.ClearDuplicateDatasetFiles();

                    var fiFile = new FileInfo(dataset.FilePath);
                    if (!fiFile.Exists)
                    {
                        var diFolder = new DirectoryInfo(dataset.FilePath);
                        if (diFolder.Exists && dataset.IsFile)
                        {
                            dataset.IsFile = false;
                        }
                    }
                }

                var success = SimulateTriggerCreation(selectedDatasets, out var validDatasets);
                if (!success)
                {
                    return;
                }

                var instrumentGoodDatasets = VerifyDatasetsMatchInstrument(validDatasets);
                if (instrumentGoodDatasets.Count == 0)
                {
                    return;
                }

                // Confirm that the dataset are not changing and are thus safe to create trigger files for
                var stableDatasets = VerifyDatasetsStable(instrumentGoodDatasets);

                if (abortTriggerCreationNow)
                {
                    return;
                }

                // Check that the datasets do not match an already-uploaded dataset with a different name.
                // This returns an IEnumerable that and the file checks are processed on consumption
                var nonDuplicateDatasets = VerifyDatasetsNotDuplicates(stableDatasets);

                foreach (var dataset in nonDuplicateDatasets)
                {
                    if (!abortTriggerCreationNow)
                    {
                        // ReSharper disable once UnusedVariable
                        var triggerFilePath = DatasetManager.CreateTriggerFileBuzzard(dataset, forceSend: true, preview: false);
                    }

                    if (abortTriggerCreationNow && dataset.DatasetStatus != DatasetStatus.TriggerFileSent)
                    {
                        dataset.DatasetStatus = DatasetStatus.TriggerAborted;
                        return;
                    }
                }

                ApplicationLogger.LogMessage(
                    0,
                    "Finished executing create trigger files command.");
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(
                    0,
                    "Exception creating trigger files for the selected datasets", ex);
            }
            finally
            {
                IsCreatingTriggerFiles = false;
            }
        }

        private void MarkAborted(IEnumerable<BuzzardDataset> selectedDatasets)
        {
            foreach (var dataset in selectedDatasets)
            {
                dataset.DatasetStatus = DatasetStatus.TriggerAborted;
            }
        }

        /// <summary>
        /// Creates the xml trigger file for each dataset but does not save it to disk
        /// </summary>
        /// <param name="selectedDatasets"></param>
        /// <param name="validDatasets"></param>
        /// <returns>True if no problems, False if a problem with one or more datasets</returns>
        private bool SimulateTriggerCreation(IReadOnlyCollection<BuzzardDataset> selectedDatasets, out List<BuzzardDataset> validDatasets)
        {
            validDatasets = new List<BuzzardDataset>();
            var datasetsAlreadyInDMS = 0;

            // Simulate trigger file creation to check for errors
            foreach (var dataset in selectedDatasets)
            {
                DatasetManager.CreateTriggerFileBuzzard(dataset, forceSend: true, preview: true);

                if (abortTriggerCreationNow)
                {
                    MarkAborted(selectedDatasets);
                    return false;
                }

                if (dataset.DatasetStatus == DatasetStatus.Pending ||
                    dataset.DatasetStatus == DatasetStatus.ValidatingStable)
                {
                    validDatasets.Add(dataset);
                }

                if (dataset.DatasetStatus == DatasetStatus.DatasetAlreadyInDMS)
                {
                    datasetsAlreadyInDMS++;
                }
            }

            if (datasetsAlreadyInDMS > 0 && datasetsAlreadyInDMS == selectedDatasets.Count)
            {
                // The datasets were already in DMS
                return false;
            }

            var invalidDatasetCount = selectedDatasets.Count - validDatasets.Count - datasetsAlreadyInDMS;
            if (invalidDatasetCount <= 0)
            {
                return true;
            }

            var warningMessage = "Warning, " + invalidDatasetCount;
            if (invalidDatasetCount == 1)
            {
                warningMessage += " dataset has ";
            }
            else
            {
                warningMessage += " datasets have ";
            }

            warningMessage += "validation errors; fix the errors then try again.";

            MessageBox.Show(warningMessage, "Validation Errors", MessageBoxButton.OK, MessageBoxImage.Warning);

            return false;
        }

        private List<BuzzardDataset> VerifyDatasetsStable(IEnumerable<BuzzardDataset> selectedDatasets)
        {
            const int SECONDS_TO_WAIT = 30;

            var stableDatasets = new List<BuzzardDataset>();

            // Values: Dataset exists (file or directory), size (in bytes), number of files
            var datasetsToVerify = new Dictionary<BuzzardDataset, (bool Exists, long Size, int FileCount)>();

            var buzzardDatasets = selectedDatasets.ToList();

            foreach (var dataset in buzzardDatasets)
            {
                var stats = dataset.GetFileStats();
                if (!stats.Exists)
                {
                    dataset.DatasetStatus = DatasetStatus.FileNotFound;
                    continue;
                }

                datasetsToVerify.Add(dataset, stats);

                dataset.DatasetStatus = DatasetStatus.ValidatingStable;
            }

            var startTime = DateTime.UtcNow;
            var nextLogTime = startTime.AddSeconds(2);
            var baseMessage = "Verifying dataset files are unchanged for " + SECONDS_TO_WAIT + " seconds";
            ApplicationLogger.LogMessage(0, baseMessage);

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < SECONDS_TO_WAIT)
            {
                Thread.Sleep(100);

                if (abortTriggerCreationNow)
                {
                    MarkAborted(buzzardDatasets);
                    ApplicationLogger.LogMessage(0, "Aborted verification of stable dataset files");
                    return stableDatasets;
                }

                if (DateTime.UtcNow >= nextLogTime)
                {
                    nextLogTime = nextLogTime.AddSeconds(2);
                    var secondsRemaining = (int)Math.Round(SECONDS_TO_WAIT - DateTime.UtcNow.Subtract(startTime).TotalSeconds);
                    ApplicationLogger.LogMessage(0, baseMessage + "; " + secondsRemaining + " seconds remain");
                }
            }

            foreach (var entry in datasetsToVerify)
            {
                var stats = entry.Key.GetFileStats();
                if (stats.Exists)
                {
                    if (stats.Equals(entry.Value))
                    {
                        stableDatasets.Add(entry.Key);
                    }
                    else if (FileBlockingChecks.DatasetHasAcquisitionLock(entry.Key.FilePath))
                    {
                        entry.Key.DatasetStatus = DatasetStatus.FileSizeChanged;
                    }
                    else
                    {
                        entry.Key.DatasetStatus = DatasetStatus.FileSizeChanged;
                    }
                }
                else
                {
                    entry.Key.DatasetStatus = DatasetStatus.FileNotFound;
                }
            }

            foreach (var dataset in stableDatasets)
            {
                dataset.DatasetStatus = DatasetStatus.Pending;
            }

            return stableDatasets;
        }

        // Return an IEnumerable and return data with yield return so that trigger files are created as this verification occurs, rather than no trigger files being created until all datasets are checked.
        private IEnumerable<BuzzardDataset> VerifyDatasetsNotDuplicates(IEnumerable<BuzzardDataset> datasets)
        {
            foreach (var dataset in datasets)
            {
                ApplicationLogger.LogMessage(0, $"Verifying non-duplicate files for dataset {dataset.DmsData.DatasetName}...");

                List<FileHashInfo> hashes;

                try
                {
                    hashes = FileHashChecks.GetHashedFiles(dataset.FilePath);
                }
                catch (Exception ex)
                {
                    // Likely caused by cyclic redundancy check error or a moved/renamed file.
                    ApplicationLogger.LogMessage(LogLevel.Error, $"Error reading dataset '{dataset.DmsData.DatasetName}': {ex.Message}", ex);
                    dataset.DatasetStatus = DatasetStatus.FileReadError;
                    continue;
                }

                if (hashes.Count == 0)
                {
                    yield return dataset;
                    continue;
                }

                // Compare the hashes against DMS...
                var matches = DMSDataAccessor.Instance.GetMatchingDatasetFiles(hashes.Select(x => x.Sha1Hash).ToList()).ToList();
                if (matches.Count == 0)
                {
                    yield return dataset;
                    continue;
                }

                // Check the returned hashes...
                // TODO: At some point we should check the file size first
                dataset.SetDuplicateDatasetFiles(matches);
                dataset.DatasetStatus = DatasetStatus.TriggerAbortedDuplicateFiles;
            }
        }

        private List<BuzzardDataset> VerifyDatasetsMatchInstrument(IEnumerable<BuzzardDataset> datasets)
        {
            var verifiedDatasets = new List<BuzzardDataset>();

            foreach (var dataset in datasets)
            {
                var matched = DatasetInstrumentChecks.DoFilesMatchInstrument(dataset, out var message);
                if (matched)
                {
                    verifiedDatasets.Add(dataset);
                    continue;
                }

                dataset.DatasetInstrumentMismatchMessage = message;
                dataset.DatasetStatus = DatasetStatus.TriggerAbortedDatasetInstrumentMismatch;
            }

            return verifiedDatasets;
        }
    }
}
