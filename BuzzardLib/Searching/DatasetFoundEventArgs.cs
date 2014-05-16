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
        /// <param name="path">Path of dataset that was found.</param>
        public DatasetFoundEventArgs(string path)
        {
            Path = path;
        }
        /// <summary>
        /// Gets the path found by a Buzzadier.
        /// </summary>
        public string Path
        {
            get;
            private set;
        }
    }
}
