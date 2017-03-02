using System.Collections.Generic;
using System;

namespace Finch.Data
{
    /// <summary>
    /// Base class for a finch component.
    /// </summary>
    public abstract class FinchComponentDataBase
    {
        /// <summary>
        /// Constructor for building a component.
        /// </summary>
        public FinchComponentDataBase()
        {
            Name    = "";
            Status  = "";
            Error   = null;
        }
        /// <summary>
        /// Gets or sets the component having an error.
        /// </summary>
        public string Error
        {
            get;
            set;
        }
        /// <summary>
        /// Gets whether the component has an error.
        /// </summary>
        public bool HasError
        {
            get { return Error != null; }
        }
        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        public string Name
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the status of the component.
        /// </summary>
        public string Status
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the last time it was updated.
        /// </summary>
        public DateTime LastUpdate
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type
        {
            get;
            set;
        }            
    }    
}
