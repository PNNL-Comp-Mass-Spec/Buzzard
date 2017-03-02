using System.Collections.Generic;

namespace Finch.Data
{
    /// <summary>
    /// Aggregate (e.g. instrument)
    /// </summary>
    public class FinchAggregateData: FinchComponentDataBase
    {
        public FinchAggregateData()
            : base()
        {
            Components  = new List<FinchComponentData>();
            DisplayMaps = new List<FinchDisplayMap>();
        }
        /// <summary>
        /// Gets or sets
        /// </summary>
        public List<FinchComponentData> Components
        {
            get;
            set;
        }

        public List<FinchDisplayMap> DisplayMaps
        {
            get;
            set;
        }
        
    }

    public class FinchDisplayMap
    {
        public string ComponentName
        {
            get;
            set;
        }
        public string TrueValue
        {
            get;
            set;
        }
        public string FalseValue
        {
            get;
            set;
        }
    }
}

