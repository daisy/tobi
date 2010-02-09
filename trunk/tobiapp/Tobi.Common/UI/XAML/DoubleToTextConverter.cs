using System;
using System.Globalization;
using System.Windows.Data;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToTextConverter : ValueConverterMarkupExtensionBase<DoubleToTextConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return Double.Parse((string)value);
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}
