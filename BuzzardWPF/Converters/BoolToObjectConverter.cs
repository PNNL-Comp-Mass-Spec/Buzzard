using System;
using System.Globalization;
using System.Windows.Data;

namespace BuzzardWPF.Converters
{
    /// <summary>
    /// This Value Converter takes in a boolean value and converts
    /// it to the object of your choice. This converter only works
    /// for one way bindings of source to target. Using this in any
    /// bindings that updates the source based on changes to the
    /// target value will throw a NotImplementedException.
    /// </summary>
    [ValueConversion(typeof(bool?), typeof(object))]
    public class BoolToObjectConverter
        : IValueConverter
    {
        #region Attributes
        private object m_trueContent;
        private object m_falseContent;
        private object m_nullContent;
        #endregion

        #region Constructors
        public BoolToObjectConverter()
        {
            TrueContent = "True";
            FalseContent = "False";
            NullContent = "Null";
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets and Sets the object you want to convert to when
        /// the input value is True.
        /// </summary>
        public object TrueContent
        {
            get { return m_trueContent; }
            set { m_trueContent = value; }
        }

        /// <summary>
        /// Gets and Sets the object you want to convert to when
        /// the input value is False.
        /// </summary>
        public object FalseContent
        {
            get { return m_falseContent; }
            set { m_falseContent = value; }
        }

        /// <summary>
        /// Gets or Sets the object you want to convert to
        /// when the input value is null.
        /// </summary>
        public object NullContent
        {
            get { return m_nullContent; }
            set { m_nullContent = value; }
        }
        #endregion

        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value as bool?;

            if (input == null || !input.HasValue)
                return NullContent;
            if (input.Value)
                return TrueContent;
            return FalseContent;
        }

        /// <summary>
        /// Convert back is NOT enabled.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
