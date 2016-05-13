using System.Collections.Generic;

namespace Finch.Data
{
    /// <summary>
    /// Represents a collection of data for plotting.
    /// </summary>
    public class FinchDataTuple : FinchSignalBase
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public FinchDataTuple()
        {            
            XValues = new List<string>();
            YValues = new List<string>();
        }
        /// <summary>
        /// Gets or sets the data type for the x-component.
        /// </summary>
        public FinchDataType XDataType
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the data type for the y-component.
        /// </summary>
        public FinchDataType YDataType
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the X-unit type.
        /// </summary>
        public string XUnits { get; set; }
        /// <summary>
        /// Gets or sets the Y-unit type.
        /// </summary>
        public string YUnits { get; set; }
        /// <summary>
        /// Gets or sets a list of x-values.
        /// </summary>
        public List<string> XValues {get;set;}
        /// <summary>
        /// Gets or sets a list of y-values.
        /// </summary>
        public List<string> YValues {get;set;}

        /// <summary>
        /// Adds the Y data to the arrays. 
        /// </summary>
        /// <typeparam name="U">Type of data to add.</typeparam>
        /// <param name="values">Values to add</param>
        /// <param name="format">Format (string.format) if you want to specify the format of the data, e.g. doubles -> "{0:0.00}"</param>
        public void SetY<U>(List<U> values, string format)
        {
            SetData<U>(YValues, values, format);
        }
        /// <summary>
        /// Adds the X data to the arrays. 
        /// </summary>
        /// <typeparam name="U">Type of data to add.</typeparam>
        /// <param name="values">Values to add</param>
        /// <param name="format">Format (string.format) if you want to specify the format of the data, e.g. doubles -> "{0:0.00}"</param>
        public void SetX<U>(List<U> values, string format)
        {
            SetData<U>(XValues, values, format);
        }
        /// <summary>
        /// Adds the Y data to the arrays.
        /// </summary>
        /// <param name="values"></param>
        public void SetY<U>(List<U> values)
        {
            SetData<U>(YValues, values, null);
        }
        /// <summary>
        /// Adds the X data to the arrays.
        /// </summary>
        /// <param name="values"></param>
        public void SetX<U>(List<U> values)
        {
            SetData<U>(XValues, values, null);
        }   
        /// <summary>
        /// Adds the data to the specified array.
        /// </summary>
        /// <typeparam name="U">Type of data to add</typeparam>
        /// <param name="data">Data to add to.</param>
        /// <param name="values">Values to add</param>
        /// <param name="format">Format if any to format the values in</param>
        private void SetData<U>(List<string> data, List<U> values, string format)
        {
            if (format == null)
            {
                foreach (U value in values)
                {
                    data.Add(value.ToString());
                }
            }
            else
            {
                foreach (U value in values)
                {
                    data.Add(string.Format(format, value));
                }
            }
        }     
    }

}
