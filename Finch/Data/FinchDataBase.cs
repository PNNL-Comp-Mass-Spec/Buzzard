using System.Collections.Generic;
using System;

namespace Finch.Data
{
    /// <summary>
    /// Represents the base of measurement data.
    /// </summary>
    public abstract class FinchSignalBase
    {

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FinchSignalBase()
        {
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public FinchDataType Type {get; set;}
        /// <summary>
        /// Gets or sets the name of measurement.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the last time the signal was updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Enumerates the possible types of data that the Finch framework supports.
    /// </summary>
    public enum FinchDataType
    {
        String,
        Double,
        Integer,
        DateTime,
        Boolean
    }
}
