using System;
using System.IO;
namespace BuzzardWPF.Searching
{
    public static class CapturePath
    {
        public static string GetCaptureSubfolderPath(string baseFolderPath, string datasetFileOrFolderPath)
        {
            if (string.IsNullOrEmpty(baseFolderPath) ||
                string.IsNullOrEmpty(datasetFileOrFolderPath))
            {
                return string.Empty;
            }

            var diBaseFolder = new DirectoryInfo(baseFolderPath);
            var datasetFile = new FileInfo(datasetFileOrFolderPath);

            if (datasetFile.Exists)
            {
                return GetCaptureSubfolderPath(diBaseFolder, datasetFile);
            }

            var datasetFolder = new DirectoryInfo(datasetFileOrFolderPath);
            if (datasetFolder.Exists)
            {
                return GetCaptureSubfolderPath(diBaseFolder, datasetFolder);
            }

            return string.Empty;
        }

        public static string GetCaptureSubfolderPath(DirectoryInfo diBaseFolder, FileInfo datasetFile)
        {
            if (datasetFile.DirectoryName == null)
            {
                return string.Empty;
            }

            // If the user included a trailing slash in the text box, then .FullName will show it (stupid C# bug)
            // The following checks for this and removes the training slash
            var baseFullName = diBaseFolder.FullName.TrimEnd('\\');

            if (string.Equals(baseFullName, datasetFile.DirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (!datasetFile.DirectoryName.StartsWith(baseFullName))
            {
                throw new Exception("Dataset " + datasetFile.Name + " not in expected parent folder: " + baseFullName);
            }

            var relativePath = datasetFile.DirectoryName.Substring(baseFullName.Length + 1);
            return relativePath;
        }

        public static string GetCaptureSubfolderPath(DirectoryInfo diBaseFolder, DirectoryInfo datasetFolder)
        {
            if (datasetFolder.Parent == null)
            {
                return string.Empty;
            }

            var baseFullName = diBaseFolder.FullName.TrimEnd('\\');

            if (string.Equals(baseFullName, datasetFolder.Parent.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (!datasetFolder.Parent.FullName.StartsWith(baseFullName))
            {
                throw new Exception("Dataset " + datasetFolder.Name + " not in expected parent folder: " + baseFullName);
            }

            var relativePath = datasetFolder.Parent.FullName.Substring(baseFullName.Length + 1);
            return relativePath;
        }
    }
}
