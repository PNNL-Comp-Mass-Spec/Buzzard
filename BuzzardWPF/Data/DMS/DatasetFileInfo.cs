namespace BuzzardWPF.Data.DMS
{
    public class DatasetFileInfo
    {
        public int DatasetId { get; }
        public string FileName { get; }
        public long FileSizeBytes { get; }
        public string FileHash { get; }

        public DatasetFileInfo(int datasetId, string fileName, long fileSizeBytes, string fileHash)
        {
            DatasetId = datasetId;
            FileName = fileName;
            FileSizeBytes = fileSizeBytes;
            FileHash = fileHash;
        }
    }
}
