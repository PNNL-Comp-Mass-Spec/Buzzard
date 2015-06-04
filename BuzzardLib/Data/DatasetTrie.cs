using System.Collections.Generic;
using LcmsNetDataClasses;

namespace BuzzardLib.Data
{
    /// <summary>
    /// Used to track Requested Runs (instruments runs) in DMS
    /// </summary>
    public class DatasetTrie
	{
        private const bool DEFAULT_IGNORE_CASE = true;

		#region Attributes
		private readonly classTrieNode					m_root;
		private readonly Dictionary<int, classDMSData>	m_requestIDToDMS;
        private readonly bool m_IgnoreCase;
		#endregion

        public DatasetTrie()
            : this(DEFAULT_IGNORE_CASE)
        {
        }

        public DatasetTrie(bool ignoreCase)
        {
            m_root = new classTrieNode();
            m_requestIDToDMS = new Dictionary<int, classDMSData>();
            m_IgnoreCase = ignoreCase;
        }
        /// <summary>
        /// Adds data to the trie.
        /// </summary>
        /// <param name="data"></param>
        public void AddData(classDMSData data)
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
        private void RemoveNodes(classTrieNode node)
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
        private void AddData(classTrieNode node, string datasetName, classDMSData data)
        {
          
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                node.DmsData = data;
            }
            else
            {
                var key     = datasetName[0];
                var hasEdge = node.Edges.ContainsKey(key);

                if (hasEdge)
                {
                    AddData(node.Edges[key],
                            datasetName.Substring(1),
                            data);
                }
                else
                {
                    var newNode = new classTrieNode();
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
		public classDMSData FindData(int requestID)
		{
			if (!m_requestIDToDMS.ContainsKey(requestID))
				throw new KeyNotFoundException("Could not resolve the Request ID.  The dataset is just not available.");

			return m_requestIDToDMS[requestID];
		}

        /// <summary>
        /// Finds the dataset data that closest resembles the dataset name.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if the dataset name does not exist in the trie, or if the dataset name cannot resolve the data.</exception>
        /// <param name="datasetName">Name of the dataset to search with.</param>
        /// <returns>DMS Data if it exists.  Exceptions are thrown if the dataset does not.</returns>
        public classDMSData FindData(string datasetName)
        {
            if (m_IgnoreCase)
            {
                // Dataset names were stored lowercase; must convert to lowercase when calling FindData
                return FindData(m_root, datasetName.ToLower());
            }
            else
            {
                return FindData(m_root, datasetName);
            }
        }

        private classDMSData FindData(classTrieNode node, string datasetName)
        {
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                // This means that we are out of our search string.
                if (node.DmsData != null)
                {
                    return node.DmsData;
                }
                throw new KeyNotFoundException("Could not resolve the dataset name.  The dataset is just not available in this trie.");
            }
            // This means we still have a string to search with...

            var key = datasetName[0];
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
                return FindData(node.Edges[key], datasetName.Substring(1));
            }
            // this means that we have nodes...but we don't have the key...
            throw new KeyNotFoundException("Could not resolve the dataset name.  The dataset is just not available in this trie.");
        }
    }

    public class classTrieNode
    {
        public classTrieNode()
        {
            DmsData = null;
            Edges   = new Dictionary<char, classTrieNode>();
        }
        public classDMSData DmsData
        {
            get;
            set;
        }
        public Dictionary<char, classTrieNode> Edges
        {
            get;
            set;
        }
    }
}
