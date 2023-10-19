using System;
using System.ComponentModel;

namespace BuzzardWPF.Data.DMS
{
    /// <summary>
    /// These values come from table T_EUS_UsageType
    /// It is rarely updated, so we're not querying the database every time
    /// Previously used, but deprecated in April 2017 is USER_UNKNOWN
    /// Previously used, but deprecated in April 2021 is USER (Replaced with USER_ONSITE and USER_REMOTE)
    /// </summary>
    public enum EmslUsageType
    {
        [Description("Invalid usage type")]
        NONE,

        [Description("Broken/Out of service")]
        BROKEN,

        [Description("Capability Development")]
        CAP_DEV,

        [Description("Maintenance")]
        MAINTENANCE,

        [Description("EMSL User Project P.I. is/was on-site and ran the instrument themselves for the sample. PNNL staff P.I. is always onsite.")]
        USER_ONSITE,

        [Description("EMSL User Project P.I. is off-site, PNNL staff ran the instrument for the sample")]
        USER_REMOTE,

        [Description("Resource Owner - generally non-EMSL instrument with no EMSL User Project")]
        RESOURCE_OWNER,
    }

    public static class EmslUsageTypeExtensions
    {
        public static bool IsUserType(this EmslUsageType usageType)
        {
            return usageType == EmslUsageType.USER_ONSITE || usageType == EmslUsageType.USER_REMOTE;
        }

        /// <summary>
        /// Tries parsing string to <see cref="EmslUsageType"/>; if it fails, returns <see cref="EmslUsageType.NONE"/>
        /// </summary>
        /// <param name="usageTypeText"></param>
        /// <returns></returns>
        public static EmslUsageType ToEmslUsageType(this string usageTypeText)
        {
            if (Enum.TryParse(usageTypeText, out EmslUsageType eusType))
            {
                return eusType;
            }

            return EmslUsageType.NONE;
        }
    }
}