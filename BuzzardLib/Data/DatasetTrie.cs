using System.Collections.Generic;
using LcmsNetSDK.Data;

namespace BuzzardLib.Data
{
    /// <summary>
    /// Used to track Requested Runs (instruments runs) in DMS
    /// </summary>
    public class DatasetTrie
    {
        private const bool DEFAULT_IGNORE_CASE = true;

        #region Attributes
        private readonly TrieNode                       m_root;
        private readonly Dictionary<int, DMSData>  m_requestIDToDMS;
        private readonly bool m_IgnoreCase;
        #endregion

        public DatasetTrie()
            : this(DEFAULT_IGNORE_CASE)
        {
        }

        public DatasetTrie(bool ignoreCase)
        {
            m_root = new TrieNode();
            m_requestIDToDMS = new Dictionary<int, DMSData>();
            m_IgnoreCase = ignoreCase;
        }
        /// <summary>
        /// Adds data to the trie.
        /// </summary>
        /// <param name="data"></param>
        public void AddData(DMSData data)
        {
            if (m_IgnoreCase)
            {
                // Store all text as lower case
                AddData(m_root, data.RequestName.ToLower(), data);
            }
            else
            {
                AddData(m_root, data.RequestName, data);
            }
        }
        public void Clear()
        {
            RemoveNodes(m_root);
            m_requestIDToDMS.Clear();
        }
        private void RemoveNodes(TrieNode node)
        {
            foreach (var edge in node.Edges.Values)
            {
                RemoveNodes(edge);
            }
            node.Edges.Clear();
        }

        /// <summary>
        /// Adds a dataset name to the trie.
        /// </summary>
        /// <param name="node">Node to add to.</param>
        /// <param name="datasetName">Dataset name to add</param>
        /// <param name="data">Data to add at leaf</param>
        private void AddData(TrieNode node, string datasetName, DMSData data)
        {

            if (string.IsNullOrWhiteSpace(datasetName))
            {
                node.DmsData = data;
            }
            else
            {
                var key = datasetName[0];
                var hasEdge = node.Edges.ContainsKey(key);

                if (hasEdge)
                {
                    AddData(node.Edges[key],
                            datasetName.Substring(1),
                            data);
                }
                else
                {
                    var newNode = new TrieNode();
                    node.Edges.Add(key, newNode);
                    AddData(newNode, datasetName.Substring(1), data);
                }
            }

            if (!m_requestIDToDMS.ContainsKey(data.RequestID))
                m_requestIDToDMS.Add(data.RequestID, null);

            m_requestIDToDMS[data.RequestID] = data;
        }

        /// <summary>
        /// Finds the dataset data that for the given request key
        /// </summary>
        public DMSData FindData(int requestID)
        {
            if (!m_requestIDToDMS.ContainsKey(requestID))
                throw new DatasetTrieException("Could not resolve the Request ID. The dataset is just not available.");

            return m_requestIDToDMS[requestID];
        }

        /// <summary>
        /// Finds the dataset data that closest resembles the dataset name.
        /// </summary>
        /// <exception cref="DatasetTrieException">Thrown if the dataset name does not exist in the trie, or if the dataset name cannot resolve the data.</exception>
        /// <param name="datasetName">Name of the dataset to search with.</param>
        /// <returns>DMS Data if it exists.  Exceptions are thrown if the dataset does not.</returns>
        public DMSData FindData(string datasetName)
        {
            if (m_IgnoreCase)
            {
                // Dataset names were stored lowercase; must convert to lowercase when calling FindData
                return FindData(m_root, datasetName.ToLower(), datasetName, 0);
            }

            return FindData(m_root, datasetName, string.Copy(datasetName), 0);
        }

        private DMSData FindData(TrieNode node, string datasetNamePart, string fullDatasetName, int searchDepth)
        {
            if (string.IsNullOrWhiteSpace(datasetNamePart))
            {
                // This means that we are out of our search string.
                if (node.DmsData != null)
                {
                    return node.DmsData;
                }
                throw new DatasetTrieException("Could not resolve the dataset name.  The dataset is just not available in this trie.");
            }
            // This means we still have a string to search with...

            var key = datasetNamePart[0];
            if (node.Edges.Count < 1)
            {
                // Found a request whose full name matches the start of the dataset file on the instrument (but not the entire filename)
                // Return this as the best matching active request
                return node.DmsData;
            }

            // more datasets exist past this point...
            var hasKey = node.Edges.ContainsKey(key);
            if (hasKey)
            {
                // This means that we have the edge we need
                // we need to keep following.
                return FindData(node.Edges[key], datasetNamePart.Substring(1), fullDatasetName, searchDepth + 1);
            }

            // this means that we have nodes...but we don't have the key...
            throw new DatasetTrieException("Could not resolve the dataset name.  The dataset is just not available in this trie.", searchDepth, fullDatasetName);
        }
    }

    public class TrieNode
    {
        public TrieNode()
        {
            DmsData = null;
            Edges = new Dictionary<char, TrieNode>();
        }
        public DMSData DmsData
        {
            get;
            set;
        }
        public Dictionary<char, TrieNode> Edges
        {
            get;
            set;
        }
    }
}
