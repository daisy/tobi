using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Tobi.Common.UI
{
    // no need for this class in .NET 4 with IsReadOnlyCaretVisible...
    public class TextBoxReadOnlyCaretVisible : TextBox
    {
        public struct TextBoxSelection
        {
            public int start;
            public int length;
        }

        public AutomationPeer m_AutomationPeer;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();
            return m_AutomationPeer;
        }

        public TextBoxReadOnlyCaretVisible(string txt) : this()
        {
            Text = txt;

            var tbSel = new TextBoxSelection();

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

        protected TextBoxReadOnlyCaretVisible()
        {
            AcceptsTab = false;
            AcceptsReturn = true;
            IsReadOnly = false;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            TextWrapping = TextWrapping.Wrap;
            
            SetResourceReference(Control.BackgroundProperty, SystemColors.ControlLightLightBrushKey);
            SetResourceReference(Control.BorderBrushProperty, SystemColors.ControlDarkDarkBrush);
            //Background = SystemColors.ControlLightLightBrush;
            //BorderBrush = SystemColors.ControlDarkDarkBrush;

            BorderThickness = new Thickness(1);
            Padding = new Thickness(6);
            SnapsToDevicePixels = true;
        }
    }
}
