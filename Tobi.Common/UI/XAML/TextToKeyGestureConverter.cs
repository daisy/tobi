using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace Tobi.Common.UI.XAML
{
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
}
