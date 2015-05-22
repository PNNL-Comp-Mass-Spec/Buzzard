using System.Collections.Generic;

namespace Finch.Data
{
    /// <summary>
    /// Component class (e.g. device)
    /// </summary>
    public class FinchComponentData: FinchComponentDataBase
    {
        public FinchComponentData()
            : base()
        {
            Signals = new List<FinchSignalBase>();
        }
        /// <summary>
        /// Gets or sets the type of a component.
        /// </summary>
        public new string Type
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the list of Signals associated with this component.
        /// </summary>
        public List<FinchSignalBase> Signals
        {
            get;
            set;
        }
    }       
}
