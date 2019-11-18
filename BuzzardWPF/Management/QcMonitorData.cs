using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using BuzzardWPF.Properties;
using DynamicData.Binding;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class QcMonitorData : ReactiveObject
    {
        private string emslUsageType;
        private string emslProposalId;
        private string experimentName;
        private string datasetNameMatch;
        private readonly ObservableAsPropertyHelper<string> emslProposalUserNames;

        public QcMonitorData()
        {
            emslProposalUserNames = this.WhenAnyValue(x => x.EmslProposalUsers, x => x.EmslProposalUsers.Count)
                .Select(x => string.Join("; ", x.Item1.Select(y => y.UserName)))
                .ToProperty(this, x => x.EmslProposalUsersNames);
            this.WhenAnyValue(x => x.DatasetNameMatch).Subscribe(_ => UpdateDatasetNameMatchRegex());
        }

        public string EmslUsageType
        {
            get => emslUsageType;
            set => this.RaiseAndSetIfChanged(ref emslUsageType, value);
        }

        public string EmslProposalId
        {
            get => emslProposalId;
            set => this.RaiseAndSetIfChanged(ref emslProposalId, value);
        }

        public ObservableCollectionExtended<ProposalUser> EmslProposalUsers { get; } = new ObservableCollectionExtended<ProposalUser>();
        public string EmslProposalUsersNames => emslProposalUserNames.Value;

        public string ExperimentName
        {
            get => experimentName;
            set => this.RaiseAndSetIfChanged(ref experimentName, value);
        }

        /// <summary>
        /// Start of dataset name, case insensitive; use "*" for "match any" (lowest priority, only one allowed in the QcMonitors)
        /// </summary>
        public string DatasetNameMatch
        {
            get => datasetNameMatch;
            set => this.RaiseAndSetIfChanged(ref datasetNameMatch, value);
        }

        /// <summary>
        /// Dataset name match regex, allows case-insensitivity and dash-vs-underscore swaps.
        /// </summary>
        public Regex DatasetNameMatchRegex { get; private set; }

        public bool MatchesAny => string.IsNullOrWhiteSpace(DatasetNameMatch) || DatasetNameMatch == "*";

        public string EmslUsageDisplayString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EmslUsageType) || !EmslUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (!EmslProposalUsers.Any())
                {
                    return EmslProposalId;
                }

                return $"{EmslProposalId}: {EmslProposalUsersNames}";
            }
        }

        private void UpdateDatasetNameMatchRegex()
        {
            if (string.IsNullOrWhiteSpace(datasetNameMatch) || datasetNameMatch.Equals("*"))
            {
                // A regex that won't match any valid dataset name
                DatasetNameMatchRegex = new Regex(@"^\\/%$", RegexOptions.Compiled);
                return;
            }

            // Allow replacement of dashes with underscores and vice-versa, and keep a case insensitive match
            var horBarMatch = new Regex(@"[-_]+");
            var modMatchString = "^" + horBarMatch.Replace(datasetNameMatch, @"[-_]+");
            DatasetNameMatchRegex = new Regex(modMatchString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private void LoadFromString(string qcMonitorData)
        {
            var split = qcMonitorData.Split(new[] {';'}, StringSplitOptions.None);
            ExperimentName = split[0];
            DatasetNameMatch = split[1];
            EmslUsageType = split[2];
            if (!string.IsNullOrWhiteSpace(EmslUsageType) && EmslUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
            {
                EmslProposalId = split[3];
                var emslUsers = split[4].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
                EmslProposalUsers.AddRange(DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EmslProposalId, emslUsers));
            }
        }

        private string SaveToString()
        {
            var data = ExperimentName + ";" + DatasetNameMatch + ";" + EmslUsageType;
            if (!string.IsNullOrWhiteSpace(EmslUsageType) && EmslUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
            {
                data += ";" + EmslProposalId + ";" + string.Join(",", EmslProposalUsers.Select(x => x.UserID));
            }

            return data;
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
