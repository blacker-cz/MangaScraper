using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace Blacker.MangaScraper.Converters
{
    /// <summary>
    /// Convertor for converting string bool value to bool value
    /// </summary>
    class BooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return Boolean.Parse(parameter.ToString()) == (bool) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return Boolean.Parse(parameter.ToString()) == (bool) value;
        }
    }
}
