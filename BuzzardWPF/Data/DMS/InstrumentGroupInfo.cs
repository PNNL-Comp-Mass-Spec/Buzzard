﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BuzzardWPF.Data.DMS
{
    /// <summary>
    /// Class to hold data about an instrument group
    /// </summary>
    [Serializable]
    public class InstrumentGroupInfo : IEquatable<InstrumentGroupInfo>, ICloneable
    {
        /// <summary>
        /// Instrument Group name as used in DMS
        /// </summary>
        [PersistenceSetting(IsUniqueColumn = true)]
        public string InstrumentGroup { get; set; }

        /// <summary>
        /// Default dataset type for the group, as specified in DMS
        /// </summary>
        public string DefaultDatasetType { get; set; }

        /// <summary>
        /// A comma-separated list of dataset types allowed in DMS for this instrument group
        /// </summary>
        public string AllowedDatasetTypes { get; set; }

        [PersistenceSetting(IgnoreProperty = true)]
        public List<string> AllowedDatasetTypesList =>
            AllowedDatasetTypes?.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList() ??
            new List<string>();

        /// <summary>
        /// Clone - make a deep copy
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            // Member-wise clone is sufficient
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return InstrumentGroup;
        }

        public bool Equals(InstrumentGroupInfo other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return InstrumentGroup == other.InstrumentGroup;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InstrumentGroupInfo) obj);
        }

        public override int GetHashCode()
        {
            return InstrumentGroup?.GetHashCode() ?? 0;
        }
    }
}
