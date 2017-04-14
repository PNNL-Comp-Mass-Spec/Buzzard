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
        [Obsolete("Unused")]
        public static ObservableCollection<BuzzardDataset> LoadDatasetData(string path)
        {
            var datasets = new ObservableCollection<BuzzardDataset>();
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
