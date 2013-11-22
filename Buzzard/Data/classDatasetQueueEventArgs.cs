using System;

namespace Buzzard.Data
{
    /// <summary>
    /// Event arguments when the status of a dataset changes.
    /// </summary>
    public class classDatasetQueueEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dataset"></param>
        public classDatasetQueueEventArgs(classDataset dataset,
                                          DatasetQueueType from,
                                          DatasetQueueType to
                                          )
        {
            Dataset     = dataset;
            QueueFrom   = from;
            QueueTo     = to;
        }
        /// <summary>
        /// Gets the dataset associated with
        /// </summary>
        public classDataset Dataset
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets or sets the queue the dataset came from.
        /// </summary>
        public DatasetQueueType QueueFrom
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets or sets the queue the dataset was sent to.
        /// </summary>
        public DatasetQueueType QueueTo
        {
            get;
            private set;
        }
    }
}
