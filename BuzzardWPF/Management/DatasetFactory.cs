using System;
using System.Collections.ObjectModel;
using BuzzardLib.Data;
using BuzzardLib.IO;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// Loads data
    /// </summary>
    public class DatasetFactory
    {
        public static ObservableCollection<BuzzardDataset> LoadDatasetData(string path)
        {
            var datasets = new ObservableCollection<BuzzardDataset>();
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
            var datasets = new ObservableCollection<BuzzardDataset>();
            for (var i = 0; i < 10; i++)
            {
                var data = new BuzzardDataset
                {
                    DMSData =
                    {
                        RequestName = "test" + i,
                        RequestID = 0
                    }
                };
                datasets.Add(data);
            }
            return datasets;
        }

        public static BuzzardDataset LoadDataset(string path)
        {
            var dataset = new BuzzardDataset
            {
                FilePath = path,
                CartName = DatasetManager.Manager.WatcherConfigSelectedCartName,
                DMSData = {DatasetName = TriggerFileTools.GetDatasetNameFromFilePath(path)}
            };

            if (dataset.DMSData.DatasetName.StartsWith("qc_shew", StringComparison.OrdinalIgnoreCase) ||
                dataset.DMSData.DatasetName.StartsWith("qc_mam", StringComparison.OrdinalIgnoreCase))
                dataset.IsQC = true;

            return dataset;
        }
    }
}
