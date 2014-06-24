using System;
using System.Windows.Data;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolToOppositeBoolConverter : ValueConverterMarkupExtensionBase<BoolToOppositeBoolConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        #endregion
    }
}
