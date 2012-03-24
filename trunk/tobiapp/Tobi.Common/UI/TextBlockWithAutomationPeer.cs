using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using AudioLib;

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

        private bool m_AutomaticChangeNotificationForAutomationPropertiesName = false;
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(
                AutomationProperties.NameProperty,
                typeof(TextBlockWithAutomationPeer));

            if (dpd != null)
            {
                m_AutomaticChangeNotificationForAutomationPropertiesName = true;
                dpd.AddValueChanged(this, delegate
                {
                    TextBlockWithAutomationPeer.NotifyScreenReaderAutomationIfKeyboardFocused(m_AutomationPeer, this);
                });
            }

            return m_AutomationPeer;
        }

        private static void NotifyScreenReaderAutomation(AutomationPeer automationPeer, string str)
        {
            if (automationPeer == null)
            {
                return;
            }

            if (!AutomationInteropProvider.ClientsAreListening)
            {
                return;
            }

            //            if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            //            {
            //                automationPeer.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);

            //#if DEBUG
            //                Console.WriteLine("AUTOMATION EVENT ==> AutomationFocusChanged");
            //#endif //DEBUG
            //            }

            //            if (AutomationPeer.ListenerExists(AutomationEvents.TextPatternOnTextSelectionChanged))
            //            {
            //                automationPeer.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged);

            //#if DEBUG
            //                Console.WriteLine("AUTOMATION EVENT ==> TextPatternOnTextSelectionChanged");
            //#endif //DEBUG
            //            }




            //automationPeer.InvalidatePeer();

            //AutomationEventArgs args = new AutomationEventArgs(InvokePatternIdentifiers.InvokedEvent);
            //AutomationInteropProvider.RaiseAutomationEvent(InvokePatternIdentifiers.InvokedEvent, this, args);

            //AutomationProperties.NameProperty

            try
            {
#if DEBUG
                var autoProp = AutomationProperty.LookupById(AutomationElementIdentifiers.NameProperty.Id);
                DebugFix.Assert(AutomationElementIdentifiers.NameProperty == autoProp);
#endif //DEBUG
                automationPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, "", str);
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine("Exception automationPeer.RaisePropertyChangedEvent");
                Debugger.Break();
#endif //DEBUG
            }
        }

        public static void NotifyScreenReaderAutomationIfKeyboardFocused(AutomationPeer automationPeer, UIElement uiElement)
        {
            if (uiElement.IsKeyboardFocused)
            {
                var str = uiElement.GetValue(AutomationProperties.NameProperty) as String;
#if DEBUG
                Console.WriteLine("AUTOMATION NAME ==> " + str);

                var str2 = AutomationProperties.GetName(uiElement);
                DebugFix.Assert(str2 == str);

                if (automationPeer != null)
                {
                    string str3 = automationPeer.GetName();
                    DebugFix.Assert(str3 == str);
                }
#endif //DEBUG
                TextBlockWithAutomationPeer.NotifyScreenReaderAutomation(automationPeer, str);
            }
        }

        public static void SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(AutomationPeer automationPeer, UIElement uiElement, string str, bool skipNotify)
        {
            uiElement.SetValue(AutomationProperties.NameProperty, str);

            if (!skipNotify)
            {
                //#if DEBUG
                //                Debugger.Break();
                //#endif //DEBUG
                TextBlockWithAutomationPeer.NotifyScreenReaderAutomationIfKeyboardFocused(automationPeer, uiElement);
            }
        }



        //public void NotifyScreenReaderAutomationIfKeyboardFocused()
        //{
        //    TextBlockWithAutomationPeer.NotifyScreenReaderAutomationIfKeyboardFocused(m_AutomationPeer, this);
        //}

        public void SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(string str)
        {
            TextBlockWithAutomationPeer.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(m_AutomationPeer, this, str, m_AutomaticChangeNotificationForAutomationPropertiesName);
        }

        //public static readonly DependencyProperty AutomationPropertiesNameProperty =
        //    DependencyProperty.Register(@"AutomationPropertiesName",
        //    typeof(string),
        //    typeof(TextBlockWithAutomationPeer),
        //    new PropertyMetadata("empty accessible name",
        //        OnAutomationPropertiesNameChanged, OnAutomationPropertiesNameCoerce));

        //public string AutomationPropertiesName
        //{
        //    get
        //    {
        //        return (string)GetValue(AutomationPropertiesNameProperty);
        //    }
        //    set
        //    {
        //        // The value will be coerced after this call !
        //        SetValue(AutomationPropertiesNameProperty, value);
        //    }
        //}

        //private static object OnAutomationPropertiesNameCoerce(DependencyObject d, object basevalue)
        //{
        //    return basevalue;
        //}

        //private static void OnAutomationPropertiesNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var tb = d as TextBlockWithAutomationPeer;
        //    if (tb == null) return;

        //    tb.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused((string)e.NewValue);
        //}
    }
}
