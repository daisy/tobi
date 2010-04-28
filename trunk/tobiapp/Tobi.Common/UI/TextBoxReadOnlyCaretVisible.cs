using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Tobi.Common.UI
{
    // no need for this class in .NET 4 with IsReadOnlyCaretVisible...
    public class TextBoxReadOnlyCaretVisible : TextBoxWithAutomationPeer
    {
        public struct TextBoxSelection
        {
            public int start;
            public int length;
        }

        private string m_Text;

        //private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    if (((TextBox)sender).SelectionLength == 0)
        //        ((TextBox)sender).SelectAll();
        //}

        public TextBoxReadOnlyCaretVisible()
        {
            //EventManager.RegisterClassHandler(typeof(TextBox),
            //    UIElement.GotFocusEvent,
            //    new RoutedEventHandler(TextBox_GotFocus));

            AcceptsTab = false;
            AcceptsReturn = false;
            IsReadOnly = false;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            TextWrapping = TextWrapping.Wrap;

            //SetResourceReference(Control.BackgroundProperty, SystemColors.ControlLightLightBrushKey);
            SetResourceReference(Control.BorderBrushProperty, SystemColors.ControlDarkDarkBrush);

            //Background = SystemColors.ControlLightLightBrush;
            //BorderBrush = SystemColors.ControlDarkDarkBrush;

            SnapsToDevicePixels = true;
#if NET40
            ////TODO:
            //TextBox.CaretBrush
            //TextBox.SelectionBrush
#endif

            GotFocus += ((sender, e) =>
            {
                if (!string.IsNullOrEmpty(SelectedText))
                {
                    SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(SelectedText);
                }
            });

            var tbSel = new TextBoxSelection();

            SelectionChanged += ((sender, e) =>
            {
                tbSel.start = SelectionStart;
                tbSel.length = SelectionLength;

                if (!string.IsNullOrEmpty(SelectedText))
                {
                    SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(SelectedText);
                }
            });

            TextChanged += ((sender, e) =>
            {
                if (!string.IsNullOrEmpty(m_Text))
                {
                    int start = tbSel.start;
                    int length = tbSel.length;
                    Text = m_Text;
                    Select(start, length);
                }
            });
        }

        public static readonly DependencyProperty TextReadOnlyProperty =
            DependencyProperty.Register(@"TextReadOnly",
            typeof(string),
            typeof(TextBoxReadOnlyCaretVisible),
            new PropertyMetadata("empty text",
                OnTextReadOnlyChanged, OnTextReadOnlyCoerce));

        public string TextReadOnly
        {
            get
            {
                return (string)GetValue(TextReadOnlyProperty);
            }
            set
            {
                // The value will be coerced after this call !
                SetValue(TextReadOnlyProperty, value);
            }
        }

        private static object OnTextReadOnlyCoerce(DependencyObject d, object basevalue)
        {
            return basevalue;
        }

        private static void OnTextReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = d as TextBoxReadOnlyCaretVisible;
            if (tb == null) return;

            tb.SetTextReadOnly((string)e.NewValue);
        }

        private void SetTextReadOnly(string txt)
        {
            // The order is important ! (otherwise infinite loop due to TextChanged event)
            m_Text = null;
            Text = txt;
            m_Text = txt;

            SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(m_Text);
        }
    }
}
