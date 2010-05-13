using System;
using System.Windows;
using System.Windows.Data;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : ValueConverterMarkupExtensionBase<BooleanToVisibilityConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a Visibility !");

            return ((bool)value ? Visibility.Visible : Visibility.Collapsed);
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be Boolean !");

            return (Visibility)value == Visibility.Visible;
        }

        #endregion
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToOppositeVisibilityConverter : ValueConverterMarkupExtensionBase<BooleanToOppositeVisibilityConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a Visibility !");

            return (!(bool)value ? Visibility.Visible : Visibility.Collapsed);
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be Boolean !");

            return (Visibility)value == Visibility.Collapsed || (Visibility)value == Visibility.Hidden;
        }

        #endregion
    }
}
