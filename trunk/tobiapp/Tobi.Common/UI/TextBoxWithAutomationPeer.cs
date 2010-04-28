using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Tobi.Common.UI
{
    public class TextBoxWithAutomationPeer : TextBox
    {
        //public TextBoxWithAutomationPeer()
        //{
        //    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(TextBoxWithAutomationPeer));
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
                m_AutomationPeer.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged);
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

        //public string AutomationPropertiesName
        //{
        //    get
        //    {
        //        return (string)GetValue(AutomationProperties.NameProperty);
        //    }
        //    set
        //    {
        //        SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(value);
        //    }
        //}


        public static readonly DependencyProperty AutomationPropertiesNameProperty =
            DependencyProperty.Register(@"AutomationPropertiesName",
            typeof(string),
            typeof(TextBoxWithAutomationPeer),
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
            var tb = d as TextBoxWithAutomationPeer;
            if (tb == null) return;

            tb.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused((string)e.NewValue);
        }
    }
}
