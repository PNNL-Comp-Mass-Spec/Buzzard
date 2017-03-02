using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Jayrock;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using Microsoft.Win32;

namespace Finch
{
    public class Utilities
    {
        private static SHA1Managed _hashProvider;
        public static string GenerateSha1Hash(string fileName)
        {
            byte[] fileHash;
            string hashString = string.Empty;
            FileInfo fi = new FileInfo(fileName);

            if (fi.Exists)
            {
                if (_hashProvider == null)
                {
                    _hashProvider = new SHA1Managed();
                }

                fileHash = _hashProvider.ComputeHash(fi.OpenRead());
                hashString = ToHexString(fileHash);
            }

            return hashString;
        }

        public static DirectoryInfo GetTempDirectory()
        {
            DirectoryInfo di;
            if (!string.IsNullOrEmpty(Configuration.LocalTempDirectory))
            {
                di = new DirectoryInfo(Configuration.LocalTempDirectory);
            }
            else
            {
                di = new DirectoryInfo(Path.GetTempPath());
            }
            return di;
        }

        public static string ToHexString(byte[] buffer)
        {
            StringBuilder genSHA1 = new StringBuilder();

            foreach (byte b in buffer)
            {
                genSHA1.AppendFormat("{0:X2}", b);
            }

            return genSHA1.ToString().ToLower();
        }

        public static Dictionary<string, object> JsonToObject(string jsonString)
        {
            JsonObject jso = (JsonObject)JsonConvert.Import(jsonString);
            return JsonObjectToDictionary(jso);
        }

        public static string ObjectToJson(IDictionary mdObject)
        {
            JsonObject jso = new JsonObject(mdObject);
            return jso.ToString();
        }

        public static Dictionary<string, object> JsonObjectToDictionary(JsonObject jso)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (string key in jso.Names)
            {
                if (jso[key] == null)
                {
                    jso[key] = string.Empty;
                }

                object value = jso[key];
                JsonObject tmpJso = null;
                JsonArray tmpJsa = null;
                if (value.GetType().Name == "JsonObject")
                {
                    tmpJso = value as JsonObject;
                    d.Add(key, JsonObjectToDictionary(tmpJso));  //Recurse!
                }
                else if (value.GetType().Name == "JsonArray")
                {
                    tmpJsa = value as JsonArray;
                    d.Add(key, JsonArrayToList(tmpJsa));
                }
                else
                {
                    d.Add(key, value);
                }
            }
            return d;
        }

        public static List<Dictionary<string, object>> JsonArrayToList(JsonArray jsa)
        {
            List<Dictionary<string, object>> l = new List<Dictionary<string, object>>();
            while (jsa.Length > 0)
            {
                JsonObject jso = jsa.Pop() as JsonObject;
                l.Add(JsonObjectToDictionary(jso));
            }
            return l;
        }

        //TODO - mime type discovery using the registry won't work in Linux.
        //Figure out another way, or implement two methods for Linux/Mac and Windows.
        [Obsolete]
        public static string MimeType(string fileName)
        {
            string mime = "application/octet-stream";
            string ext = Path.GetExtension(fileName).ToLower();
            RegistryKey rk = Registry.ClassesRoot.OpenSubKey(ext);

            if (rk != null && rk.GetValue("Content Type") != null)
            {
                mime = rk.GetValue("Content Type").ToString();
            }

            if (ext == "zip")
            {
                mime = "application/zip";
            }

            return mime;
        }

        public long ToUnixTime(DateTime dt)
        {
            return UnixTime.ToInt64(dt);
        }

        public DateTime ToDateTime(long unixEpoch)
        {
            return UnixTime.ToDateTime(unixEpoch);
        }

        public static string ByteFormat(long numBytes)
        {
            string unit = "bytes";
            double num = 0;
            if (numBytes >= 1099511627776)
            {
                num = Math.Round((double)numBytes / Math.Pow(1024, 4), 1);
                unit = "TiB";
            }
            else if (numBytes >= 1073741824)
            {
                num = Math.Round((double)numBytes / Math.Pow(1024, 3), 1);
                unit = "GiB";
            }
            else if (numBytes >= 1048576)
            {
                num = Math.Round((double)numBytes / Math.Pow(1024, 2), 1);
                unit = "MiB";
            }
            else if (numBytes >= 1024)
            {
                num = Math.Round((double)numBytes / 1024, 1);
                unit = "KiB";
            }
            else if (numBytes >= 1024)
            {
                num = Math.Round((double)numBytes, 1);
            }

            return string.Format("{0} {1}", num, unit);
        }

        //TODO - MONO check
        public static string GetUserName(bool cleanDomain = false)
        {
            string userName = WindowsIdentity.GetCurrent().Name;

            if (cleanDomain)
            {
                userName = userName.Substring(userName.IndexOf('\\') + 1);
            }

            return userName;
        }

        /// <summary>
        /// Get a NetworkCredentials instance associated with location.
        /// </summary>
        /// <param name="location">A URI to test user credentials against.</param>
        /// <param name="userName">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="domain">Domain.</param>
        /// <param name="throwExceptions">Throws exceptions on error if true.</param>
        /// <returns>On success a NetworkCredential instance is returned.  If throwExceptions equals
        /// true all exceptions will propogate up the stack, otherwise null is returned.</returns>
        public static NetworkCredential GetCredential(Uri location, string userName,
            SecureString password, string domain, bool throwExceptions = true)
        {
            NetworkCredential ret = null;
            try
            {
                Uri uri = location;
                bool redirected = false;
                do
                {
                    HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
                    //Configuration.SetProxy(request);

                    ret = new NetworkCredential(userName, password.ToString(), domain);
                    request.UseDefaultCredentials = false;
                    request.Credentials = ret;

                    request.AllowAutoRedirect = false;
                    HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                    if (resp.StatusCode == HttpStatusCode.Redirect)
                    {
                        uri = new Uri(resp.GetResponseHeader("Location"));
                        redirected = true;
                    }
                    else
                    {
                        redirected = false;
                    }
                } while (redirected);
            }
            catch
            {
                if (throwExceptions)
                {
                    throw;
                }
                ret = null;
            }
            return ret;
        }

        public static string GetRedirect(Uri location, bool throwExceptions = true)
        {
            try
            {
                do
                {
                    HttpWebRequest request = WebRequest.Create(location) as HttpWebRequest;
                    request.UseDefaultCredentials = false;

                    request.AllowAutoRedirect = false;

                    HttpWebResponse resp = request.GetResponse() as HttpWebResponse;

                    //TODO - add other 30x codes...
                    if (resp.StatusCode == HttpStatusCode.Redirect)
                    {
                        location = new Uri(resp.GetResponseHeader("Location"));
                    }
                    else
                    {
                        break;
                    }
                } while (true);
            }
            catch (WebException ex)
            {
                HttpWebResponse resp = ex.Response as HttpWebResponse;
                if (resp.StatusCode != HttpStatusCode.Unauthorized)
                {
                    throw;
                }
            }
            catch
            {
                if (throwExceptions)
                {
                    throw;
                }
                location = null;
            }

            return location.Scheme + "://" + location.Host;
        }
    }
}