using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
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

        protected string KeyGestureSerializedEncoded { get; set; }

        public KeyGestureString KeyGesture
        {
            get
            {
                if (KeyGestureSerializedEncoded == null)
                {
                    KeyGestureSerializedEncoded = Text;
                }
                return KeyGestureStringConverter.Convert(KeyGestureSerializedEncoded);
            }
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

            KeyGestureSerializedEncoded = KeyGestureStringConverter.Convert(key, Keyboard.Modifiers, true);

            //if (Text != KeyGestureSerializedEncoded)
            //{
            //    Text = KeyGestureSerializedEncoded;
            //}
            Text = KeyGestureSerializedEncoded;
            updateTooltip();

            //var converter = new KeyConverter();
            //string keyDisplayString = converter.ConvertToString(key);
            //ToolTip = common + (key != Key.None ?
            //    (!keyDisplayString.ToLower().Contains("oem") ?
            //    keyDisplayString :
            //    ("" + GetChar(KeyInterop.VirtualKeyFromKey(key))).ToUpper()) :
            //    "");

            var keyG = KeyGesture;

            string displayStr = keyG == null ? null : keyG.GetDisplayString();

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
                restorePreviousValidText();

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

        private void restorePreviousValidText()
        {
            if (!string.IsNullOrEmpty(m_previousValidText))
            {
                Text = m_previousValidText;
                KeyGestureSerializedEncoded = Text;

                updateTooltip();
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

        public AutomationPeer m_AutomationPeer;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();
            return m_AutomationPeer;
        }

        private void updateTooltip()
        {
            var keyG = KeyGesture;

            string displayStr = keyG == null ? Text : KeyGestureStringConverter.Convert(keyG.Key, keyG.Modifiers, true); //keyG.GetDisplayString();

            ToolTip = displayStr;

            SetValue(AutomationProperties.NameProperty, displayStr);
        }

        private void notifyScreenReader()
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            {
                m_AutomationPeer.RaiseAutomationEvent(
                    AutomationEvents.AutomationFocusChanged);
            }
        }

        private string m_previousValidText;

        private void OnLostFocus_TextBox(object sender, RoutedEventArgs e)
        {
            //Focused = false;

            if (System.Windows.Controls.Validation.GetHasError(this))
            {
                restorePreviousValidText();
            }
            FontWeight = FontWeights.Normal;
        }

        private void OnGotFocus_TextBox(object sender, RoutedEventArgs e)
        {
            m_previousValidText = (System.Windows.Controls.Validation.GetHasError(this) ? null : Text);
            FontWeight = FontWeights.UltraBold;

            notifyScreenReader();
        }

        private void OnLoaded_TextBox(object sender, RoutedEventArgs e)
        {
            updateTooltip();
        }
    }
}
