using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
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

            //AddHandler(MouseLeftButtonUpEvent,
            //    new MouseButtonEventHandler((s, e) => FocusHelper.Focus(this, this)),
            //    true);
        }

        public string KeyGestureSerializedEncoded { get; private set; }

        public KeyGesture KeyGesture
        {
            get
            {
                return KeyGestureStringConverter.Convert(KeyGestureSerializedEncoded);
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

        private void OnMouseLeftButtonUp_TextBox(object sender, MouseButtonEventArgs e)
        {
            FocusHelper.Focus(this, this);
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

            // We ignore reserved keys (but they bubble-up in the UI tree for further processing, if any)
            if (key == Key.Escape || key == Key.Tab || key == Key.Return)
            {
                return;
            }

            // Any other key is captured so it doesn't bubble-up
            e.Handled = true;

            string keyStr = key.ToString().ToLower();
            if (keyStr.Contains("ctrl") || keyStr.Contains("shift") || keyStr.Contains("alt") || keyStr.Contains("win"))
            {
                key = Key.None;
            }

            //////string common =
            //////                ((Keyboard.Modifiers & ModifierKeys.Shift) > 0 ? "SHIFT " : "")
            //////                +
            //////                ((Keyboard.Modifiers & ModifierKeys.Control) > 0 ? "CTRL " : "")
            //////                +
            //////                ((Keyboard.Modifiers & ModifierKeys.Alt) > 0 ? "ALT " : "")
            //////                +
            //////                ((Keyboard.Modifiers & ModifierKeys.Windows) > 0 ? "WIN " : "")
            //////                ;

            //////if (string.IsNullOrEmpty(common)) common = "NONE ";

            //////KeyGestureSerializedEncoded = "[ " + common + "] " + (key != Key.None ? key.ToString() : "");

            KeyGestureSerializedEncoded = KeyGestureStringConverter.Convert(key, Keyboard.Modifiers);

            //if (Text != KeyGestureSerializedEncoded)
            //{
            //    Text = KeyGestureSerializedEncoded;
            //}
            Text = KeyGestureSerializedEncoded;

            //var converter = new KeyConverter();
            //string keyDisplayString = converter.ConvertToString(key);
            //ToolTip = common + (key != Key.None ?
            //    (!keyDisplayString.ToLower().Contains("oem") ?
            //    keyDisplayString :
            //    ("" + GetChar(KeyInterop.VirtualKeyFromKey(key))).ToUpper()) :
            //    "");

            var keyG = KeyGestureStringConverter.Convert(KeyGestureSerializedEncoded);

            string displayStr = keyG == null ? null : GetDisplayString(keyG);

            Console.WriteLine("\n" + @"=====> "
                + (keyG == null ? "INVALID" : displayStr)
                + @" <==> "
                + (keyG == null ? "INVALID" : KeyGestureStringConverter.Convert((KeyGesture)keyG)));
        }

        private void OnPreviewKeyUp_TextBox(object sender, KeyEventArgs e)
        {
            //if (e.Key != Key.Tab)
            //{
            //    e.Handled = true;
            //}
        }

        //public static readonly RoutedEvent EscapedEvent = EventManager.RegisterRoutedEvent(
        //    "Escaped",
        //    RoutingStrategy.Bubble,
        //    typeof(RoutedEventHandler),
        //    typeof(KeyGestureSinkBox));
        //public static void AddEscapedHandler(DependencyObject d, RoutedEventHandler handler)
        //{
        //    UIElement uie = d as UIElement;
        //    if (uie != null)
        //    {
        //        uie.AddHandler(EscapedEvent, handler);
        //    }
        //}
        //public static void RemoveEscapedHandler(DependencyObject d, RoutedEventHandler handler)
        //{
        //    UIElement uie = d as UIElement;
        //    if (uie != null)
        //    {
        //        uie.RemoveHandler(EscapedEvent, handler);
        //    }
        //}

        //public event EventHandler Escaped;

        private void OnKeyUp_TextBox(object sender, KeyEventArgs e)
        {
            Console.WriteLine(@"------------");

            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key != Key.Tab && key != Key.Escape && key != Key.Return)
            {
                // Any key other than the reserved ones is captured so it doesn't bubble-up
                e.Handled = true;
            }

            if (key == Key.Escape)
            {
                if (!string.IsNullOrEmpty(m_previousValidText))
                {
                    Text = m_previousValidText;
                }

                //RaiseEvent(new RoutedEventArgs {RoutedEvent = EscapedEvent});

                //if (target is UIElement)
                //{
                //    (target as UIElement).RaiseEvent(args);
                //}
                //else if (target is ContentElement)
                //{
                //    (target as ContentElement).RaiseEvent(args);
                //}


                //EventHandler eventHandler = Escaped;
                //if (eventHandler != null) eventHandler(this, EventArgs.Empty);
            }
        }

        //public bool Focused
        //{
        //    get { return (bool)GetValue(FocusedProperty); }
        //    set { SetValue(FocusedProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for Watermark.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty FocusedProperty = DependencyProperty.Register(
        //    "Focused",
        //    typeof(bool),
        //    typeof(KeyGestureSinkBox),
        //    new UIPropertyMetadata(false, OnFocusedChanged));

        //private static void OnFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if ((bool)e.NewValue)
        //    {
        //        FocusHelper.Focus(d, (UIElement) d);
        //    }
        //}

        private string m_previousValidText;

        private void OnLostFocus_TextBox(object sender, RoutedEventArgs e)
        {
            //Focused = false;

            if (System.Windows.Controls.Validation.GetHasError(this)
                && !string.IsNullOrEmpty(m_previousValidText))
            {
                Text = m_previousValidText;
            }
            FontWeight = FontWeights.Normal;
        }

        private void OnGotFocus_TextBox(object sender, RoutedEventArgs e)
        {
            m_previousValidText = (System.Windows.Controls.Validation.GetHasError(this) ? null : Text);
            FontWeight = FontWeights.UltraBold;
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
