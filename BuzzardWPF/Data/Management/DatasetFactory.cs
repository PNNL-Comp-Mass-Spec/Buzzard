using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;
using LcmsNetDmsTools;
using LcmsNetDataClasses.Data;


namespace BuzzardWPF.Data
{
    /// <summary>
    /// Loads data 
    /// </summary>
    public class DatasetFactory
    {
        public static ObservableCollection<BuzzardDataset> LoadDatasetData(string path)
        {
            ObservableCollection<BuzzardDataset> datasets = new ObservableCollection<BuzzardDataset>();
            switch (path)
            {
                case "":
                case null:
                    //datasets = LoadDummyDatasetData();
                    break;
                default:

                    break;
            }

            return datasets;
        }

        private static ObservableCollection<BuzzardDataset> LoadDummyDatasetData()
        {
            ObservableCollection<BuzzardDataset> datasets = new ObservableCollection<BuzzardDataset>();
            for (int i = 0; i < 10; i++)
            {
                BuzzardDataset data = new BuzzardDataset();                
                data.DMSData.RequestName    = "test" + i.ToString();
                data.DMSData.RequestID      = 0;
                datasets.Add(data);
            }
            return datasets;
        }

		public static BuzzardDataset LoadDataset(string path)
		{
			BuzzardDataset dataset = new BuzzardDataset()
			{
				FilePath = path
			};

			dataset.DMSData.DatasetName = Path.GetFileNameWithoutExtension(path);

			if (dataset.DMSData.DatasetName.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
				dataset.DMSData.DatasetName = dataset.DMSData.DatasetName.Substring(2);

			if (dataset.DMSData.DatasetName.StartsWith("qc_shew", StringComparison.OrdinalIgnoreCase))
				dataset.IsQC = true;

			return dataset;
		}
    }
}
