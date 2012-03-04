using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tobi.Common.UI
{
    public class ComboBoxWithAutomationPeer : ComboBox
    {
        //static ComboBoxWithAutomationPeer()
        //{
        //    DefaultStyleKeyProperty.OverrideMetadata(typeof(ComboBoxWithAutomationPeer),
        //                                             new FrameworkPropertyMetadata(typeof(ComboBoxWithAutomationPeer)));
        //}

        //public override void OnApplyTemplate()
        //{
        //    base.OnApplyTemplate();

        //    var style = (Style)Resources["WatermarkTextBoxStyle"];
        //    var textBox = GetTextBox();
        //    textBox.Style = style;
        //}


        public TextBox GetTextBox()
        {
            var textBox = FindChild(this, "PART_EditableTextBox", typeof(TextBox)) as TextBox;
            return textBox;
        }


        public static DependencyObject FindChild(DependencyObject reference, string childName, Type childType)
        {
            DependencyObject foundChild = null;
            if (reference != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(reference, i);
                    // If the child is not of the request child type child
                    if (child.GetType() != childType)
                    {
                        // recursively drill down the tree
                        foundChild = FindChild(child, childName, childType);
                    }
                    else if (!String.IsNullOrEmpty(childName))
                    {
                        var frameworkElement = child as FrameworkElement;
                        // If the child's name is set for search
                        if (frameworkElement != null && frameworkElement.Name == childName)
                        {
                            // if the child's name is of the request name
                            foundChild = child;
                            break;
                        }
                    }
                    else
                    {
                        // child element found.
                        foundChild = child;
                        break;
                    }
                }
            }
            return foundChild;
        }











        public AutomationPeer m_AutomationPeer;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            m_AutomationPeer = base.OnCreateAutomationPeer();
            return m_AutomationPeer;
        }

        public void NotifyScreenReaderAutomation()
        {
            if (m_AutomationPeer != null
                && AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            {
                m_AutomationPeer.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
                if (this.IsEditable)
                {
                    m_AutomationPeer.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged);
                }
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
