﻿using BuzzardWPF.Data;
using LcmsNetData.Data;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// RequestedRun data container
    /// </summary>
    public class RequestedRun : IRequestedRunData
    {
        public RequestedRun()
        {
            DmsData = new DMSData();
        }

        /// <inheritdoc cref="IRequestedRunData"/>>
        public IDmsData DmsBasicData => DmsData;

        /// <summary>
        /// DMSData instance
        /// </summary>
        public DMSData DmsData { get; set; }
    }
}
