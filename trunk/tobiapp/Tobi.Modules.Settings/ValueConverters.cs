using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using Tobi.Common.UI;

namespace Tobi.Plugin.Settings
{
    public class ValidationErrorConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var error = value as ValidationError;

            if (error != null)
                return error.ErrorContent;

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class TextToKeyGestureConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return KeyGestureStringConverter.Convert((KeyGesture)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return KeyGestureStringConverter.Convert((string)value);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class TextToDoubleConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
