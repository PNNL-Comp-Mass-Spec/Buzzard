using System;
using System.Collections.Generic;
using System.Linq;
using BuzzardWPF.Logging;

namespace BuzzardWPF.Data.Trie
{
    /// <summary>
    /// Trie Node that indexes on a set-length string.
    /// </summary>
    /// <remarks>
    /// Implementation is more complex than that of <see cref="TrieNodeChar"/>, but the memory use and call stack
    /// can be significantly smaller because the key length is kept as long as possible for any node of the trie
    /// (excluding the opportunistic grouping based on matching string prefixes).
    /// Adding new data to this trie is more complex than adding to <see cref="TrieNodeChar"/> because the
    /// key length for a single node may need to change to add the new data, which also requires other changes to maintain existing data.
    /// </remarks>
    public class TrieNodeString : Dictionary<string, TrieNodeString>, ITrieNode
    {
        // Ignore Spelling: Trie

        public int RequestID { get; set; } = -1;

        private byte keyCharCount = byte.MaxValue;

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
                // Check to see if the value should be partially consolidated with another entry
                // OR: keyCharCount is larger than the portion of the datasetNamePart remaining, split the keys accordingly.
                // Max split: length of datasetNamePart
                var splitIndex = datasetNamePart.Length;

                // Find how many characters match
                for (var i = 1; i <= datasetNamePart.Length; i++)
                {
                    var matchSection = datasetNamePart.Substring(0, i);
                    if (!Keys.Any(x => x.StartsWith(matchSection)))
                    {
                        splitIndex = i - 1;
                        break;
                    }
                }

                if (splitIndex <= 0)
                {
                    // No characters match, split at the length
                    splitIndex = datasetNamePart.Length;
                }

                // Don't allow zero-length keys when adding children.
                if (keyCharCount <= 0 && Count == 0)
                {
                    keyCharCount = (byte)splitIndex;
                }

                if (splitIndex == 1 && keyCharCount > 1 && datasetNamePart.Length > 1)
                {
                    // Avoid single-char keys, if possible.
                    splitIndex = 2;
                }

                if (splitIndex < keyCharCount)
                {
                    try
                    {
                        // Need to adjust the length of the key. Copy all old data to handle it appropriately.
                        foreach (var oldNode in this.ToList())
                        {
                            // Remove the old key
                            Remove(oldNode.Key);
                            // Add the information from the old node to a new node, with the appropriate key
                            var newKey = oldNode.Key.Substring(0, splitIndex);
                            if (!TryGetValue(newKey, out var newNode))
                            {
                                newNode = new TrieNodeString { keyCharCount = (byte)(keyCharCount - splitIndex) };
                                Add(newKey, newNode);
                            }

                            newNode.Add(oldNode.Key.Substring(splitIndex), oldNode.Value);
                        }

                        keyCharCount = (byte) splitIndex;
                    }
                    catch (Exception ex)
                    {
                        ApplicationLogger.LogError(LogLevel.Error, "Requested runs missing - issue adjusting Trie", ex);
                        // For safety, remove the incomplete node contents
                        Clear();

                        // Also, don't try to do anything else with this data; we've determined there is an issue adding it.
                        return;
                    }
                }

                var key = datasetNamePart.Substring(0, keyCharCount);
                if (!TryGetValue(key, out var node))
                {
                    node = new TrieNodeString { keyCharCount = (byte)(datasetNamePart.Length - keyCharCount) };
                    Add(key, node);
                }

                node.AddDatasetInternal(datasetNamePart.Substring(keyCharCount), requestId);
            }
        }

        private int FindRequestIdForNameInternal(string datasetNamePart, ref int depth)
        {
            if (string.IsNullOrWhiteSpace(datasetNamePart))
            {
                // Ran out of search string; return regardless of value (check externally)
                return RequestID;
            }

            if (keyCharCount > datasetNamePart.Length)
            {
                // Ran out of search string; return regardless of value (check externally)
                return RequestID;
            }

            var key = datasetNamePart.Substring(0, keyCharCount);
            if (TryGetValue(key, out var node))
            {
                // We have the edge, we need to keep following
                depth++;
                return node.FindRequestIdForNameInternal(datasetNamePart.Substring(keyCharCount), ref depth);
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

            // For nodes with an EdgeCount of 1 and no RequestID, and immediate sub-node EdgeCount of 1, consolidate, and adjust keyCharCount
            // Don't permit consolidation when the child has a valid request ID (because it leads to improper matches)
            if (Count == 1 && RequestID == -1 && Values.First().Count == 1 && Values.First().RequestID == -1)
            {
                var content = this.First();
                var key = content.Key;
                var child = content.Value;
                var childContent = child.First();

                // Modify the current object appropriately
                // remove the old key
                Remove(key);

                // Copy the child requestID
                RequestID = child.RequestID;

                // Change the key length and merge the child content.
                keyCharCount += child.keyCharCount;
                key += childContent.Key;
                Add(key, childContent.Value);
            }
        }

        /// <summary>
        /// Overridden ToString(); however, Visual Studio apparently prefers the dictionary ToString to this one.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{RequestID}, {string.Join(",", Keys)}";
        }
    }
}
