using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LcmsNetDataClasses;

namespace BuzzardWPF.Data
{
    public class DatasetTrie
	{
		#region Attributes
		private classTrieNode					m_root;
		private Dictionary<int, classDMSData>	m_requestIDToDMS;
		#endregion

		public DatasetTrie()
        {
            m_root = new classTrieNode();
			m_requestIDToDMS = new Dictionary<int, classDMSData>();
        }
        /// <summary>
        /// Adds data to the trie.
        /// </summary>
        /// <param name="data"></param>
        public void AddData(classDMSData data)
        {
            AddData(m_root, data.RequestName, data);
        }
        public void Clear()
        {
            RemoveNodes(m_root);
			m_requestIDToDMS.Clear();
        }
        private void RemoveNodes(classTrieNode node)
        {
            foreach (classTrieNode edge in node.Edges.Values)
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
            if (string.IsNullOrEmpty(datasetName))
            {
                node.DmsData = data;
            }
            else
            {
                char key     = datasetName[0];
                bool hasEdge = node.Edges.ContainsKey(key);

                if (hasEdge)
                {
                    AddData(node.Edges[key],
                            datasetName.Substring(1),
                            data);
                }
                else
                {
                    classTrieNode newNode = new classTrieNode();
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
            return FindData(m_root, datasetName);
        }
        private classDMSData FindData(classTrieNode node, string datasetName)
        {
            if (string.IsNullOrEmpty(datasetName))
            {
                // This means that we are out of our search string.
                if (node.DmsData != null)
                {
                    return node.DmsData;
                }
                else
                {
                    throw new KeyNotFoundException("Could not resolve the dataset name.  The dataset is just not available in this trie.");
                }                                
            }
            else
            {
                // This means we still have a string to search with...

                char key = datasetName[0];
                if (node.Edges.Count < 1)
                {
                    // but now we are out of datasets to search now...this has to be the guy...                    
                    // although it could still not be him...
                    //TODO: Speak with Gary about this.                    
                    return node.DmsData;
                }
                else
                {
                    // more datasets exist past this point...
                    bool hasKey = node.Edges.ContainsKey(key);
                    if (hasKey)
                    {
                        // This means that we have the edge we need
                        // we need to keep following.
                        return FindData(node.Edges[key], datasetName.Substring(1));
                    }
                    else
                    {
                        // this means that we have nodes...but we don't have the key...
                        throw new KeyNotFoundException("Could not resolve the dataset name.  The dataset is just not available in this trie.");
                    }
                }
            }            
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
