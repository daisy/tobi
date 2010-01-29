using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(bool), typeof(WindowState))]
    public class WindowStateFullScreenConverter : ValueConverterMarkupExtensionBase<WindowStateFullScreenConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(WindowState))
                throw new InvalidOperationException("The target must be a WindowState !");

            return ((bool)value ? WindowState.Maximized : WindowState.Normal);
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be an enum WindowState !");

            return (WindowState) value == WindowState.Maximized;
        }

        #endregion
    }
}
