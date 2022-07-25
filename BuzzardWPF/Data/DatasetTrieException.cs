using System;
using System.Runtime.Serialization;

namespace BuzzardWPF.Data
{
    [Serializable()]
    public class DatasetTrieException : SystemException
    {
        public int SearchDepth { get; set; }

        public string DatasetName { get; set; }

        public DatasetTrieException()
        { }

        public DatasetTrieException(string message) : base(message) { }

        public DatasetTrieException(string message, Exception inner) : base(message, inner) { }

        public DatasetTrieException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public DatasetTrieException(string message, int searchDepth, string datasetName)
            : base(message)
        {
            SearchDepth = searchDepth;
            DatasetName = datasetName;
        }

        public DatasetTrieException(string message, int searchDepth, string datasetName, Exception inner)
            : base(message, inner)
        {
            SearchDepth = searchDepth;
            DatasetName = datasetName;
        }
    }
}
