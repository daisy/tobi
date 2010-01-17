using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Tobi.Common.UI
{
    public partial class KeyGestureSinkBox
    {
        public KeyGestureSinkBox()
        {
            InitializeComponent();
        }

        public string KeyGestureSerializedEncoded { get; private set; }

        public KeyGesture KeyGesture
        {
            get
            {
                return Convert(KeyGestureSerializedEncoded);
            }
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


            char c = '\0';
            try
            {
                int vk = KeyInterop.VirtualKeyFromKey(keyG.Key);
                c = GetChar(vk);
            }
            catch
            {
                Console.WriteLine(@"!!! Key to char conversion error: " + keyG.Key);
            }

            var converter = new KeyConverter();
            string strKey = converter.ConvertToString(keyG.Key);
            //string strKey = (string)kc.ConvertTo(keyG.Key, typeof(string));

            if (c == '\0')
            {
#if DEBUG
                Debugger.Break();
#endif
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
            if (string.IsNullOrEmpty(str))
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
                var keyG = new KeyGesture(key, modKey);
                return keyG;
            }
            catch
            {
                Console.WriteLine(@"!! not a valid KeyGesture: " + str);
                return null;
            }
        }

        private void OnPreviewKeyDown_TextBox(object sender, KeyEventArgs e)
        {
            //if (e.Key != Key.Tab)
            //{
            //    e.Handled = true;
            //}
        }
        private void OnKeyDown_TextBox(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            bool isTab = key == Key.Tab;

            string keyStr = key.ToString().ToLower();
            if (keyStr.Contains("ctrl") || keyStr.Contains("shift") || keyStr.Contains("alt"))
            {
                key = Key.None;
            }
            if (key == Key.Escape || key == Key.Tab)
            {
                key = Key.None;
            }

            string common =
                            ((Keyboard.Modifiers & ModifierKeys.Shift) > 0 ? "SHIFT " : "")
                            +
                            ((Keyboard.Modifiers & ModifierKeys.Control) > 0 ? "CTRL " : "")
                            +
                            ((Keyboard.Modifiers & ModifierKeys.Alt) > 0 ? "ALT " : "")
                            +
                            ((Keyboard.Modifiers & ModifierKeys.Windows) > 0 ? "WIN " : "")
                            ;

            KeyGestureSerializedEncoded = "[ " + common + "] " + (key != Key.None ? key.ToString() : "");

            var converter = new KeyConverter();
            string keyDisplayString = converter.ConvertToString(key);
            Text = common + (key != Key.None ?
                (!keyDisplayString.ToLower().Contains("oem") ?
                keyDisplayString :
                ("" + GetChar(KeyInterop.VirtualKeyFromKey(key))).ToUpper()) :
                "");

            var keyG = Convert(KeyGestureSerializedEncoded);

            ToolTip = keyG == null ? null : GetDisplayString(keyG);

            Console.WriteLine("\n" + @"=====> "
                + (keyG == null ? "INVALID" : ToolTip.ToString())
                + @" <==> "
                + (keyG == null ? "INVALID" : Convert(keyG)));

            if (!isTab)
            {
                e.Handled = true;
            }
        }

        private void OnPreviewKeyUp_TextBox(object sender, KeyEventArgs e)
        {
            //if (e.Key != Key.Tab)
            //{
            //    e.Handled = true;
            //}
        }
        private void OnKeyUp_TextBox(object sender, KeyEventArgs e)
        {
            Console.WriteLine(@"------------");

            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));
            if (key != Key.Tab)
            {
                e.Handled = true;
            }
        }
    }
}
