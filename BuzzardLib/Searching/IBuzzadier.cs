﻿using System;

namespace BuzzardLib.Searching
{
    /// <summary>
    /// Interface for monitoring searching a directory.
    /// </summary>
    public interface IBuzzadier
    {
        /// <summary>
        /// Fired when a dataset is found.
        /// </summary>
        event EventHandler<DatasetFoundEventArgs> DatasetFound;
        
        /// <summary>
        /// Fired when a search was completed.
        /// </summary>
        event EventHandler SearchComplete;

        /// <summary>
        /// Fired when we start searching for datasets
        /// </summary>
        event EventHandler SearchStarted;

        /// <summary>
        /// Fired when a search was stopped.
        /// </summary>
        event EventHandler SearchStopped;

        /// <summary>
        /// Fired when an error occurs
        /// </summary>
        event EventHandler<ErrorEventArgs> ErrorEvent;

        /// <summary>
        /// Call to start searching.
        /// </summary>
        /// <param name="config"></param>
        void Search(SearchConfig config);

        /// <summary>
        /// Call to stop searching.
        /// </summary>
        void Stop();
    }
}
