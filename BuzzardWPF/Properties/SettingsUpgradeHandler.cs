﻿using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

// ReSharper disable InconsistentNaming

namespace BuzzardWPF.Properties
{
    internal sealed partial class Settings
    {
        public override void Upgrade()
        {
            // Load existing compatible settings
            base.Upgrade();

            // Upgrade settings that have been renamed.
            var oldSettings = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
                    var valueEmpty = string.IsNullOrWhiteSpace(previousValue.ToString());
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
                        if (previousValue is StringCollection osc && setting.PropertyType == typeof(string))
                        {
                            // Old property was StringCollection, new one is just 'string', just keep the first item in the StringCollection
                            previousValue = osc[0];
                        }

                        this[setting.Name] = previousValue;
                    }
                }

                // Remove the old property from the collections so they are not saved
                PropertyValues.Remove(oldSetting.Property.Name);
                Properties.Remove(oldSetting.Property.Name);
            }

            // Upgrade obsolete values for some settings
            UpgradeSettingValue(nameof(FilldownEMSLUsageType), "USER", "USER_ONSITE");
            UpgradeSettingValue(nameof(WatcherEMSLUsageType), "USER", "USER_ONSITE");

            // Persist the upgraded settings.
            Save();
            Reload();
        }

        private void UpgradeSettingValue(string settingName, string oldValue, string replacementValue)
        {
            var settingValue = this[settingName];
            if (!(settingValue is string value) || string.IsNullOrWhiteSpace(value))
            {
                // This should only be encountered when Upgrade is called twice.
                return;
            }

            if (value.Equals(oldValue))
            {
                this[settingName] = replacementValue;
            }
        }

        #region Obsolete Settings Properties

        [AttributeUsage(AttributeTargets.All)]
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
        [Obsolete("Use  " + nameof(FilldownEMSLUser), true)]
        [SettingUpgradeName(nameof(FilldownEMSLUser))]
        // ReSharper disable once UnusedMember.Global
        public StringCollection FilldownEMSLUsers { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use  " + nameof(WatcherEMSLUser), true)]
        [SettingUpgradeName(nameof(WatcherEMSLUser))]
        // ReSharper disable once UnusedMember.Global
        public StringCollection WatcherEMSLUsers { get; set; }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        [NoSettingsVersionUpgrade]
        [Obsolete("Use  " + nameof(DMSInstrumentHostName), true)]
        [SettingUpgradeName(nameof(DMSInstrumentHostName))]
        // ReSharper disable once UnusedMember.Global
        public string InstName { get; set; }

        #endregion
    }
}
