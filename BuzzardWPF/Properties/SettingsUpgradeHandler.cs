using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BuzzardWPF.Properties
{
    internal sealed partial class Settings
    {
        public override void Upgrade()
        {
            // Load existing compatible settings
            base.Upgrade();

            // Upgrade settings that have been renamed.
            var oldSettings = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => new
                {
                    Property = x,
                    UpgradeName = x.GetCustomAttributes<SettingUpgradeNameAttribute>(true).FirstOrDefault()
                })
                .Where(x => x.UpgradeName != null);

            foreach (var oldSetting in oldSettings)
            {
                var settingData = Properties[oldSetting.Property.Name];
                if (settingData == null)
                {
                    // This should only be encountered when Upgrade is called twice.
                    continue;
                }

                // Only try to upgrade settings that are not the default value
                var previousValue = GetPreviousVersion(oldSetting.Property.Name);
                var valueDefault = false;
                if (string.IsNullOrWhiteSpace(previousValue?.ToString()))
                {
                    valueDefault = true;
                }
                else if (settingData.DefaultValue != null && settingData.PropertyType != typeof(StringCollection))
                {
                    var defaultEmpty = string.IsNullOrWhiteSpace(settingData.DefaultValue.ToString());
                    var valueEmpty = string.IsNullOrWhiteSpace(previousValue?.ToString());
                    if (defaultEmpty && valueEmpty)
                    {
                        valueDefault = true;
                    }

                    if (!defaultEmpty && !valueEmpty)
                    {
                        if (settingData.DefaultValue.Equals(previousValue.ToString()))
                        {
                            valueDefault = true;
                        }
                    }
                }
                else if (previousValue is StringCollection sc && sc.Count == 0)
                {
                    valueDefault = true;
                }

                // Only overwrite the upgrade setting if it is still the default value
                var setting = Properties[oldSetting.UpgradeName.Name];
                if (!valueDefault && setting != null)
                {
                    if (string.Equals(setting.DefaultValue?.ToString(), this[setting.Name]?.ToString())
                        || this[setting.Name] is StringCollection sc && sc.Count == 0)
                    {
                        this[oldSetting.UpgradeName.Name] = previousValue;
                    }
                }

                // Remove the old property from the collections so they are not saved
                PropertyValues.Remove(oldSetting.Property.Name);
                Properties.Remove(oldSetting.Property.Name);
            }

            // Persist the upgraded settings.
            Save();
            Reload();
        }

        #region Obsolete Settings Properties

        private class SettingUpgradeNameAttribute : Attribute
        {
            public string Name { get; }

            public SettingUpgradeNameAttribute(string name)
            {
                Name = name;
            }
        }

        private class SettingCombinedNameAttribute : SettingUpgradeNameAttribute
        {
            public SettingCombinedNameAttribute(string name) : base(name)
            {
            }
        }

        // https://www.codeproject.com/Articles/247333/Renaming-User-Settings-Properties-between-Software
        /* Example
        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValueAttribute("")]
        [Obsolete("Please use NewUserValue instead")]
        [NoSettingsVersionUpgrade]
        public string OldUserValue
        {
            get { throw new NotSupportedException("OldUserValue is obsolete"); }
            set { throw new NotSupportedException("OldUserValue is obsolete"); }
        }
        */

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use  " + nameof(FilldownEMSLUsageType), true)]
        [SettingUpgradeName(nameof(FilldownEMSLUsageType))]
        // ReSharper disable once UnusedMember.Global
        public string FilldownEMSLUsage { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use  " + nameof(WatcherCartName), true)]
        [SettingUpgradeName(nameof(WatcherCartName))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_SelectedCartName { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherCartConfigName), true)]
        [SettingUpgradeName(nameof(WatcherCartConfigName))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_SelectedCartConfigName { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherColumn), true)]
        [SettingUpgradeName(nameof(WatcherColumn))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_LCColumn { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherComment), true)]
        [SettingUpgradeName(nameof(WatcherComment))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_UserComment { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherDatasetType), true)]
        [SettingUpgradeName(nameof(WatcherDatasetType))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_SelectedColumnData { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherEMSLProposalID), true)]
        [SettingUpgradeName(nameof(WatcherEMSLProposalID))]
        // ReSharper disable once UnusedMember.Global
        public string Watcher_EMSL_ProposalID { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherEMSLUsageType), true)]
        [SettingUpgradeName(nameof(WatcherEMSLUsageType))]
        // ReSharper disable once UnusedMember.Global
        public string Watcher_EMSL_UsageType { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherEMSLUsers), true)]
        [SettingUpgradeName(nameof(WatcherEMSLUsers))]
        // ReSharper disable once UnusedMember.Global
        public global::System.Collections.Specialized.StringCollection Watcher_EMSL_Users { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherExperimentName), true)]
        [SettingUpgradeName(nameof(WatcherExperimentName))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_ExperimentName { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherInstrument), true)]
        [SettingUpgradeName(nameof(WatcherInstrument))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_SelectedInstrument { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherOperator), true)]
        [SettingUpgradeName(nameof(WatcherOperator))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_SelectedOperator { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherSeparationType), true)]
        [SettingUpgradeName(nameof(WatcherSeparationType))]
        // ReSharper disable once UnusedMember.Global
        public string WatcherConfig_SelectedSeperationType { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherQCExperimentName), true)]
        [SettingUpgradeName(nameof(WatcherQCExperimentName))]
        // ReSharper disable once UnusedMember.Global
        public string QC_ExperimentName { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherQCMonitors), true)]
        [SettingUpgradeName(nameof(WatcherQCMonitors))]
        // ReSharper disable once UnusedMember.Global
        public string QC_Monitors { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherQCCreateTriggerOnDMSFail), true)]
        [SettingUpgradeName(nameof(WatcherQCCreateTriggerOnDMSFail))]
        // ReSharper disable once UnusedMember.Global
        public bool QC_CreateTriggerOnDMS_Fail { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(WatcherCreateTriggerOnDMSFail), true)]
        [SettingUpgradeName(nameof(WatcherCreateTriggerOnDMSFail))]
        // ReSharper disable once UnusedMember.Global
        public bool WatcherConfig_CreateTriggerOnDMS_Fail { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("C:\\")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(SearchPath), true)]
        [SettingCombinedName(nameof(SearchPath))]
        // ReSharper disable once UnusedMember.Global
        public string Watcher_WatchDir { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("TopDirectoryOnly")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(SearchDirectoryOptions), true)]
        [SettingCombinedName(nameof(SearchDirectoryOptions))]
        // ReSharper disable once UnusedMember.Global
        public global::System.IO.SearchOption Watcher_SearchType { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue(".raw")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(SearchExtension), true)]
        [SettingCombinedName(nameof(SearchExtension))]
        // ReSharper disable once UnusedMember.Global
        public string Watcher_FilePattern { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("100")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(SearchMinimumSizeKB), true)]
        [SettingCombinedName(nameof(SearchMinimumSizeKB))]
        // ReSharper disable once UnusedMember.Global
        public int Watcher_FileSize { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use " + nameof(Search_MatchFolders), true)]
        [SettingCombinedName(nameof(Search_MatchFolders))]
        // ReSharper disable once UnusedMember.Global
        public bool Watcher_MatchFolders { get; set; }

        #endregion
    }
}
