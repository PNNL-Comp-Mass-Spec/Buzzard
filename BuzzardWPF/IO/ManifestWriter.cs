using System;
using System.IO;

namespace BuzzardWPF.IO
{
    public static class ManifestWriter
    {
        public const string Header = "Dataset Name\tDate Moved\tSource Path\tTarget Path";

        public static string CreateManifestPath(string directory)
        {

            return "";
        }

        public static void RecordDataMove(string datasetName, string startingPath, string endingPath,
            string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                File.AppendAllText(manifestPath, Header);
            }

            var formatString = "{0}{1}{2}{1}{3}{1}{4}";
            var newLine = string.Format(formatString, datasetName, "\t", DateTime.Now, startingPath, endingPath);
            File.AppendAllText(manifestPath, newLine);
        }
    }
}
