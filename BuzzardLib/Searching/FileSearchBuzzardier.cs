using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using BuzzardLib.IO;
using LcmsNet.Method;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;

namespace BuzzardLib.Searching
{
    /// <summary>
    /// Class that searches for a set of files.
    /// </summary>
    public class FileSearchBuzzardier : IBuzzadier
    {
        #region Events

        /// <summary>
        /// Fired when a dataset is found.
        /// </summary>
        public event EventHandler<DatasetFoundEventArgs> DatasetFound;

        /// <summary>
        /// Fired when a search was completed.
        /// </summary>
        public event EventHandler SearchComplete;

        /// <summary>
        /// Fired when a search was started.
        /// </summary>
        public event EventHandler SearchStarted;

        /// <summary>
        /// Fired when a search was stopped.
        /// </summary>
        public event EventHandler SearchStopped;

        #endregion

        #region "Enums"

        private enum DatasetType
        {
            File = 0,
            Folder = 1
        }

        #endregion

        #region Members
        /// <summary>
        /// Thread for searching.
        /// </summary>
        private Thread m_thread;
        /// <summary>
        /// Flag inidicating whether to search
        /// </summary>
        private volatile bool m_keepSearching;

        private readonly Dictionary<string, classInstrumentInfo> mInstrumentInfo;

        #endregion

        #region Searching and Threading Methods

        /// <summary>
        ///  Constructor
        /// </summary>
        public FileSearchBuzzardier(Dictionary<string, classInstrumentInfo> instrumentInfo)
        {
            mInstrumentInfo = instrumentInfo;
        }

        /// <summary>
        /// Stops searching
        /// </summary>
        public void Stop()
        {
            if (m_thread == null)
            {
                m_keepSearching = false;
                return;
            }

            try
            {
                m_thread.Abort();
                m_thread.Join(100);

                if (SearchStopped != null)
                {
                    SearchStopped(this, null);
                }
            }
            catch (ThreadAbortException)
            {
                // pass on abort
            }
            catch (TimeoutException)
            {
                // pass
            }
            finally
            {
                m_keepSearching = false;
                m_thread = null;
            }
        }

        /// <summary>
        /// Searches 
        /// </summary>
        /// <param name="config"></param>
        public void Search(SearchConfig config)
        {
            if (m_thread != null)
            {
                try
                {
                    Stop();
                }
                catch (Exception)
                {
                    return;
                }
                
            }

            var start = new ParameterizedThreadStart(Search);
            m_thread = new Thread(start);
            m_keepSearching = true;
            m_thread.Start(config);
        }

