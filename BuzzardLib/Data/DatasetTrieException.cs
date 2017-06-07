using System;
using System.Runtime.Serialization;

namespace BuzzardLib.Data
{
    [Serializable()]
    public class DatasetTrieException : SystemException, ISerializable
    {
        public int SearchDepth { get; set; }

        public string DatasetName { get; set; }

        public DatasetTrieException() : base() { }

        public DatasetTrieException(string message) : base(message) { }

        public DatasetTrieException(string message, System.Exception inner) : base(message, inner) { }

        public DatasetTrieException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public DatasetTrieException(string message, int searchDepth, string datasetName)
            : base(message)
        {
            this.SearchDepth = searchDepth;
            this.DatasetName = datasetName;
        }

        public DatasetTrieException(string message, int searchDepth, string datasetName, System.Exception inner)
            : base(message, inner)
        {
            this.SearchDepth = searchDepth;
            this.DatasetName = datasetName;
        }
    }

}
