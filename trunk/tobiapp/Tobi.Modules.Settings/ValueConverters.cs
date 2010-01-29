using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;

namespace Tobi.Plugin.Settings
{
    [ValueConversion(typeof(ValidationError), typeof(string))]
    public class ValidationErrorConverter : ValueConverterMarkupExtensionBase<ValidationErrorConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var error = value as ValidationError;

            if (error != null)
                return error.ErrorContent;

            return "";
        }
    }


    [ValueConversion(typeof(KeyGesture), typeof(string))]
    public class TextToKeyGestureConverter : ValueConverterMarkupExtensionBase<TextToKeyGestureConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return KeyGestureStringConverter.Convert((KeyGesture)value);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return KeyGestureStringConverter.Convert((string)value);
        }
    }


    [ValueConversion(typeof(double), typeof(string))]
    public class TextToDoubleConverter : ValueConverterMarkupExtensionBase<TextToDoubleConverter>
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
