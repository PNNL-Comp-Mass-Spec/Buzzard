using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF.Searching
{
    /// <summary>
    /// Class that searches for a set of files.
    /// </summary>
    public class FileSearchBuzzardier: IBuzzadier
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
        /// Fired when a search was stopped.
        /// </summary>
        public event EventHandler SearchStopped;
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
        #endregion
        
        #region Searching and Threading Methods
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
                m_thread        = null;
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
                Stop();
            }

            ParameterizedThreadStart start  = new ParameterizedThreadStart(Search);
            m_thread                        = new Thread(start);
            m_keepSearching                 = true;
            m_thread.Start(config);
        }

        private void Search(object objectConfig)
        {
            SearchConfig config = objectConfig as SearchConfig;
            if (config == null)
            {
                m_keepSearching = false;
                throw new InvalidCastException("The search configuration was invalid.");
            }

			// If we have a an ending date, then
			// we can use the less than opperator
			// on the next day to make sure the a
			// file's DateTime is on or before the
			// date specificed.
			DateTime endDate = DateTime.MaxValue;
			if(config.EndDate != null)
			{
				endDate = config.EndDate.Value.AddDays(1).Date;
			}

            bool    shouldSearchBelow   = (config.Option == SearchOption.AllDirectories);
            string  searchFilter        = string.Format("*{0}", config.FileExtension);

            // Breadth first search across directories as to make it fast and responsive to a listening UI
            Queue<string> m_paths = new Queue<string>();
            m_paths.Enqueue(config.DirectoryPath);           
            while (m_paths.Count > 0 && m_keepSearching)
            {
                string path         = m_paths.Dequeue();
                string absolutePath = Path.GetFullPath(path);

                List<string> files = new List<string>();
				List<string> directories = new List<string>();
                try
                {
                    files		= Directory.GetFiles(absolutePath, searchFilter, SearchOption.TopDirectoryOnly).ToList();
					directories = Directory.GetDirectories(absolutePath, searchFilter, SearchOption.TopDirectoryOnly).ToList();
					files.AddRange(directories);
                    
					if (shouldSearchBelow)
                    {
                        string[] subDirectories = Directory.GetDirectories(absolutePath);

                        foreach (string directory in subDirectories)
                        {
							if (!directories.Contains(directory))
								m_paths.Enqueue(directory);
                        }
                    }
                }
                catch(UnauthorizedAccessException ex)
                {
                    classApplicationLogger.LogError(0, 
                                                string.Format("Could not access the path {0}.", 
                                                    absolutePath), ex);
                    continue;
                }

                foreach (string file in files)
                {
                    if (DatasetFound != null)
                    {
                        string fullFilePath = Path.GetFullPath(file);

						// If we're filtering data based on a date range, then
						// do so.
						if (config.StartDate != null || config.EndDate != null)
						{
							DateTime creationDate;
							DateTime lastWriteDate;
							try
							{
								if (File.Exists(fullFilePath))
								{
									creationDate	= File.GetCreationTime(fullFilePath);
									lastWriteDate	= File.GetLastWriteTime(fullFilePath);
								}
								else
								{
									creationDate	= Directory.GetCreationTime(fullFilePath);
									lastWriteDate	= Directory.GetLastWriteTime(fullFilePath);
								}
							}
							catch
							{
								// If we can't access something as simple as when the file
								// was created or writen too, then I'm not so sure we'll
								// be able to use it to create a Dataset.
								// - FCT
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
						}

                        DatasetFound(this, new DatasetFoundEventArgs(fullFilePath));
                    }
                }
            }

            if (SearchComplete != null)
            {
                SearchComplete(this, null);
            }
        }
        #endregion
    }    
}
