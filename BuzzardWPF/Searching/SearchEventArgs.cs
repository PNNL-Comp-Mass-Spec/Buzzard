using System;

namespace BuzzardWPF.Searching
{
    /// <summary>
    /// Event arguments when a file search action is to be performed.
    /// </summary>
    public class SearchEventArgs: EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="config"></param>
        public SearchEventArgs(SearchConfig config)
        {
            Config = config;
        }
        /// <summary>
        /// Gets the search configuration.
        /// </summary>
        public SearchConfig Config
        {
            get;
            private set;
        }
    }
}
