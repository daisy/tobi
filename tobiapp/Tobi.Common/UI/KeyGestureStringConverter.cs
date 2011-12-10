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

        public static string Convert(Key key, ModifierKeys modKeys, bool useFriendlyKeyDisplayString)
        {
            string str = "[ ";

            bool hasModKey = false;
            if ((modKeys & ModifierKeys.Shift) != ModifierKeys.None)
            {
                str += "SHIFT ";
                hasModKey = true;
            }
            if ((modKeys & ModifierKeys.Control) != ModifierKeys.None)
            {
                str += "CTRL ";
                hasModKey = true;
            }
            if ((modKeys & ModifierKeys.Alt) != ModifierKeys.None)
            {
                str += "ALT ";
                hasModKey = true;
            }
            if ((modKeys & ModifierKeys.Windows) != ModifierKeys.None)
            {
                str += "WIN ";
                hasModKey = true;
            }
            if (!hasModKey)
            {
                str += "NONE ";
            }

            str += "] ";
            str += key;

            char ch = '\0';
            if (useFriendlyKeyDisplayString
                && (ch = KeyGestureString.GetDisplayChar(key)) != '\0'
                && (key < Key.A || key > Key.Z)
                && key != Key.Space
                && key != Key.Enter
                && key != Key.Return)
                str += " (" + ch + ")";

            return str;
        }

        public static string Convert(KeyGesture keyG)
        {
            if (keyG == null)
            {
                return null;
            }

            return Convert(keyG.Key, keyG.Modifiers, true);
        }

        public static KeyGestureString Convert(string str)
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

            int spaceIndex = keyStr.IndexOf(' ');
            if (spaceIndex >= 0)
            {
                keyStr = keyStr.Substring(0, spaceIndex);
            }

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
                //return null; WORKS for SOME gestures, not all (e.g. F1-F12 work fine, but not alpha keys).
            }

            try
            {
                var keyG = new KeyGestureString(key, modKey);
                return keyG;
            }
            catch
            {
                Console.WriteLine(@"!! not a valid KeyGesture: " + str);
                //Debug.Fail(@"Not a valid KeyGesture: " + str);
            }

            return null;
        }
    }
}
