using System.Collections.Generic;
using System.Linq;
using LcmsNetData.Data;

namespace BuzzardWPF.Data
{
    /// <summary>
    /// Used to track Requested Runs (instruments runs) in DMS
    /// </summary>
    public class DatasetTrie
    {
        private readonly TrieNode rootNode = new TrieNode();
        private readonly Dictionary<int, DMSData> requestIDToDMSMap = new Dictionary<int, DMSData>();

        public void Clear()
        {
            rootNode.Clear();
            requestIDToDMSMap.Clear();
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
            var firstItem = true;
            foreach (var datum in newData)
            {
                if (firstItem)
                {
                    // Avoid clearing the Trie when no data was returned.
                    Clear();
                    firstItem = false;
                }

                AddData(datum);
            }

            if (!firstItem)
            {
                // Only do this if we added data to the Trie
                RemoveEmptyNodes();
            }

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
                requestIDToDMSMap.Add(data.RequestID, data);
        }

        /// <summary>
        /// Finds the dataset data that for the given request key
        /// </summary>
        public DMSData FindData(int requestID)
        {
            if (!requestIDToDMSMap.ContainsKey(requestID))
                throw new DatasetTrieException("Could not resolve the Request ID. The dataset is just not available.");

            return requestIDToDMSMap[requestID];
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
            if (requestId < 0 || !requestIDToDMSMap.TryGetValue(requestId, out var dmsData))
            {
                throw new DatasetTrieException("Could not resolve the dataset name. The dataset is just not available in this trie.", searchDepth, datasetName);
            }

            return dmsData;
        }
    }

    public class TrieNode
    {
        public int RequestID { get; set; } = -1;
        public Dictionary<char, TrieNode> Edges { get; } = new Dictionary<char, TrieNode>();

        /// <summary>
        /// Adds data to the trie (ignoring case).
        /// </summary>
        /// <param name="datasetName">dataset name to add</param>
        /// <param name="requestId">request id associated with the dataset name</param>
        public void AddDataset(string datasetName, int requestId)
        {
            AddDatasetInternal(datasetName.ToLowerInvariant(), requestId);
        }

        /// <summary>
        /// Finds the dataset data that closest resembles the dataset name (ignoring case).
        /// </summary>
        /// <param name="datasetName">Name of the dataset to search with.</param>
        /// <param name="searchDepth">Index in the dataset name where match or error occurred.</param>
        /// <returns>int &gt;0 if it exists. -1 if the name does not exist</returns>
        public int FindRequestIdForName(string datasetName, out int searchDepth)
        {
            searchDepth = 0;
            return FindRequestIdForNameInternal(datasetName.ToLowerInvariant(), ref searchDepth);
        }

        private void AddDatasetInternal(string datasetNamePart, int requestId)
        {
            if (string.IsNullOrWhiteSpace(datasetNamePart))
            {
                RequestID = requestId;
            }
            else
            {
                var key = datasetNamePart[0];
                if (!Edges.TryGetValue(key, out var node))
                {
                    node = new TrieNode();
                    Edges.Add(key, node);
                }

                node.AddDatasetInternal(datasetNamePart.Substring(1), requestId);
            }
        }

        private int FindRequestIdForNameInternal(string datasetNamePart, ref int depth)
        {
            if (string.IsNullOrWhiteSpace(datasetNamePart))
            {
                // Ran out of search string; return regardless of value (check externally)
                return RequestID;
            }

            var key = datasetNamePart[0];
            if (Edges.TryGetValue(key, out var node))
            {
                // We have the edge, we need to keep following
                depth++;
                return node.FindRequestIdForNameInternal(datasetNamePart.Substring(1), ref depth);
            }

            // Either:
            // 1) Found a request whose full name matches the start of the dataset file on the instrument (but not the entire filename)
            //   - Return this as the best matching active request
            // 2) There are more edges, but the key does not exist
            //   - Return this as the best matching active request (usually will be '-1' for error)
            return RequestID;
        }

        /// <summary>
        /// 'Clear' the node, a.k.a. blank the request ID and do the same for all child nodes
        /// </summary>
        public void Clear()
        {
            RequestID = -1;
            foreach (var node in Edges.Values)
            {
                node.Clear();
            }
        }

        /// <summary>
        /// Prune any empty branches in the trie
        /// </summary>
        public void RemoveEmptyNodes()
        {
            foreach (var node in Edges.ToList())
            {
                // Depth first - we need to cascade from the leaves back to the trunk
                node.Value.RemoveEmptyNodes();

                if (node.Value.RequestID < 0 && node.Value.Edges.Count == 0)
                {
                    // Node edge doesn't have (or no longer has) any child nodes, nor a valid RequestID. Remove it.
                    Edges.Remove(node.Key);
                }
            }
        }

        public override string ToString()
        {
            return $"{RequestID}, {new string(Edges.Keys.ToArray())}";
        }
    }
}
