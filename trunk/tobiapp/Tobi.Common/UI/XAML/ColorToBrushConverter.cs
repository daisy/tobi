using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(Color), typeof(Brush))]
    public class ColorToBrushConverter : ValueConverterMarkupExtensionBase<ColorToBrushConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is Color)) return null;

            var scb = new SolidColorBrush((Color)value);
            return scb;
        }

        #endregion
    }
}
