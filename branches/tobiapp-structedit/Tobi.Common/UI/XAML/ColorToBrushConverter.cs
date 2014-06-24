using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;

namespace Tobi.Common.UI.XAML
{
    public static class ColorBrushCache
    {
        private static Dictionary<
#if USE_COLOR_STRING
            String
#else
Color
#endif //USE_COLOR_STRING
, SolidColorBrush> m_SolidColorBrushCache;
        public static SolidColorBrush Get(Color color)
        {
            if (m_SolidColorBrushCache == null)
            {
                m_SolidColorBrushCache = new Dictionary<
#if USE_COLOR_STRING
            String
#else
Color
#endif //USE_COLOR_STRING
, SolidColorBrush>();
            }

#if USE_COLOR_STRING
            string colorString = color.ToString();
#else
            Color colorKey = color;
#endif //USE_COLOR_STRING

            SolidColorBrush obj;
            m_SolidColorBrushCache.TryGetValue(colorKey, out obj);

            if (obj == null) //!m_SolidColorBrushCache.ContainsKey(colorString))
            {
                bool found = false;
                foreach (PropertyInfo propertyInfo in typeof(Brushes).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (propertyInfo.PropertyType == typeof(SolidColorBrush))
                    {
                        var brush = (SolidColorBrush)propertyInfo.GetValue(null, null);
                        if (brush.Color
#if USE_COLOR_STRING
                            .ToString()
#endif //USE_COLOR_STRING
 == colorKey)
                        {
                            found = true;
                            obj = brush;
                            obj.Freeze();
                            m_SolidColorBrushCache.Add(colorKey, obj);
                            break;
                        }
                    }
                }
                if (!found)
                {
                    obj = new SolidColorBrush(color);
                    obj.Freeze();
                    m_SolidColorBrushCache.Add(colorKey, obj);
                }
            }

            return obj; // m_SolidColorBrushCache[colorString];
        }
    }

    [ValueConversion(typeof(Color), typeof(Brush))]
    public class ColorToBrushConverter : ValueConverterMarkupExtensionBase<ColorToBrushConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is Color)) return null;

            //            var scb = new SolidColorBrush((Color)value);

            return ColorBrushCache.Get((Color)value);
        }

        #endregion
    }
}
