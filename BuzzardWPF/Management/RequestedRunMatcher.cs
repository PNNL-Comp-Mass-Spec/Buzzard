using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using BuzzardWPF.Data;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class RequestedRunMatcher : ReactiveObject, IDisposable
    {
        public const int RequestedRunsUpdateIntervalMinutes = 10;

        public RequestedRunMatcher()
        {
            requestedRunsUpdateTimer = new Timer(RequestedRunsUpdateTimer_Tick, this, TimeSpan.FromMinutes(RequestedRunsUpdateIntervalMinutes), TimeSpan.FromMinutes(RequestedRunsUpdateIntervalMinutes));
        }

        /// <summary>
        /// Trie that holds requested run names from DMS.
        /// </summary>
        private readonly DatasetTrie requestedRunTrie = new DatasetTrie();
        private DateTime requestedRunsLastUpdated;
        private readonly object lockObject = new object();

        /// <summary>
        /// This timer will call DatasetManager.LoadRequestedRuns every 10 minutes
        /// </summary>
        private readonly Timer requestedRunsUpdateTimer;

        public TriggerFileMonitor TriggerMonitor => TriggerFileMonitor.Instance;

        public bool IsLoading { get; private set; }

        public DateTime RequestedRunsLastUpdated
        {
            get => requestedRunsLastUpdated;
            private set => this.RaiseAndSetIfChanged(ref requestedRunsLastUpdated, value);
        }

        public async Task LoadRequestedRunsCache()
        {
            lock (lockObject)
            {
                if (IsLoading)
                {
                    return;
                }

                IsLoading = true;
            }

            await Task.Run(LoadDmsRequestedRuns).ConfigureAwait(false);

            lock (lockObject)
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads active requested runs from DMS, and re-loads the trigger file history
        /// </summary>
        public void LoadDmsRequestedRuns()
        {
            var currentTask = "Initializing";

            try
            {
                // Load the samples (essentially requested runs) from DMS
                currentTask = "Retrieving samples (requested runs) from DMS";
                var samples = DMS_DataAccessor.Instance.LoadDMSRequestedRuns();

                currentTask = "Populating mRequestedRunTrie";

                bool requestedRunsUpdated;
                lock (requestedRunTrie)
                {
                    requestedRunsUpdated = requestedRunTrie.LoadData(samples);
                }

                // We can use this to get an idea if any datasets already have trigger files that were sent.
                currentTask = "Examine the trigger file folder";
                TriggerMonitor.ReloadTriggerFileStates(ref currentTask);

                if (requestedRunsUpdated)
                {
                    currentTask = "Raise event DatasetsLoaded";
                    var lastUpdatedTime = DateTime.Now;
                    RxApp.MainThreadScheduler.Schedule(() => RequestedRunsLastUpdated = lastUpdatedTime);
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(LogLevel.Error, "Error loading data, task " + currentTask + ": " + ex.Message, ex);
            }

            // Force a garbage collection to try to clean up the freed memory from the trie
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(int.MaxValue, GCCollectionMode.Forced, true, true);
        }

        public DMSData MatchDatasetName(FileInfo fiDataset, out string datasetName)
        {
            const int SEARCH_DEPTH_AMBIGUOUS_MATCH = 5;

            var fileName = Path.GetFileNameWithoutExtension(fiDataset.Name);
            datasetName = fileName;
            DMSData data = null;

            lock (requestedRunTrie)
            {
                try
                {
                    data = requestedRunTrie.FindData(fileName);
                }
                catch (DatasetTrieException ex)
                {
                    // Not found
                    // Get the path name of the directory, then use that as the "search string for dms"
                    if (fiDataset.Directory != null)
                    {
                        fileName = Path.GetFileName(fiDataset.Directory.Name);

                        try
                        {
                            data = requestedRunTrie.FindData(fileName);
                        }
                        catch (DatasetTrieException)
                        {
                            // No match to the folder name
                            if (ex.SearchDepth >= SEARCH_DEPTH_AMBIGUOUS_MATCH)
                            {
                                throw new DatasetTrieException(ex.Message, ex.SearchDepth, ex.DatasetName, ex);
                            }

                            throw;
                        }

                        // Match found to the directory name; update the dataset name
                        datasetName = fileName;
                    }
                }
            }

            if (data == null)
            {
                // The Trie didn't find the dataset but for some reason didn't throw an exception. Throw one here to avoid a NullReferenceException.
                throw new DatasetTrieException("Dataset not found in Trie.", 0, fileName);
            }

            return data;
        }

        private async void RequestedRunsUpdateTimer_Tick(object state)
        {
            // Load active requested runs from DMS
            await LoadRequestedRunsCache().ConfigureAwait(false);
        }

        public void Dispose()
        {
            requestedRunsUpdateTimer?.Dispose();
        }
    }
}
