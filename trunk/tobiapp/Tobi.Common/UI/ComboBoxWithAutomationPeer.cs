using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Tobi.Common.UI
{
    public class ComboBoxWithAutomationPeer : ComboBox
    {
        public AutomationPeer m_AutomationPeer;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();
            return m_AutomationPeer;
        }

        private void NotifyScreenReaderAutomation()
        {
            if (m_AutomationPeer != null
                && AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            {
                m_AutomationPeer.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
            }
        }

        public void SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(string str)
        {
            SetValue(AutomationProperties.NameProperty, str);
            NotifyScreenReaderAutomationIfKeyboardFocused();
        }

        public void NotifyScreenReaderAutomationIfKeyboardFocused()
        {
            if (IsKeyboardFocused)
            {
                NotifyScreenReaderAutomation();
            }
        }

        public static readonly DependencyProperty AutomationPropertiesNameProperty =
            DependencyProperty.Register(@"AutomationPropertiesName",
            typeof(string),
            typeof(ComboBoxWithAutomationPeer),
            new PropertyMetadata("empty accessible name",
                OnAutomationPropertiesNameChanged, OnAutomationPropertiesNameCoerce));

        public string AutomationPropertiesName
        {
            get
            {
                return (string)GetValue(AutomationPropertiesNameProperty);
            }
            set
            {
                // The value will be coerced after this call !
                SetValue(AutomationPropertiesNameProperty, value);
            }
        }

        private static object OnAutomationPropertiesNameCoerce(DependencyObject d, object basevalue)
        {
            return basevalue;
        }

        private static void OnAutomationPropertiesNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = d as ComboBoxWithAutomationPeer;
            if (tb == null) return;

            tb.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused((string)e.NewValue);
        }
    }
}
