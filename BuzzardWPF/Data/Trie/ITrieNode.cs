namespace BuzzardWPF.Data.Trie
{
    /// <summary>
    /// Interface for a Trie for matching the start of a string to something else; in this case, used to match a dataset to a requested run name, returning the requested run ID.
    /// </summary>
    internal interface ITrieNode
    {
        /// <summary>
        /// Clears all matches out of the trie, but doesn't remove any nodes (reduces memory allocations and garbage collections)
        /// </summary>
        void Clear();

        /// <summary>
        /// After populating the trie, removes all leaf nodes that do not contain matches
        /// </summary>
        void RemoveEmptyNodes();

        /// <summary>
        /// Adds the provided requested run to the appropriate place in the Trie
        /// </summary>
        /// <param name="requestName"></param>
        /// <param name="requestId"></param>
        void AddDataset(string requestName, int requestId);

        /// <summary>
        /// Finds the requested run ID for the requested run name that matches the dataset name, or -1 if no match was found.
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="depth">The last depth checked for a match</param>
        /// <returns>value &gt;0 if a match was found, -1 if no match was found.</returns>
        int FindRequestIdForName(string datasetName, out int depth);
    }
}
