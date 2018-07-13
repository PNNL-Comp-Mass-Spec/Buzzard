using System;
using System.Collections.Generic;
using System.Linq;
using BuzzardWPF.Properties;
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

        public QcMonitorData()
        {
            EmslProposalUsers.Changed.Subscribe(x => this.RaisePropertyChanged(nameof(EmslProposalUsersNames)));
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

        public ReactiveList<ProposalUser> EmslProposalUsers { get; } = new ReactiveList<ProposalUser>();
        public string EmslProposalUsersNames => string.Join("; ", EmslProposalUsers.Select(x => x.UserName));

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
                using (EmslProposalUsers.SuppressChangeNotifications())
                {
                    EmslProposalUsers.AddRange(DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EmslProposalId, emslUsers));
                }
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
            var savedMonitors = Settings.Default.QC_Monitors;
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
            Settings.Default.QC_Monitors = string.Join("/", qcMonitors.Select(x => x.SaveToString()));
        }
    }
}
