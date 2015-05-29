using System;

namespace BuzzardLib.Searching
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
        /// <param name="parentFolderPathRelative">Relative </param>
        /// <param name="config">Search config options</param>
        public DatasetFoundEventArgs(string datasetPath, string parentFolderPathRelative, SearchConfig config)
        {
            Path = datasetPath;
            RelativeParentFolderPath = parentFolderPathRelative;
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
        public string RelativeParentFolderPath
        {
            get;
            private set;
        }

    }
}
