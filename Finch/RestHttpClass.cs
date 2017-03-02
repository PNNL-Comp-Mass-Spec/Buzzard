using System;
using System.Collections.Generic;
using System.IO;

namespace Finch
{
    public class FinchRestHttpClass: IFinchWriter
    {
        public FinchRestHttpClass()
        {
            URL = null;
        }

        /// <summary>
        /// Gets or sets the URL for the writer.
        /// </summary>
        public string URL
        {
            get;
            set;
        }

        #region IFinchWriter Members
        /// <summary>
        /// Writes the data aggregates to the server.
        /// </summary>
        /// <param name="aggregates"></param>
        /// <param name="path"></param>
        /// <exception cref="URLNotSetException">Thrown if the URL is not set.</exception>
        public void WriteAggregates(List<Data.FinchAggregateData> aggregates, string path)
        {
            if (URL == null)
            {
                throw new URLNotSetException("The URL for the Finch HTTP Writer was not set.  Cannot contact server.");
            }

            FinchXmlWriter writer = new FinchXmlWriter();
            writer.WriteAggregates(aggregates, path);

            string data = File.ReadAllText(path);

            RESTEasyHttp.Send(URL, data, RESTEasyHttp.HttpMethod.Post, "text/xml");
            //RESTEasyHttp.SendFile(URL, path);
        }
        #endregion
    }


    public class URLNotSetException : Exception
    {
        public URLNotSetException(string message) :
            base(message)
        {

        }
    }
}