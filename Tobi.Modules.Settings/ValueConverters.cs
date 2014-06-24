using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
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
}
