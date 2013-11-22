using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using BuzzardWPF.Data;

using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;


namespace BuzzardWPF.IO
{
	/// <summary>
	/// This class is a variation of LCMSNetDataClasses.classTriggerFileTools, that has been
	/// modified to work with datasets that 
	/// </summary>
	public class TriggerFileTools
	{
		private static XmlDataDocument mobject_TriggerFileContents = null;

		/// <summary>
		/// Generates a trigger file for a sample
		/// </summary>
		/// <param name="sample"></param>
		public static void GenerateTriggerFile(classSampleData sample, BuzzardDataset dataset, DMSData dmsData, string destinationDir)
		{
			// Exit if trigger file creation disabled
			if (!bool.Parse(classLCMSSettings.GetParameter("CreateTriggerFiles")))
			{
				string msg = "GenerateTriggerFile: Sample " + sample.DmsData.DatasetName + ", Trigger file creation disabled";
				classApplicationLogger.LogMessage(0, msg);
				return;
			}

			// Create an XML document containing the trigger file's contents
			GenerateXmlDoc(sample, dataset, dmsData);

			// Write the document to the file
			SaveFile(mobject_TriggerFileContents, sample, sample.DmsData.DatasetName, dataset, dmsData, destinationDir);
		}

		/// <summary>
		/// Generates the XML-formatted trigger file contents
		/// </summary>
		/// <param name="sample">sample object for sample that was run</param>
		/// <remarks>
		/// </remarks>
		private static void GenerateXmlDoc(classSampleData sample, BuzzardDataset dataset, DMSData dmsData)
		{
			// Create and initialize the document
			mobject_TriggerFileContents     = new XmlDataDocument();
			XmlDeclaration docDeclaration   = mobject_TriggerFileContents.CreateXmlDeclaration("1.0", null, null);
			mobject_TriggerFileContents.AppendChild(docDeclaration);

			// Add dataset (Root) element
			XmlElement rootElement          = mobject_TriggerFileContents.CreateElement("Dataset");
			mobject_TriggerFileContents.AppendChild(rootElement);

			// Add the parameters
			AddParam(rootElement, "Dataset Name",		dmsData.DatasetName);

			if (dmsData.LockData || string.IsNullOrWhiteSpace(dataset.ExperimentName))
				AddParam(rootElement, "Experiment Name", dmsData.Experiment);
			else
				AddParam(rootElement, "Experiment Name", dataset.ExperimentName);

			AddParam(rootElement, "Instrument Name",	dataset.Instrument);
			AddParam(rootElement, "Separation Type",	dataset.SeparationType);
			AddParam(rootElement, "LC Cart Name",		dmsData.CartName);
			AddParam(rootElement, "LC Column",			dataset.LCColumn);
			AddParam(rootElement, "Wellplate Number",	sample.PAL.WellPlate);
			AddParam(rootElement, "Well Number",		sample.PAL.Well.ToString());
			AddParam(rootElement, "Dataset Type",		dmsData.DatasetType);
			AddParam(rootElement, "Operator (PRN)",		dataset.Operator);
			AddParam(rootElement, "Comment",			dmsData.Comment + " Buzzard: " + dataset.Comment);
			AddParam(rootElement, "Interest Rating",	dataset.InterestRating);

			/// 
			/// BLL: Added to appease the trigger file gods, so that we dont write 
			/// confuse DMS with EMSL related data when the requests are already fulfilled.
			/// 
			string usage    = "";
			string userList = "";
			string proposal = "";

			if (dataset.DMSData.LockData)
			{
				if (sample.DmsData.RequestID <= 0)
				{
					usage = dmsData.EMSLUsageType;
					userList = dmsData.UserList;
					proposal = dmsData.EMSLProposalID;
				}
			}
			else
			{
				if (sample.DmsData.RequestID <= 0)
				{
					proposal	= dataset.DMSData.EMSLProposalID;
					usage		= dataset.DMSData.EMSLUsageType;

					if (dataset.EMSLProposalUsers == null || dataset.EMSLProposalUsers.Count == 0)
					{
						userList = string.Empty;
					}
					else
					{
						for (int i = 0; i < dataset.EMSLProposalUsers.Count; i++)
						{
							userList += dataset.EMSLProposalUsers[i].UserID +
								(i < dataset.EMSLProposalUsers.Count - 1 ?
								 "," :
								 "");
						}
					}
				}
			}

			AddParam(rootElement, "Request",			dmsData.RequestID.ToString());
			AddParam(rootElement, "EMSL Proposal ID",	proposal);
			AddParam(rootElement, "EMSL Usage Type",	usage);
			AddParam(rootElement, "EMSL Users List",	userList);
			AddParam(rootElement, "Run Start",			sample.LCMethod.ActualStart.ToString("MM/dd/yyyy HH:mm:ss"));
			AddParam(rootElement, "Run Finish",			sample.LCMethod.ActualEnd.ToString("MM/dd/yyyy HH:mm:ss"));
			// Removed to fix date comparison problems during DMS data import
			//AddParam(rootElement, "Run Finish UTC",   ConvertTimeLocalToUtc(sample.LCMethod.ActualEnd)); 
		}

