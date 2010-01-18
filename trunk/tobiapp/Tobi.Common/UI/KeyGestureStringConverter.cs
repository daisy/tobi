using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace Tobi.Common.UI
{
    public class KeyGestureStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                KeyGesture keyG = Convert((string)value);
                return keyG;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return Convert((KeyGesture)value);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public static string Convert(KeyGesture keyG)
        {
            if (keyG == null)
            {
                return null;
            }

            string str = "[ ";
            if ((keyG.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
            {
                str += "SHIFT ";
            }
            if ((keyG.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
            {
                str += "CTRL ";
            }
            if ((keyG.Modifiers & ModifierKeys.Alt) != ModifierKeys.None)
            {
                str += "ALT ";
            }
            if ((keyG.Modifiers & ModifierKeys.Windows) != ModifierKeys.None)
            {
                str += "WIN ";
            }
            str += "] ";
            str += keyG.Key;
            return str;
        }

        public static KeyGesture Convert(string str)
        {
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }

            TypeConverter keyConverter = TypeDescriptor.GetConverter(typeof(Key));
            if (keyConverter == null)
            {
                return null;
            }

            string lowStr = str.ToLower();

            int modStart = lowStr.IndexOf('[');
            if (modStart < 0 || modStart >= lowStr.Length - 3)
            {
                return null;
            }

            int modEnd = lowStr.IndexOf(']');
            if (modEnd < 0 || modEnd <= modStart || modEnd >= lowStr.Length - 2)
            {
                return null;
            }

            string modStr = lowStr.Substring(modStart, modEnd - modStart);

            bool isModCtrl = modStr.Contains("ctrl");
            bool isModShift = modStr.Contains("shift");
            bool isModAlt = modStr.Contains("alt");
            bool isModWin = modStr.Contains("win");

            string keyStr = lowStr.Substring(modEnd + 1).Trim();

            Key key = Key.None;
            try
            {
                key = (Key)keyConverter.ConvertFromString(keyStr);
            }
            catch
            {
                Console.WriteLine(@"!! invalid modifier key string: " + keyStr);
                key = Key.None;
            }
            if (key == Key.None)
            {
                return null;
            }

            ModifierKeys modKey = ModifierKeys.None;
            if (isModShift) modKey |= ModifierKeys.Shift;
            if (isModCtrl) modKey |= ModifierKeys.Control;
            if (isModAlt) modKey |= ModifierKeys.Alt;
            if (isModWin) modKey |= ModifierKeys.Windows;

            if (modKey == ModifierKeys.None)
            {
                return null;
            }

            try
            {
                var keyG = new KeyGestureString(key, modKey);
                return keyG;
            }
            catch
            {
                Console.WriteLine(@"!! not a valid KeyGesture: " + str);
                return null;
            }
        }
    }
}
