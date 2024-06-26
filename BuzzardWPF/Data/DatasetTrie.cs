﻿using System;
using System.Collections.Generic;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.Data.Trie;

namespace BuzzardWPF.Data
{
    /// <summary>
    /// Used to track Requested Runs (instruments runs) in DMS
    /// </summary>
    public class DatasetTrie
    {
        // Ignore Spelling: trie

        private ITrieNode rootNode = new TrieNodeString();
        private Dictionary<int, DMSData> requestIDToDMSMap = new Dictionary<int, DMSData>();
        private DateTime lastLoadTime = DateTime.MinValue;

        public int Count => requestIDToDMSMap.Count;

        // ReSharper disable once UnusedMember.Global

        /// <summary>
        /// Clear the root node and request ID to DMS data map
        /// </summary>
        public void Clear()
        {
            rootNode.Clear();
            requestIDToDMSMap.Clear();
        }

        private void ClearForLoad()
        {
            if (lastLoadTime.Date < DateTime.Now.Date)
            {
                // Clear these out once per day.
                rootNode = new TrieNodeString();
                requestIDToDMSMap = new Dictionary<int, DMSData>();
            }
            else
            {
                rootNode.Clear();
                requestIDToDMSMap.Clear();
            }
        }

        public void RemoveEmptyNodes()
        {
            rootNode.RemoveEmptyNodes();
        }

        /// <summary>
        /// Replaces existing data with new data, unless <paramref name="newData"/> is an empty enumerable (then this is a no-op).
        /// </summary>
        /// <param name="newData">New data to place in the trie</param>
        /// <returns>True if data was changed</returns>
        public bool LoadData(IEnumerable<DMSData> newData)
        {
            var loadTime = DateTime.Now;
            var firstItem = true;
            foreach (var datum in newData)
            {
                if (firstItem)
                {
                    // Avoid clearing the Trie when no data was returned.
                    ClearForLoad();
                    firstItem = false;
                }

                AddData(datum);
            }

            if (!firstItem)
            {
                // Only do this if we added data to the Trie
                RemoveEmptyNodes();
            }

            lastLoadTime = loadTime;

            return !firstItem;
        }

        /// <summary>
        /// Adds data to the trie.
        /// </summary>
        /// <param name="data"></param>
        public void AddData(DMSData data)
        {
            // Store all text as lower case
            rootNode.AddDataset(data.RequestName, data.RequestID);

            if (!requestIDToDMSMap.ContainsKey(data.RequestID))
            {
                requestIDToDMSMap.Add(data.RequestID, data);
            }
        }

        /// <summary>
        /// Finds the dataset data that closest resembles the dataset name.
        /// </summary>
        /// <exception cref="DatasetTrieException">Thrown if the dataset name does not exist in the trie, or if the dataset name cannot resolve the data.</exception>
        /// <param name="datasetName">Name of the dataset to search with.</param>
        /// <returns>DMS Data if it exists.  Exceptions are thrown if the dataset does not.</returns>
        public DMSData FindData(string datasetName)
        {
            var requestId = rootNode.FindRequestIdForName(datasetName, out var searchDepth);
            if (requestId < 0 || !requestIDToDMSMap.TryGetValue(requestId, out var dmsData) || !datasetName.StartsWith(dmsData.RequestName, StringComparison.OrdinalIgnoreCase))
            {
                throw new DatasetTrieException("Could not resolve the dataset name. The dataset is just not available in this trie.", searchDepth, datasetName);
            }

            return dmsData;
        }
    }
}
