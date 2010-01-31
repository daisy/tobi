using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;

namespace Tobi.Common.UI
{
    [TypeConverter(typeof(KeyGestureStringConverter))]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class KeyGestureString : KeyGesture
    {
        public KeyGestureString(Key key)
            : base(key)
        {
        }

        public KeyGestureString(Key key, ModifierKeys modifiers)
            : base(key, modifiers)
        {
        }

        public KeyGestureString(Key key, ModifierKeys modifiers, string displayString)
            : base(key, modifiers, displayString)
        {
        }

        public override string ToString()
        {
            return KeyGestureStringConverter.Convert(this);
        }

        public static bool AreEqual(KeyGesture keyG1, KeyGesture keyG2)
        {
            if (keyG1 == null || keyG2 == null) return false;
            return keyG1.Key == keyG2.Key
                && keyG1.Modifiers == keyG2.Modifiers;
        }

        public override bool Equals(object obj)
        {
            var otherKG = obj as KeyGestureString;
            return AreEqual(this, otherKG);
        }

        public string GetDisplayString()
        {
            return GetDisplayString(this);
        }

        public static char GetDisplayChar(Key key)
        {
            char c = '\0';
            try
            {
                int vk = KeyInterop.VirtualKeyFromKey(key);
                c = GetChar(vk);
            }
            catch
            {
                Console.WriteLine(@"!!! Key to char conversion error: " + key);
            }

            return c;
        }

        public static string GetDisplayString(KeyGesture keyG)
        {
            if (keyG == null)
            {
                return null;
            }

            string strDisplay = keyG.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
            if (!strDisplay.ToLower().Contains("oem"))
            {
                return strDisplay;
            }

            //Keys modKey = Keys.None;
            //if ((keyG.Modifiers & ModifierKeys.Shift) != ModifierKeys.None) modKey |= Keys.Shift;
            //if ((keyG.Modifiers & ModifierKeys.Control) != ModifierKeys.None) modKey |= Keys.Control;
            //if ((keyG.Modifiers & ModifierKeys.Alt) != ModifierKeys.None) modKey |= Keys.Alt;
            //if ((keyG.Modifiers & ModifierKeys.Windows) != ModifierKeys.None) modKey |= Keys.LWin;
            //modKey = Keys.None;


            char c = GetDisplayChar(keyG.Key);

            var converter = new KeyConverter();
            string strKey = converter.ConvertToString(keyG.Key);
            //string strKey = (string)kc.ConvertTo(keyG.Key, typeof(string));

            if (c == '\0')
            {
#if DEBUG
                Debugger.Break();
#endif
                return strDisplay;
            }

            return strDisplay.Replace(strKey, c + "");
        }




        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] 
          StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        private const byte HighBit = 0x80;
        private static byte[] GetKeyboardState(Keys modifiers)
        {
            var keyState = new byte[256];
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if ((modifiers & key) == key)
                {
                    keyState[(int)key] = HighBit;
                }
            }
            return keyState;
        }
        private static char GetChar(int vk)
        {
            //var ks = new byte[256];
            //GetKeyboardState(ks);

            //var sc = MapVirtualKey((uint)vk, MapType.MAPVK_VK_TO_VSC);
            var sb = new StringBuilder(2);
            var ch = (char)0;

            switch (ToUnicode((uint)vk,
                0,
                GetKeyboardState(Keys.None),
                sb,
                sb.Capacity,
                0))
            {
                case -1: break;
                case 0: break;
                case 1:
                    {
                        ch = sb[0];
                        break;
                    }
                default:
                    {
                        ch = sb[0];
                        break;
                    }
            }
            return ch;
        }
    }
}
