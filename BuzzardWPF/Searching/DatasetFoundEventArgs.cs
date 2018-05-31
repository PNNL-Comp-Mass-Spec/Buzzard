using System;

namespace BuzzardWPF.Searching
{
    /// <summary>
    /// Arguments when a dataset is found.
    /// </summary>
    public class DatasetFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="datasetPath">Full path to the dataset file or folder</param>
        /// <param name="subfolderPathRelative">Subfolder path, relative to the base folder we searched from</param>
        /// <param name="config">Search config options</param>
        public DatasetFoundEventArgs(string datasetPath, string subfolderPathRelative, SearchConfig config)
        {
            Path = datasetPath;
            CaptureSubfolderPath = subfolderPathRelative;
            CurrentSearchConfig = config;
        }

        /// <summary>
        /// Current search configuration
        /// </summary>
        public SearchConfig CurrentSearchConfig
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full dataset path found by a Buzzadier.
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the relative storage path of the parent folder for the found dataset
        /// </summary>
        public string CaptureSubfolderPath
        {
            get;
            private set;
        }

    }
}
