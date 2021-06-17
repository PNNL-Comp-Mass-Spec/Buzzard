using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BuzzardWPF.Properties;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class QcMonitorData : ReactiveObject
    {
        private string experimentName;
        private string datasetNameMatch;

        public QcMonitorData()
        {
            this.WhenAnyValue(x => x.DatasetNameMatch).Subscribe(_ => UpdateDatasetNameMatchRegex());
        }

        public string ExperimentName
        {
            get => experimentName;
            set => this.RaiseAndSetIfChanged(ref experimentName, value);
        }

        /// <summary>
        /// Start of dataset name, case insensitive; use "*" for "match any" (lowest priority, only one allowed in the QCMonitors)
        /// </summary>
        public string DatasetNameMatch
        {
            get => datasetNameMatch;
            set => this.RaiseAndSetIfChanged(ref datasetNameMatch, value);
        }

        /// <summary>
        /// Dataset name match RegEx, allows case-insensitivity and dash-vs-underscore swaps.
        /// </summary>
        public Regex DatasetNameMatchRegex { get; private set; }

        public bool MatchesAny => string.IsNullOrWhiteSpace(DatasetNameMatch) || DatasetNameMatch == "*";

        private void UpdateDatasetNameMatchRegex()
        {
            if (string.IsNullOrWhiteSpace(datasetNameMatch) || datasetNameMatch.Equals("*"))
            {
                // A RegEx that won't match any valid dataset name
                DatasetNameMatchRegex = new Regex(@"^\\/%$", RegexOptions.Compiled);
                return;
            }

            var startMatch = new Regex("^(?<start>QC|Blank)(?<bar>[-_])", RegexOptions.IgnoreCase);
            var allowNumberString = startMatch.Replace(datasetNameMatch, "${start}(\\d+\\w?)?${bar}");

            // Allow replacement of dashes with underscores and vice-versa, and keep a case insensitive match
            var horBarMatch = new Regex("[-_]+");
            var modMatchString = "^" + horBarMatch.Replace(allowNumberString, "[-_]+");
            DatasetNameMatchRegex = new Regex(modMatchString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private void LoadFromString(string qcMonitorData)
        {
            var split = qcMonitorData.Split(new[] {';'}, StringSplitOptions.None);
            ExperimentName = split[0];
            DatasetNameMatch = split[1];
        }

        private string SaveToString()
        {
            return ExperimentName + ";" + DatasetNameMatch;
        }

        public static IEnumerable<QcMonitorData> LoadSettings()
        {
            var savedMonitors = Settings.Default.WatcherQCMonitors;
            if (string.IsNullOrWhiteSpace(savedMonitors))
            {
                yield break;
            }

            var split = savedMonitors.Split(new [] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var qcMonitor in split)
            {
                var qcMonitorData = new QcMonitorData();
                qcMonitorData.LoadFromString(qcMonitor);
                yield return qcMonitorData;
            }
        }

        public static void SaveSettings(IEnumerable<QcMonitorData> qcMonitors)
        {
            Settings.Default.WatcherQCMonitors = string.Join("/", qcMonitors.Select(x => x.SaveToString()));
        }
    }
}
