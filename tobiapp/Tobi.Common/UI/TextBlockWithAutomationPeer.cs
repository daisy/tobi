using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Tobi.Common.UI
{
    public class TextBlockWithAutomationPeer : TextBlock
    {
        //public TextBlockWithAutomationPeer()
        //{
        //    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(TextBlockWithAutomationPeer));
        //    if (dpd != null)
        //    {
        //        dpd.AddValueChanged(this, delegate
        //        {
        //            SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(Text);
        //        });
        //    }
        //}

        public AutomationPeer m_AutomationPeer;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();
            return m_AutomationPeer;
        }

        private void NotifyScreenReaderAutomation()
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            {
                m_AutomationPeer.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
            }
        }

        public void SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(string str)
        {
            SetValue(AutomationProperties.NameProperty, str);

            if (IsKeyboardFocused)
            {
                NotifyScreenReaderAutomation();
            }
        }
    }
}
