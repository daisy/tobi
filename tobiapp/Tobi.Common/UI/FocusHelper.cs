using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tobi.Common.UI
{
    public class TextBoxEx : TextBox
    {
        public AutomationPeer m_AutomationPeer;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();
            return m_AutomationPeer;
        }

        public TextBoxEx(string txt)
        {
            Text = txt;

            AcceptsReturn = true;
            IsReadOnly = false;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            TextWrapping = TextWrapping.Wrap;
            Background = SystemColors.ControlLightLightBrush;
            BorderBrush = SystemColors.ControlDarkDarkBrush;
            BorderThickness = new Thickness(1);
            Padding = new Thickness(6);
            SnapsToDevicePixels = true;

            var tbSel = new FocusHelper.TextBoxSelection();

            SelectionChanged += ((sender, e) =>
            {
                tbSel.start = SelectionStart;
                tbSel.length = SelectionLength;

                SetValue(AutomationProperties.NameProperty, SelectedText);

                if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
                {
                    m_AutomationPeer.RaiseAutomationEvent(
                        AutomationEvents.AutomationFocusChanged);
                }
            });

            TextChanged += ((sender, e) =>
            {
                int start = tbSel.start;
                int length = tbSel.length;
                Text = txt;
                Select(start, length);
            });
        }
    }

    public static class FocusHelper
    {
        public static void Focus(DependencyObject obj, UIElement ui)
        {
            ui.Focus();
            FocusManager.SetFocusedElement(obj, ui);
            Keyboard.Focus(ui);
        }

        public struct TextBoxSelection
        {
            public int start;
            public int length;
        }
    }
}
