using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tobi.Common.UI.XAML
{
    [ValueConversion(typeof(Enum), typeof(String))]
    public class EnumToStringConverter : ValueConverterMarkupExtensionBase<EnumToStringConverter>
    {
        #region IValueConverter Members

        private static List<Type> m_EnumTypes = new List<Type>();

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            //if (targetType != typeof(String))
            //    throw new InvalidOperationException("The target must be String !");

            var typez = value.GetType();
            if (!m_EnumTypes.Contains(typez))
            {
                m_EnumTypes.Add(typez);
            }

            return ((Enum)value).ToString();
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != null && targetType.Name != "Object" && typeof(Enum).IsAssignableFrom(targetType))
            {
                var obj = Enum.Parse(targetType, value.ToString(), true);
                return obj;
            }
            else if (m_EnumTypes.Count == 0)
            {
                var obj = Enum.Parse(typeof(Enum), value.ToString(), true);
                return obj;
            }
            else
            {
                foreach (var enumType in m_EnumTypes)
                {
                    try
                    {
                        var obj = Enum.Parse(enumType, value.ToString(), true);
                        return obj;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return string.Empty;
        }

        #endregion
    }

    //[ValueConversion(typeof(TextAlignment), typeof(String))]
    //public class TextAlignmentToStringConverter : ValueConverterMarkupExtensionBase<TextAlignmentToStringConverter>
    //{

    //    #region IValueConverter Members

    //    public override object Convert(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        //if (targetType != typeof(String))
    //        //    throw new InvalidOperationException("The target must be String !");

    //        return ((TextAlignment)value).ToString();
    //    }

    //    public override object ConvertBack(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        //if (targetType != typeof(TextAlignment))
    //        //    throw new InvalidOperationException("The target must be a TextAlignment !");

    //        String str = ((String)value).ToLower();

    //        //TextAlignment.TryParse(str)

    //        if (str == "center" || str == "middle") return TextAlignment.Center;
    //        if (str == "left") return TextAlignment.Left;
    //        if (str == "right") return TextAlignment.Right;
    //        if (str == "justify") return TextAlignment.Justify;

    //        return String.Empty; // will generate exception
    //    }

    //    #endregion
    //}

    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToTextConverter : ValueConverterMarkupExtensionBase<DoubleToTextConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return Double.Parse((string)value);
            }
            catch
            {
                return String.Empty; // will generate exception
            }
        }
    }
}
