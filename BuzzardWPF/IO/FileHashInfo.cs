using System.IO;

namespace BuzzardWPF.IO
{
    public class FileHashInfo
    {
        public FileHashInfo(FileInfo fi, string sha1Hash)
        {
            File = fi;
            Sha1Hash = sha1Hash;
        }

        public FileInfo File { get; }
        public string FileName => File.Name;
        public string FilePath => File.FullName;
        public long Size => File.Length;
        public string Sha1Hash { get; }
    }
}
