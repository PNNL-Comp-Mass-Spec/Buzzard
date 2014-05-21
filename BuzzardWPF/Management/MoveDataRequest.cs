using System;
using System.IO;
using BuzzardLib.Data;
using BuzzardWPF.Windows;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF.Management
{
	public class MoveDataRequest
	{
		public string SourceDataPath { get; set; }
		public string DestinationDataPath { get; set; }
		public BuzzardDataset Dataset { get; set; }

		public void MoveData(ref bool informUser, ref bool skipOnConflict)
		{
			var hasConflict = HasConflict();

			if (hasConflict)
			{
				if (informUser)
				{
					var dialog = new DatasetOverwriteDialog
					{
						FileToMovePath	= SourceDataPath,
						FileInWayPath	= DestinationDataPath
					};

					dialog.ShowDialog();

					informUser		= !dialog.DoSameToOtherConflicts;
					skipOnConflict	= dialog.SkipDatasetMove;
				}

				if (skipOnConflict)
				{
					classApplicationLogger.LogMessage(
						0,
						string.Format("Skipping {0}", SourceDataPath));
				}
				else
				{
					// If we're dealing with a file
					if (File.Exists(SourceDataPath))
					{
						try
						{
							// Grab the create and last write datatimes because
							// it looked like replace only overwrites the file's
							// contents instead of actually replacing the file.
							var creationDate	= Dataset.RunStart;
							var modDate		= Dataset.RunFinish;

							File.Replace(SourceDataPath, DestinationDataPath, null);
							
							// Set the creation date and last write time before
							// we update the FilePath on the dataset. This way,
							// the DateTime's read from the FileInfo will be up
							// to date when the Dataset uses them to set its own
							// properties.
							File.SetCreationTime(DestinationDataPath, creationDate);
							File.SetLastWriteTime(DestinationDataPath, modDate);

							Dataset.FilePath = DestinationDataPath;

							classApplicationLogger.LogMessage(
								0,
								string.Format("Moved '{0}' to '{1}'", SourceDataPath, DestinationDataPath));
						}
						catch (Exception ex)
						{
							classApplicationLogger.LogError(
										0,
										string.Format("Could not move '{0}' to '{1}'", SourceDataPath, DestinationDataPath),
										ex);
						}
					}
					// If we're dealing with a directory
					else if (Directory.Exists(SourceDataPath))
					{
						try
						{
							Directory.Delete(DestinationDataPath, true);
							Directory.Move(SourceDataPath, DestinationDataPath);
							classApplicationLogger.LogMessage(
								0,
								string.Format("Moved '{0}' to '{1}'", SourceDataPath, DestinationDataPath));
							Dataset.FilePath = DestinationDataPath;
						}
						catch (Exception ex)
						{
							classApplicationLogger.LogError(
										0,
										string.Format("Could not move '{0}' to '{1}'", SourceDataPath, DestinationDataPath),
										ex);
						}
					}
					// If we're dealing with something that isn't really there anymore.
					else
					{
						classApplicationLogger.LogError(
									0,
									string.Format(
										"The item must have been moved since the search was performed.  Cannot find the selected item. {0}.  ",
										SourceDataPath));
					}
				}
			}
			else
			{
				// If we're dealing with a file
				if (File.Exists(SourceDataPath))
				{
					try
					{
						File.Move(SourceDataPath, DestinationDataPath);
						classApplicationLogger.LogMessage(
							0,
							string.Format("Moved '{0}' to '{1}'", SourceDataPath, DestinationDataPath));
						Dataset.FilePath = DestinationDataPath;
					}
					catch (Exception ex)
					{
						classApplicationLogger.LogError(
									0,
									string.Format("Could not move '{0}' to '{1}'", SourceDataPath, DestinationDataPath),
									ex);
					}
				}
				// If we're dealing with a directory
				else if (Directory.Exists(SourceDataPath))
				{
					try
					{
						Directory.Move(SourceDataPath, DestinationDataPath);
						classApplicationLogger.LogMessage(
							0,
							string.Format("Moved '{0}' to '{1}'", SourceDataPath, DestinationDataPath));
						Dataset.FilePath = DestinationDataPath;
					}
					catch (Exception ex)
					{
						classApplicationLogger.LogError(
									0,
									string.Format("Could not move '{0}' to '{1}'", SourceDataPath, DestinationDataPath),
									ex);
					}
				}
				// If we're dealing with something that isn't really there anymore.
				else
				{
					classApplicationLogger.LogError(
								0,
								string.Format(
									"The item must have been moved since the search was performed.  Cannot find the selected item. {0}.  ",
									SourceDataPath));
				}
			}
		}


		private bool HasConflict()
		{
			var hasFile	= File.Exists(DestinationDataPath);
			var hasFolder	= Directory.Exists(DestinationDataPath);

			return (hasFile || hasFolder);
		}
	}
}
