using System.Collections.Generic;
using System.IO;
using PRISM;

namespace BuzzardWPF.IO
{
    public static class FileHashChecks
    {
        // Ignore Spelling: uimf

        // NOTE: Intentionally excluding Agilent MassHunter 'Contents.xml' and 'IMSFrameMeth.xml' files
        private static readonly IReadOnlyList<string> HashedFileNameSpecs = new List<string>()
        {
            "MSPeak.bin",
            "MSPeriodicActuals.bin",
            "MSScan.bin",
            "MSProfile.bin",
            "IMSFrame.bin",
            "analysis.baf",
            "analysis.tdf_bin",
            "analysis.tdf",
            "fid",
            "DATA.MS",
            "ser",
            "*.uimf",
            "*.qgd",
            "*.raw",
            "_FUNC*.DAT",
            "_PROC*.DAT",
        };

        public static List<FileHashInfo> GetHashedFiles(string datasetPath)
        {
            var hashedFiles = new List<FileHashInfo>();
            if (File.Exists(datasetPath))
            {
                // single file, always hash it
                hashedFiles.Add(HashFile(datasetPath));
                return hashedFiles;
            }

            var di = new DirectoryInfo(datasetPath);
            if (!di.Exists)
            {
                return null;
            }

            foreach (var fileSpec in HashedFileNameSpecs)
            {
                var matched = di.EnumerateFiles(fileSpec, SearchOption.AllDirectories);
                foreach (var fi in matched)
                {
                    // Ignore zero-byte files in directories
                    if (fi.Length > 0)
                    {
                        hashedFiles.Add(HashFile(fi));
                    }
                }
            }

            return hashedFiles;
        }

        private static FileHashInfo HashFile(string filePath)
        {
            return HashFile(new FileInfo(filePath));
        }

        private static FileHashInfo HashFile(FileInfo file)
        {
            var sha1Hash = HashUtilities.ComputeFileHashSha1(file.FullName);
            return new FileHashInfo(file, sha1Hash);
        }
    }
}
