using System.Collections.Generic;
using System.Linq;

namespace BuzzardWPF.Data.Trie
{
    /// <summary>
    /// Trie Node that indexes on a single char.
    /// </summary>
    /// <remarks>
    /// Simpler implementation than <see cref="TrieNodeString"/>, but leads to more memory use and larger call stacks.
    /// Adding new data to this trie is also faster than adding to a <see cref="TrieNodeString"/> because the key is always a fixed length of 1.</remarks>
    public class TrieNodeChar : Dictionary<char, TrieNodeChar>, ITrieNode
    {
        // Ignore Spelling: Trie

        public int RequestID { get; set; } = -1;

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
                if (!TryGetValue(key, out var node))
                {
                    node = new TrieNodeChar();
                    Add(key, node);
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
            if (TryGetValue(key, out var node))
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
        public new void Clear()
        {
            RequestID = -1;
            foreach (var node in Values)
            {
                node.Clear();
            }
        }

        /// <summary>
        /// Prune any empty branches in the trie
        /// </summary>
        public void RemoveEmptyNodes()
        {
            foreach (var node in this.ToList())
            {
                // Depth first - we need to cascade from the leaves back to the trunk
                node.Value.RemoveEmptyNodes();

                if (node.Value.RequestID < 0 && node.Value.Count == 0)
                {
                    // Node edge doesn't have (or no longer has) any child nodes, nor a valid RequestID. Remove it.
                    Remove(node.Key);
                }
            }
        }

        public override string ToString()
        {
            return $"{RequestID}, {new string(Keys.ToArray())}";
        }
    }
}
