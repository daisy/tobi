using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(TextAlignment), typeof(String))]
    public class TextAlignmentToStringConverter : ValueConverterMarkupExtensionBase<TextAlignmentToStringConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            //if (targetType != typeof(String))
            //    throw new InvalidOperationException("The target must be String !");

            return ((TextAlignment)value).ToString();
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            //if (targetType != typeof(TextAlignment))
            //    throw new InvalidOperationException("The target must be a TextAlignment !");

            String str = ((String)value).ToLower();

            //TextAlignment.TryParse(str)

            if (str == "center" || str == "middle") return TextAlignment.Center;
            if (str == "left") return TextAlignment.Left;
            if (str == "right") return TextAlignment.Right;
            if (str == "justify") return TextAlignment.Justify;
            
            return String.Empty; // will generate exception
        }

        #endregion
    }

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
                return String.Empty; // will generate exception
            }
        }
    }
}
