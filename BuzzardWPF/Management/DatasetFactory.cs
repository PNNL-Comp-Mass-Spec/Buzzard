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
                DmsData =
                {
                    DatasetName = BuzzardTriggerFileTools.GetDatasetNameFromFilePath(path),
                    CartName = DatasetManager.Manager.WatcherMetadata.CartName
                }
            };

            if (dataset.DmsData.DatasetName.StartsWith("qc_", StringComparison.OrdinalIgnoreCase) ||
                dataset.DmsData.DatasetName.StartsWith("qc-", StringComparison.OrdinalIgnoreCase))
            {
                // Assuming that people will generally name QC datasets 'QC_xxx' or 'QC-xxx'
                // But we can't watch for everything a user may do here...
                // This is now used as a gateway check for if we need to match the dataset name to a QC experiment name
                dataset.IsQC = true;
            }

            return dataset;
        }
    }
}