		/// <summary>
		/// Adds a trigger file parameter to the XML document defining the file contents
		/// </summary>
		/// <param name="Parent">Parent element to add the parameter to</param>
		/// <param name="paramName">Name of the parameter to add</param>
		/// <param name="paramValue">Value of the parameter</param>
		private static void AddParam(XmlElement Parent, string paramName, string paramValue)
		{
			try
			{
				XmlElement newElement = mobject_TriggerFileContents.CreateElement("Parameter");
				XmlAttribute nameAttr = mobject_TriggerFileContents.CreateAttribute("Name");
				nameAttr.Value = paramName;
				newElement.Attributes.Append(nameAttr);
				XmlAttribute valueAttr = mobject_TriggerFileContents.CreateAttribute("Value");
				valueAttr.Value = paramValue;
				newElement.Attributes.Append(valueAttr);
				Parent.AppendChild(newElement);
			}
			catch (Exception ex)
			{
				classApplicationLogger.LogError(0, "Exception creating trigger file", ex);
			}
		}

		/// <summary>
		/// Write the trigger file
		/// </summary>
		/// <param name="doc">XML document to be written</param>
		/// <param name="sample">Name of the sample this trigger file is for</param>
		private static void SaveFile(XmlDocument doc, classSampleData sample, string datasetName, BuzzardDataset dataset, DMSData dmsData, string outFilePath)
		{
			string sampleName       = sample.DmsData.DatasetName;			
			string outFileName      = GetTriggerFileName(sample, ".xml", dataset);			
			string outFileNamePath  = Path.Combine(outFilePath, outFileName);

			try
			{
				// Skip remote file creation if disabled
				if (bool.Parse(classLCMSSettings.GetParameter("CopyTriggerFiles")))
				{
					// Attempt to write the trigger file to a remote server
					FileStream outputFile = new FileStream(outFileNamePath, FileMode.Create, FileAccess.Write);
					doc.Save(outputFile);
					outputFile.Close();
					classApplicationLogger.LogMessage(0, "Remote trigger file created for sample " + sample.DmsData.DatasetName);
					return;
				}
				else
				{
					string msg = "GenerateTriggerFile: Sample " + datasetName + ", Trigger file copy disabled";
					classApplicationLogger.LogMessage(0, msg);
				}
			}
			catch (Exception ex)
			{
				// If remote write failed or disabled, log and try to write locally
				string msg = "Remote trigger file copy failed or disabled, sample " + sample.DmsData.DatasetName + ". Creating file locally.";
				classApplicationLogger.LogError(0, msg, ex);
			}

			// Write trigger file to local folder
			outFilePath = Path.Combine(classLCMSSettings.GetParameter("ApplicationPath"), "TriggerFiles");

			// If local folder doen't exist, then create it
			if (!Directory.Exists(outFilePath))
			{
				try
				{
					Directory.CreateDirectory(outFilePath);
				}
				catch (Exception ex)
				{
					classApplicationLogger.LogError(0, "Exception creating local trigger file folder", ex);
					return;
				}
			}

			outFileNamePath = Path.Combine(outFilePath, outFileName);
			try
			{
				FileStream outputFile = new FileStream(outFileNamePath, FileMode.Create, FileAccess.Write);
				doc.Save(outputFile);
				outputFile.Close();
				classApplicationLogger.LogMessage(0, "Local trigger file created for sample " + sampleName);
				return;
			}
			catch (Exception ex)
			{
				// If local write failed, log it
				string msg = "Error creating local trigger file for sample " + sampleName;
				classApplicationLogger.LogError(0, msg, ex);
			}
		}

		public static string GetTriggerFileName(classSampleData sample, string extension, BuzzardDataset dataset)
		{
			string datasetName = sample.DmsData.DatasetName;
			string outFileName = 
                string.Format("{0}_{1}_{2}{3}",
									dataset.DMSData.CartName,
				//DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0)).ToString("MM.dd.yyyy_hh.mm.ss_"),
									sample.LCMethod.Start.ToString("MM.dd.yyyy_hh.mm.ss"),
									datasetName,
									extension);
			return outFileName;
		}
	}
}
