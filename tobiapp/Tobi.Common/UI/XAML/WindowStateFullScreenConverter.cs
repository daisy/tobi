using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Tobi.Common.UI.XAML
{
    public class WindowStateFullScreenConverter : MarkupExtension, IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(WindowState))
                throw new InvalidOperationException("The target must be a boolean !");

            return ((bool)value ? WindowState.Maximized : WindowState.Normal);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be an enum WindowState !");

            return (WindowState) value == WindowState.Maximized;
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
