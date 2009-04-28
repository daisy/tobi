using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Tobi.Modules.ToolBars
{
    public class FrameworkElementToVisualBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var frameworkElement = value as FrameworkElement;
            if (frameworkElement == null)
            {
                return null;
            }
            return new VisualBrush(frameworkElement);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}