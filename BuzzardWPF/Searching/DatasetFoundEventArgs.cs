using System;

namespace BuzzardWPF.Searching
{
    /// <summary>
    /// Arguments when a dataset is found.
    /// </summary>
    public class DatasetFoundEventArgs : EventArgs
    {
        // Ignore Spelling: Buzzadier

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="datasetPath">Full path to the dataset file or folder</param>
        /// <param name="subdirectoryPathRelative">Subdirectory path, relative to the base folder we searched from</param>
        /// <param name="config">Search config options</param>
        public DatasetFoundEventArgs(string datasetPath, string subdirectoryPathRelative, SearchConfig config)
        {
            Path = datasetPath;
            CaptureSubfolderPath = subdirectoryPathRelative;
            CurrentSearchConfig = config;
        }

        /// <summary>
        /// Current search configuration
        /// </summary>
        public SearchConfig CurrentSearchConfig
        {
            get;
        }

        /// <summary>
        /// Gets the full dataset path found by a Buzzadier.
        /// </summary>
        public string Path
        {
            get;
        }

        /// <summary>
        /// Gets the relative storage path of the parent folder for the found dataset
        /// </summary>
        public string CaptureSubfolderPath
        {
            get;
        }
    }
}