        private void Search(object objectConfig)
        {

            try
            {
                if (SearchStarted != null)
                {
                    SearchStarted(this, null);
                }

                var config = objectConfig as SearchConfig;
                if (config == null)
                {
                    m_keepSearching = false;
                    ReportError("The search configuration defined in FileSearchBuzzardier is invalid; disabling search");
                    return;
                }

                // If we have a an ending date, then
                // we can use the less than operator
                // on the next day to make sure the
                // file's DateTime is on or before the
                // date specificed.
                var endDate = DateTime.MaxValue;
                if (config.EndDate != null)
                {
                    endDate = config.EndDate.Value.Date.AddDays(1).Date;
                }

                var shouldSearchBelow = (config.SearchDepth == SearchOption.AllDirectories);
                var extensionFilter = string.Format("*{0}", config.FileExtension);

                if (string.IsNullOrWhiteSpace(config.DirectoryPath))
                {
                    ReportError("The search directory is empty");
                    return;
                }

                var diBaseFolder = new DirectoryInfo(config.DirectoryPath);
                if (!diBaseFolder.Exists)
                {
                    ReportError("Folder not found: " + config.DirectoryPath);
                    return;
                }

                string expectedBaseFolderPath;

                var baseFolderValidator = new InstrumentFolderValidator(mInstrumentInfo);

                if (!baseFolderValidator.ValidateBaseFolder(diBaseFolder, out expectedBaseFolderPath))
                {
                    if (string.IsNullOrWhiteSpace(baseFolderValidator.ErrorMessage))
                        ReportError("Base folder not valid for this instrument; should be " + expectedBaseFolderPath);
                    else
                        ReportError(baseFolderValidator.ErrorMessage);
                    return;
                }

                var folderNameFilterLCase = string.Empty;

                if (!string.IsNullOrWhiteSpace(config.FolderNameFilter))
                    folderNameFilterLCase = config.FolderNameFilter.ToLower();
                
                // Breadth first search across directories as to make it fast and responsive to a listening UI
                var m_paths = new Queue<string>();
                m_paths.Enqueue(diBaseFolder.FullName);

                while (m_paths.Count > 0 && m_keepSearching)
                {
                    var path = m_paths.Dequeue();
                    var currentDirectory = new DirectoryInfo(path);

                    var fileAndFolderPaths = new List<KeyValuePair<DatasetType, FileSystemInfo>>();

                    try
                    {
                        var processFolder = true;

                        if (!string.IsNullOrWhiteSpace(folderNameFilterLCase))
                        {
                            if (!currentDirectory.FullName.ToLower().Contains(folderNameFilterLCase))
                                processFolder = false;
                        }

                        if (processFolder)
                        {
                            foreach (
                                var file in currentDirectory.GetFiles(extensionFilter, SearchOption.TopDirectoryOnly))
                            {
                                fileAndFolderPaths.Add(new KeyValuePair<DatasetType, FileSystemInfo>(DatasetType.File,
                                                                                                     file));
                            }

                            if (config.MatchFolders)
                            {
                                foreach (
                                    var subDirectory in
                                        currentDirectory.GetDirectories(extensionFilter, SearchOption.TopDirectoryOnly))
                                {
                                    fileAndFolderPaths.Add(new KeyValuePair<DatasetType, FileSystemInfo>(
                                                               DatasetType.Folder, subDirectory));
                                }
                            }
                        }

                        if (shouldSearchBelow)
                        {
                            foreach (var subDirectory in currentDirectory.GetDirectories())
                            {
                                m_paths.Enqueue(subDirectory.FullName);
                            }
                        }

                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        ReportError(string.Format("Could not access the path {0}", path), ex);
                        continue;
                    }

                    foreach (var datasetEntry in fileAndFolderPaths)
                    {

                        try
                        {
                            if (!string.IsNullOrWhiteSpace(config.FilenameFilter))
                            {
                                if (!datasetEntry.Value.Name.ToLower().Contains(config.FilenameFilter.ToLower()))
                                    continue;
                            }

                            DateTime creationDate;
                            DateTime lastWriteDate;
                            double datasetSizeKB = 0;
                            string parentFolderPath;
                            if (datasetEntry.Key == DatasetType.File)
                            {
                                var datasetFile = (FileInfo)datasetEntry.Value;
                                datasetSizeKB = (datasetFile.Length / 1024.0);
                                creationDate = datasetFile.CreationTime;
                                lastWriteDate = datasetFile.LastAccessTime;
                                parentFolderPath = TriggerFileTools.GetCaptureSubfolderPath(diBaseFolder,
                                                                                                datasetFile);
                            }
                            else
                            {
                                var datasetFolder = (DirectoryInfo)datasetEntry.Value;
                                datasetSizeKB +=
                                    datasetFolder.GetFiles("*", SearchOption.AllDirectories)
                                        .Sum(file => file.Length / 1024.0);
                                creationDate = datasetFolder.CreationTime;
                                lastWriteDate = datasetFolder.LastAccessTime;
                                parentFolderPath = TriggerFileTools.GetCaptureSubfolderPath(diBaseFolder,
                                                                                                datasetFolder);
                            }

                            if (datasetSizeKB < config.MinimumSizeKB)
                            {
                                // Dataset too small
                                continue;
                            }

                            // If the file predates the date-range we want, skip it.
                            if (config.StartDate != null)
                            {
                                if (config.StartDate > creationDate && config.StartDate > lastWriteDate)
                                    continue;
                            }

                            // If the file postdates the date-range we want, skip it.
                            if (config.EndDate != null)
                            {
                                if (endDate < creationDate || endDate < lastWriteDate)
                                    continue;
                            }

                            if (DatasetFound != null)
                            {
                                DatasetFound(this,
                                             new DatasetFoundEventArgs(datasetEntry.Value.FullName, parentFolderPath, config));
                            }

                        }
                        catch
                        {
                            // File access error; ignore this file or folder
                        }
                    }
                }

                if (SearchComplete != null)
                {
                    SearchComplete(this, null);
                }

            }
            catch (ThreadAbortException)
            {
                ReportError("Aborted Buzzardier.Search");
            }
            catch (Exception ex)
            {
                ReportError("Exception in Buzzardier.Search: " + ex.Message, ex);
            }

        }

        private void ReportError(string errorMessage, Exception ex = null)
        {
            if (ex == null)
                classApplicationLogger.LogError(0, errorMessage);
            else
                classApplicationLogger.LogError(0, errorMessage, ex);
        }

        #endregion
    }
}
