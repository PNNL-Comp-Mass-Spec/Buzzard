using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Principal;

namespace Finch
{
    internal class RESTEasyHttp
    {
        public static void SendFile(
            string url,
            string filePath,
            NetworkCredential loginCredentials = null)
        {
            SendFileWorker(url, filePath, (ICredentials)loginCredentials);
        }

        private static void SendFileWorker(
            string url,
            string filePath,
            ICredentials loginCredentials = null)
        {
            const string CONTENT_TYPE = "text/xml";
            const int PORT = 80;

            var uri = new Uri(url);
            var credentialCache = new CredentialCache
            {
                {uri, "Negotiate", (NetworkCredential)loginCredentials}
            };

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {

                var fileLength = fileStream.Length;

                var webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Credentials = (ICredentials)credentialCache;
                webRequest.KeepAlive = true;
                webRequest.Method = "PUT";
                webRequest.AllowWriteStreamBuffering = false;
                webRequest.Headers.Add("Content-Disposition", "attachment; filename=" + Path.GetFileName(filePath));
                webRequest.Timeout = -1;
                webRequest.ReadWriteTimeout = -1;

                using (var requestStream = webRequest.GetRequestStream())
                {

                    var bufferLength = (int)Math.Min(short.MaxValue, fileStream.Length);
                    var buffer = new byte[bufferLength];
                    var bytesRead = 0L;
                    var chunksToRead = (long)Math.Ceiling((double)fileLength / (double)bufferLength);
                    var chunksRead = 0L;

                    int count;
                    do
                    {
                        count = fileStream.Read(buffer, 0, buffer.Length);
                        bytesRead += (long)count;
                        ++chunksRead;
                        var percentComplete = Math.Ceiling((double)chunksRead / (double)chunksToRead * 100.0);
                        requestStream.Write(buffer, 0, count);
                    } while (count > 0);

                    webRequest.GetResponse();
                    using (var responseStream = webRequest.GetResponse().GetResponseStream())
                    {
                        if (responseStream == null)
                        {
                            return;
                        }

                        using (var streamReader = new StreamReader(responseStream))
                        {
                            streamReader.ReadToEnd();
                        }
                    }
                }

            }


        }

        public static string GetUserName(bool cleanDomain = false)
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity == null)
                return "Unknown_Windows_User";
            
            var str = windowsIdentity.Name;
            if (cleanDomain)
                str = str.Substring(str.IndexOf('\\') + 1);
            return str;
        }

        /// <summary>
        /// Get or post data to the given URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="method"></param>
        /// <param name="contentType">Often "text/xml"</param>
        /// <param name="sendStringInHeader"></param>
        /// <param name="loginCredentials"></param>
        /// <returns></returns>
        /// <remarks>If method is HttpMethod.Post and content type is empty, we assume "application/x-www-form-urlencoded"</remarks>
        public static string Send(
            string url,
            string postData = "",
            HttpMethod method = HttpMethod.Get,
            string contentType = "",
            bool sendStringInHeader = false,
            NetworkCredential loginCredentials = null)
        {
            var requestUri = new Uri(url);
            var userName = GetUserName(true);

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            httpWebRequest.Credentials = (ICredentials)loginCredentials;

            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Cookie("user_name", userName)
            {
                Domain = "pnl.gov"
            });

            httpWebRequest.CookieContainer = cookieContainer;
            httpWebRequest.Method = method.ToString().ToUpper();
            httpWebRequest.PreAuthenticate = false;

            if (sendStringInHeader && method == HttpMethod.Get)
                httpWebRequest.Headers.Add("X-Json-Data", postData);

            if (method == HttpMethod.Post && !string.IsNullOrEmpty(postData) && string.IsNullOrEmpty(contentType))
                contentType = "application/x-www-form-urlencoded";

            if (!string.IsNullOrEmpty(contentType) && method == HttpMethod.Post)
            {
                httpWebRequest.ContentType = contentType;
                if (string.IsNullOrEmpty(postData))
                    httpWebRequest.ContentLength = 0;
                else
                    httpWebRequest.ContentLength = (long)postData.Length;
            }

            if (method == HttpMethod.Post)
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    streamWriter.Write(postData);
            }

            try
            {
                var responseStream = httpWebRequest.GetResponse().GetResponseStream();

                if (responseStream == null)
                {
                    return string.Empty;
                }

                using (var streamReader = new StreamReader(responseStream))
                {
                    var message = streamReader.ReadToEnd();
                    return message;
                }
            }
            catch (WebException ex)
            {
                var responseStream = httpWebRequest.GetResponse().GetResponseStream();
                if (responseStream == null)
                {
                    throw new Exception(ex.Message, (Exception)ex);
                }

                using (var streamReader = new StreamReader(responseStream))
                {
                    var message = streamReader.ReadToEnd();
                    throw new Exception(message, (Exception)ex);
                }
            }
        }

        public enum HttpMethod
        {
            [Description("GET")]
            Get,
            [Description("POST")]
            Post,
            [Description("PUT")]
            Put,
        }
    }
}