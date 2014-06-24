using System;
using System.Windows;
using System.Windows.Data;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(bool), typeof(FlowDirection))]
    public class WindowFlowDirectionConverter : ValueConverterMarkupExtensionBase<WindowFlowDirectionConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(FlowDirection))
                throw new InvalidOperationException("The target must be a WindowState !");

            return ((bool)value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be Boolean !");

            return (FlowDirection)value == FlowDirection.RightToLeft;
        }

        #endregion
    }
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
                throw new InvalidOperationException("The target must be Boolean !");

            return (WindowState) value == WindowState.Maximized;
        }

        #endregion
    }
}
