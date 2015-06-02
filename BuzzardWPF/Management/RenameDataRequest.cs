using System;
using System.IO;
using System.Text;
using System.Windows;
using BuzzardLib.Data;
using BuzzardLib.IO;
using BuzzardWPF.Windows.Dialogs;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF.Management
{
	public class RenameDataRequest
	{
        /// <summary>
        /// File path to fix invalid characters in
        /// </summary>
		public string SourceDataPath {
		    get 
            { 
                return mSourceDataPath; 
            }
		    set
		    {
		        mSourceDataPath = value;
		        mFixedDataPath = FixFileName(mSourceDataPath);
		    }
		}

        /// <summary>
        /// Updated file path
        /// </summary>
	    public string FixedDataPath {
            get { return mFixedDataPath; }
        }

		public BuzzardDataset Dataset { get; set; }

        protected string mSourceDataPath;
        protected string mFixedDataPath;

	    /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataset">Buzzard dataset</param>
	    public RenameDataRequest(BuzzardDataset dataset)
	    {
	        Dataset = dataset;
	        SourceDataPath = dataset.FilePath;
	    }

	    public void RenameData(ref bool informUser, ref bool skipOnConflict)
		{
            if (string.Equals(SourceDataPath, FixedDataPath, StringComparison.CurrentCultureIgnoreCase))
		    {
                // Source and destination are the same; nothing to do
		        return;
		    }

	        var hasConflict = HasConflict();

			if (hasConflict)
			{
				if (informUser)
				{
					var dialog = new DatasetOverwriteDialog
					{
                        FileToRenamePath = SourceDataPath,
                        FileInWayPath = FixedDataPath
					};

					dialog.ShowDialog();

					informUser = !dialog.DoSameToOtherConflicts;
                    skipOnConflict = dialog.SkipDatasetRename;
				}

				if (skipOnConflict)
				{
					classApplicationLogger.LogMessage(
						0,
						string.Format("Skipping {0}", SourceDataPath));
				    return;
				}
   
			}
	        
            if (File.Exists(SourceDataPath))
	        {
	            RenameFile();
	        }
	        else if (Directory.Exists(SourceDataPath))
	        {
	            RenameFolder();
	        }	        
	        else
	        {
                // File or folder not found
                classApplicationLogger.LogError(
                       0,
                       string.Format(
                           "The item must have been renamed since the search was performed.  Cannot find the selected item. {0}.  ",
                           SourceDataPath));
	        }
		}

        private void FinalizeRename()
        {
            if (string.IsNullOrWhiteSpace(Dataset.Name))
                Dataset.Name = string.Copy(Dataset.DMSData.DatasetName);

            Dataset.Name = FixName(SourceDataPath, Dataset.Name);
            Dataset.DMSData.DatasetName = FixName(SourceDataPath, Dataset.DMSData.DatasetName);

            Dataset.DatasetStatus = DatasetStatus.Pending;
            Dataset.TriggerCreationWarning = "Fixed name";

            classApplicationLogger.LogMessage(
                0,
                string.Format("Renamed '{0}' to '{1}'", SourceDataPath, FixedDataPath));
        }

        private string FixFileName(string sourceDataPath)
        {
            if (string.IsNullOrWhiteSpace(sourceDataPath))
                return string.Empty;

            var fiFile = new FileInfo(sourceDataPath);
            var parentFolder = fiFile.DirectoryName ?? string.Empty;
            var fileExtension = Path.GetExtension(fiFile.Name);

            var hasArchivePrefix = fiFile.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase);

            var datasetName = TriggerFileTools.GetDatasetNameFromFilePath(fiFile.Name);

            var fixedName = FixName(sourceDataPath, datasetName);

            if (hasArchivePrefix)
                fixedName = "x_" + fixedName;

            var newPath = Path.Combine(parentFolder, fixedName + fileExtension);

            return newPath;
        }

        private string FixName(string sourceDataPath, string datasetName)
        {
            var fixedName = new StringBuilder(datasetName.Length);
            foreach (var letter in datasetName)
            {
                if (TriggerFileTools.NameHasInvalidCharacters(letter.ToString()))
                    fixedName.Append('_');
                else
                {
                    fixedName.Append(letter);
                }
            }

            if (fixedName.Length < TriggerFileTools.MINIMUM_DATASET_NAME_LENGTH)
            {
                // Add a datestamp
                var fiSourceFile = new FileInfo(sourceDataPath);
                if (fiSourceFile.Exists)
                    fixedName.Append(fiSourceFile.LastWriteTime.ToString("_yyyyMMdd"));
                else
                    fixedName.Append(DateTime.Now.ToString("_yyyyMMdd"));
            }

            return fixedName.ToString();
        }

        private bool HasConflict()
        {
            var hasFile = File.Exists(FixedDataPath);
            var hasFolder = Directory.Exists(FixedDataPath);

            return (hasFile || hasFolder);
        }

	    private void RenameFile()
	    {
	        try
	        {
	            if (File.Exists(FixedDataPath))
                    File.Delete(FixedDataPath);

	            File.Move(SourceDataPath, FixedDataPath);
	            Dataset.FilePath = FixedDataPath;

	            FinalizeRename();
	        }
	        catch (Exception ex)
	        {
	            classApplicationLogger.LogError(
	                0,
	                string.Format("Could not rename '{0}' to '{1}'", SourceDataPath, FixedDataPath),
	                ex);
	        }
	    }	    

        private void RenameFolder()
        {
            try
            {
                if (Directory.Exists(FixedDataPath))
                    Directory.Delete(FixedDataPath, true);

                Directory.Move(SourceDataPath, FixedDataPath);
                Dataset.FilePath = FixedDataPath;

                FinalizeRename();
            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(
                    0,
                    string.Format("Could not rename '{0}' to '{1}'", SourceDataPath, FixedDataPath),
                    ex);
            }
        }
	   
	}
}
