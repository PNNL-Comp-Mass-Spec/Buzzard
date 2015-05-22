using System.Collections.Generic;

namespace Finch.Data
{    
    /// <summary>
    /// A scalar signal value.
    /// </summary>
    public class FinchScalarSignal : FinchSignalBase
    {
        /// <summary>
        /// Gets or sets the unit type.
        /// </summary>
        public string Units { get; set; }
        /// <summary>
        /// Gets or sets the value of the measurement.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Gets or sets the data type for this signal.
        /// </summary>
        public new FinchDataType Type { get; set; }             
    }
}
