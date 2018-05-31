using System;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// Loads data
    /// </summary>
    public class DatasetFactory
    {
        [Obsolete("Unused")]
        public static ReactiveList<BuzzardDataset> LoadDatasetData(string path)
        {
            var datasets = new ReactiveList<BuzzardDataset>();
            return datasets;
        }

        public static BuzzardDataset LoadDataset(string path)
        {
            var dataset = new BuzzardDataset
            {
                FilePath = path,
                CartName = DatasetManager.Manager.WatcherConfigSelectedCartName,
                DMSData = {DatasetName = BuzzardTriggerFileTools.GetDatasetNameFromFilePath(path)}
            };

            if (dataset.DMSData.DatasetName.StartsWith("qc_shew", StringComparison.OrdinalIgnoreCase) ||
                dataset.DMSData.DatasetName.StartsWith("qc_mam", StringComparison.OrdinalIgnoreCase))
                dataset.IsQC = true;

            return dataset;
        }
    }
}
