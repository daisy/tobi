using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;


namespace Tobi.Common.UI
{
    public static class AdornerExtensions
    {
        public static void TryRemoveAdorners<T>(this UIElement elem)
            where T : Adorner
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(elem);
            if (adornerLayer != null)
            {
                adornerLayer.RemoveAdorners<T>(elem);
            }
        }

        public static void RemoveAdorners<T>(this AdornerLayer adr, UIElement elem)
            where T : Adorner
        {
            Adorner[] adorners = adr.GetAdorners(elem);

            if (adorners == null) return;

            for (int i = adorners.Length - 1; i >= 0; i--)
            {
                if (adorners[i] is T)
                    adr.Remove(adorners[i]);
            }
        }

        public static void TryAddAdorner<T>(this UIElement elem, Adorner adorner)
            where T : Adorner
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(elem);
            if (adornerLayer != null && !adornerLayer.ContainsAdorner<T>(elem))
            {
                adornerLayer.Add(adorner);
            }
        }

        public static bool ContainsAdorner<T>(this AdornerLayer adr, UIElement elem)
            where T : Adorner
        {
            Adorner[] adorners = adr.GetAdorners(elem);

            if (adorners == null) return false;

            for (int i = adorners.Length - 1; i >= 0; i--)
            {
                if (adorners[i] is T)
                    return true;
            }
            return false;
        }

        public static void RemoveAllAdorners(this AdornerLayer adr, UIElement elem)
        {
            Adorner[] adorners = adr.GetAdorners(elem);

            if (adorners == null) return;

            foreach (Adorner toRemove in adorners)
                adr.Remove(toRemove);
        }
    }

    public class TextBlockAdorner : Adorner
    {
        private readonly TextBlock m_TextBlock;

        public TextBlockAdorner(UIElement adornedElement, string label, Style labelStyle)
            : base(adornedElement)
        {
            m_TextBlock = new TextBlock { Style = labelStyle, Text = label };
        }

        protected override Size MeasureOverride(Size constraint)
        {
            m_TextBlock.Measure(constraint);
            return constraint;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            m_TextBlock.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return m_TextBlock;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }
    }

    //#define HANDLE_MEM_LEAK

    public sealed class WatermarkComboBoxBehavior
    {
        private readonly ComboBox m_ComboBox;
        private TextBlockAdorner m_TextBlockAdorner;

        private WatermarkComboBoxBehavior(ComboBox comboBox)
        {
            if (comboBox == null)
                throw new ArgumentNullException("comboBox");

            m_ComboBox = comboBox;
        }

        #region Behavior Internals

        private static WatermarkComboBoxBehavior GetWatermarkComboBoxBehavior(DependencyObject obj)
        {
            return (WatermarkComboBoxBehavior)obj.GetValue(WatermarkComboBoxBehaviorProperty);
        }

        private static void SetWatermarkComboBoxBehavior(DependencyObject obj, WatermarkComboBoxBehavior value)
        {
            obj.SetValue(WatermarkComboBoxBehaviorProperty, value);
        }

        private static readonly DependencyProperty WatermarkComboBoxBehaviorProperty =
            DependencyProperty.RegisterAttached("WatermarkComboBoxBehavior",
                typeof(WatermarkComboBoxBehavior), typeof(WatermarkComboBoxBehavior), new UIPropertyMetadata(null));

        public static bool GetEnableWatermark(ComboBox obj)
        {
            return (bool)obj.GetValue(EnableWatermarkProperty);
        }

        public static void SetEnableWatermark(ComboBox obj, bool value)
        {
            obj.SetValue(EnableWatermarkProperty, value);
        }

        public static readonly DependencyProperty EnableWatermarkProperty =
            DependencyProperty.RegisterAttached("EnableWatermark", typeof(bool),
                typeof(WatermarkComboBoxBehavior), new UIPropertyMetadata(false, OnEnableWatermarkChanged));

        private static void OnEnableWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                var enabled = (bool)e.OldValue;

                if (enabled)
                {
                    var comboBox = (ComboBox)d;
                    var behavior = GetWatermarkComboBoxBehavior(comboBox);
                    behavior.Detach();

                    SetWatermarkComboBoxBehavior(comboBox, null);
                }
            }

            if (e.NewValue != null)
            {
                var enabled = (bool)e.NewValue;

                if (enabled)
                {
                    var comboBox = (ComboBox)d;
                    var behavior = new WatermarkComboBoxBehavior(comboBox);
                    behavior.Attach();

                    SetWatermarkComboBoxBehavior(comboBox, behavior);
                }
            }
        }

        private void Attach()
        {
            m_ComboBox.Loaded += ComboBoxLoaded;

            m_ComboBox.IsVisibleChanged += ComboBoxVisibleChanged;

            m_ComboBox.GotFocus += ComboBoxGotFocus;
            m_ComboBox.LostFocus += ComboBoxLostFocus;

            m_ComboBox.SelectionChanged += ComboBoxSelectionChanged;

            //m_ComboBox.DragEnter += ComboBoxDragEnter;
            //m_ComboBox.DragLeave += ComboBoxDragLeave;
        }

        private void Detach()
        {
            m_ComboBox.Loaded -= ComboBoxLoaded;

            m_ComboBox.IsVisibleChanged -= ComboBoxVisibleChanged;

            m_ComboBox.GotFocus -= ComboBoxGotFocus;
            m_ComboBox.LostFocus -= ComboBoxLostFocus;

            m_ComboBox.SelectionChanged -= ComboBoxSelectionChanged;

            var textBox = GetTextBox();
            if (textBox != null)
                textBox.TextChanged -= ComboBoxTextChanged;

            //m_ComboBox.DragEnter -= ComboBoxDragEnter;
            //m_ComboBox.DragLeave -= ComboBoxDragLeave;
        }

        private void ComboBoxSelectionChanged(object senderz, RoutedEventArgs e)
        {
            UpdateAdorner();
        }

        private void ComboBoxVisibleChanged(object senderz, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            UpdateAdorner();
        }

        public TextBox GetTextBox()
        {
            return ComboBoxWithAutomationPeer.GetTextBox(m_ComboBox);
        }

        //private void ComboBoxDragLeave(object sender, DragEventArgs e)
        //{
        //    UpdateAdorner();
        //}

        //private void ComboBoxDragEnter(object sender, DragEventArgs e)
        //{
        //    m_ComboBox.TryRemoveAdorners<TextBlockAdorner>();
        //}

        private void ComboBoxTextChanged(object senderz, RoutedEventArgs e)
        {
            UpdateAdorner();
        }

        private void ComboBoxGotFocus(object senderz, RoutedEventArgs e)
        {
            UpdateAdorner();
        }

        private void ComboBoxLostFocus(object senderz, RoutedEventArgs e)
        {
            UpdateAdorner();
        }

        private void ComboBoxLoaded(object senderz, RoutedEventArgs e)
        {
            var textBox = GetTextBox();
            if (textBox != null)
                textBox.TextChanged += ComboBoxTextChanged;

            m_TextBlockAdorner = new TextBlockAdorner(m_ComboBox, GetLabel(m_ComboBox), GetLabelStyle(m_ComboBox));
            UpdateAdorner();

            //            DependencyPropertyDescriptor focusProp = DependencyPropertyDescriptor.FromProperty(UIElement.IsFocusedProperty, typeof(ComboBox));
            //            if (focusProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                focusProp.AddValueChanged(m_ComboBox, OnValueChanged_IsFocusedProperty);
            //                m_ComboBox.SetValue(IsFocusedPropertyDescriptor, focusProp);
            //#else
            //                focusProp.AddValueChanged(m_ComboBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }

            //            DependencyPropertyDescriptor focusKeyboardProp = DependencyPropertyDescriptor.FromProperty(UIElement.IsKeyboardFocusedProperty, typeof(ComboBox));
            //            if (focusKeyboardProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                focusKeyboardProp.AddValueChanged(m_ComboBox, OnValueChanged_IsKeyboardFocusedProperty);
            //                m_ComboBox.SetValue(IsKeyboardFocusedPropertyDescriptor, focusKeyboardProp);
            //#else
            //                focusKeyboardProp.AddValueChanged(m_ComboBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }

            //            DependencyPropertyDescriptor focusKeyboardWithinProp = DependencyPropertyDescriptor.FromProperty(UIElement.IsKeyboardFocusWithinProperty, typeof(ComboBox));
            //            if (focusKeyboardWithinProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                focusKeyboardWithinProp.AddValueChanged(m_ComboBox, OnValueChanged_IsKeyboardFocusWithinProperty);
            //                m_ComboBox.SetValue(IsKeyboardFocusWithinPropertyDescriptor, focusKeyboardWithinProp);
            //#else
            //                focusKeyboardWithinProp.AddValueChanged(m_ComboBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }

            //            DependencyPropertyDescriptor textProp = DependencyPropertyDescriptor.FromProperty(ComboBox.TextProperty, typeof(ComboBox));
            //            if (textProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                textProp.AddValueChanged(m_ComboBox, OnValueChanged_TextProperty);
            //                m_ComboBox.SetValue(TextPropertyDescriptor, textProp);
            //#else
            //                textProp.AddValueChanged(m_ComboBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }

            //            DependencyPropertyDescriptor selectedIndexProp = DependencyPropertyDescriptor.FromProperty(Selector.SelectedIndexProperty, typeof(ComboBox));
            //            if (selectedIndexProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                selectedIndexProp.AddValueChanged(m_ComboBox, OnValueChanged_SelectedIndexProperty);
            //                m_ComboBox.SetValue(SelectedIndexPropertyDescriptor, selectedIndexProp);
            //#else
            //                selectedIndexProp.AddValueChanged(m_ComboBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }

            //            DependencyPropertyDescriptor selectedItemProp = DependencyPropertyDescriptor.FromProperty(Selector.SelectedItemProperty, typeof(ComboBox));
            //            if (selectedItemProp != null)
            //            {

            //#if HANDLE_MEM_LEAK
            //                selectedItemProp.AddValueChanged(m_ComboBox, OnValueChanged_SelectedItemProperty);
            //                m_ComboBox.SetValue(SelectedItemPropertyDescriptor, selectedItemProp);
            //#else
            //                selectedItemProp.AddValueChanged(m_ComboBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }
        }

        #endregion

        #region Attached Properties

        #region Label

        public static string GetLabel(ComboBox obj)
        {
            return (string)obj.GetValue(LabelProperty);
        }

        public static void SetLabel(ComboBox obj, string value)
        {
            obj.SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.RegisterAttached("Label", typeof(string), typeof(WatermarkComboBoxBehavior));

        #endregion

        #region LabelStyle

        public static Style GetLabelStyle(ComboBox obj)
        {
            return (Style)obj.GetValue(LabelStyleProperty);
        }

        public static void SetLabelStyle(ComboBox obj, Style value)
        {
            obj.SetValue(LabelStyleProperty, value);
        }

        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.RegisterAttached("LabelStyle", typeof(Style),
                                                typeof(WatermarkComboBoxBehavior));

        #endregion

        #endregion


#if HANDLE_MEM_LEAK
        
        public static bool OnValueChanged(object sender, EventArgs args, DependencyProperty dependencyProperty, DependencyProperty dependencyPropertyDescriptor, EventHandler evHandler)
        {
            var source = (DependencyObject)sender;
            if (!BindingOperations.IsDataBound(source, dependencyProperty))
            {
                // remove the ValueChanged event listener to avoid a memory leak 
                // http://sharpfellows.com/post/Memory-Leaks-and-Dependency-Properties.aspx
                var dpd = (DependencyPropertyDescriptor)source.GetValue(dependencyPropertyDescriptor);
                if (dpd != null)
                {
                    dpd.RemoveValueChanged(source, evHandler);
                    source.SetValue(dependencyPropertyDescriptor, null);
                }
                return false;
            }

            return true;
        }


        private static readonly DependencyProperty SelectedItemPropertyDescriptor =
        DependencyProperty.RegisterAttached("SelectedItemPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(ComboBox));
        private void OnValueChanged_SelectedItemProperty(object sender, EventArgs args)
        {
            if (OnValueChanged(sender, args, Selector.SelectedItemProperty, SelectedItemPropertyDescriptor, OnValueChanged_SelectedItemProperty))
            {
                UpdateAdorner();
            }
        }

        //

        private static readonly DependencyProperty SelectedIndexPropertyDescriptor =
        DependencyProperty.RegisterAttached("SelectedIndexPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(ComboBox));
        private void OnValueChanged_SelectedIndexProperty(object sender, EventArgs args)
        {
            if (OnValueChanged(sender, args, Selector.SelectedIndexProperty, SelectedIndexPropertyDescriptor, OnValueChanged_SelectedIndexProperty))
            {
                UpdateAdorner();
            }
        }

        //

        private static readonly DependencyProperty TextPropertyDescriptor =
        DependencyProperty.RegisterAttached("TextPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(ComboBox));
        private void OnValueChanged_TextProperty(object sender, EventArgs args)
        {
            if (OnValueChanged(sender, args, ComboBox.TextProperty, TextPropertyDescriptor, OnValueChanged_TextProperty))
            {
                UpdateAdorner();
            }
        }

        //

        private static readonly DependencyProperty IsKeyboardFocusWithinPropertyDescriptor =
        DependencyProperty.RegisterAttached("IsKeyboardFocusWithinPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(ComboBox));
        private void OnValueChanged_IsKeyboardFocusWithinProperty(object sender, EventArgs args)
        {
            if (OnValueChanged(sender, args, UIElement.IsKeyboardFocusWithinProperty, IsKeyboardFocusWithinPropertyDescriptor, OnValueChanged_IsKeyboardFocusWithinProperty))
            {
                UpdateAdorner();
            }
        }

        //

        private static readonly DependencyProperty IsKeyboardFocusedPropertyDescriptor =
        DependencyProperty.RegisterAttached("IsKeyboardFocusedPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(ComboBox));
        private void OnValueChanged_IsKeyboardFocusedProperty(object sender, EventArgs args)
        {
            if (OnValueChanged(sender, args, UIElement.IsKeyboardFocusedProperty, IsKeyboardFocusedPropertyDescriptor, OnValueChanged_IsKeyboardFocusedProperty))
            {
                UpdateAdorner();
            }
        }


        //

        private static readonly DependencyProperty IsFocusedPropertyDescriptor =
        DependencyProperty.RegisterAttached("IsFocusedPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(ComboBox));
        private void OnValueChanged_IsFocusedProperty(object sender, EventArgs args)
        {
            if (OnValueChanged(sender, args, UIElement.IsFocusedProperty, IsFocusedPropertyDescriptor, OnValueChanged_IsFocusedProperty))
            {
                UpdateAdorner();
            }
        }

#endif //HANDLE_MEM_LEAK


        private void UpdateAdorner()
        {
            if (
                m_TextBlockAdorner == null
                || !m_ComboBox.IsVisible
                || !string.IsNullOrEmpty(m_ComboBox.Text) ||
                m_ComboBox.IsFocused ||
                m_ComboBox.IsKeyboardFocused ||
                m_ComboBox.IsKeyboardFocusWithin ||
                m_ComboBox.SelectedIndex != -1 ||
                m_ComboBox.SelectedItem != null)
            {
                // Hide the Watermark Label if the adorner layer is visible
                m_ComboBox.ToolTip = GetLabel(m_ComboBox);
                m_ComboBox.TryRemoveAdorners<TextBlockAdorner>();
            }
            else
            {
                // Show the Watermark Label if the adorner layer is visible
                m_ComboBox.ToolTip = null;
                m_ComboBox.TryAddAdorner<TextBlockAdorner>(m_TextBlockAdorner);
            }
        }
    }

    public sealed class WatermarkTextBoxBehavior
    {
        private readonly TextBox m_TextBox;
        private TextBlockAdorner m_TextBlockAdorner;

        private WatermarkTextBoxBehavior(TextBox textBox)
        {
            if (textBox == null)
                throw new ArgumentNullException("textBox");

            m_TextBox = textBox;
        }

        #region Behavior Internals

        private static WatermarkTextBoxBehavior GetWatermarkTextBoxBehavior(DependencyObject obj)
        {
            return (WatermarkTextBoxBehavior)obj.GetValue(WatermarkTextBoxBehaviorProperty);
        }

        private static void SetWatermarkTextBoxBehavior(DependencyObject obj, WatermarkTextBoxBehavior value)
        {
            obj.SetValue(WatermarkTextBoxBehaviorProperty, value);
        }

        private static readonly DependencyProperty WatermarkTextBoxBehaviorProperty =
            DependencyProperty.RegisterAttached("WatermarkTextBoxBehavior",
                typeof(WatermarkTextBoxBehavior), typeof(WatermarkTextBoxBehavior), new UIPropertyMetadata(null));

        public static bool GetEnableWatermark(TextBox obj)
        {
            return (bool)obj.GetValue(EnableWatermarkProperty);
        }

        public static void SetEnableWatermark(TextBox obj, bool value)
        {
            obj.SetValue(EnableWatermarkProperty, value);
        }

        public static readonly DependencyProperty EnableWatermarkProperty =
            DependencyProperty.RegisterAttached("EnableWatermark", typeof(bool),
                typeof(WatermarkTextBoxBehavior), new UIPropertyMetadata(false, OnEnableWatermarkChanged));

        private static void OnEnableWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                var enabled = (bool)e.OldValue;

                if (enabled)
                {
                    var textBox = (TextBox)d;
                    var behavior = GetWatermarkTextBoxBehavior(textBox);
                    behavior.Detach();

                    SetWatermarkTextBoxBehavior(textBox, null);
                }
            }

            if (e.NewValue != null)
            {
                var enabled = (bool)e.NewValue;

                if (enabled)
                {
                    var textBox = (TextBox)d;
                    var behavior = new WatermarkTextBoxBehavior(textBox);
                    behavior.Attach();

                    SetWatermarkTextBoxBehavior(textBox, behavior);
                }
            }
        }

        private void Attach()
        {
            m_TextBox.Loaded += TextBoxLoaded;
            //m_TextBox.Unloaded += TextBoxUnLoaded;

            m_TextBox.IsVisibleChanged += TextBoxVisibleChanged;

            m_TextBox.GotFocus += TextBoxFocusedChanged;
            m_TextBox.LostFocus += TextBoxFocusedChanged;

            m_TextBox.TextChanged += TextBoxTextChanged;

            //m_TextBox.DragEnter += TextBoxDragEnter;
            //m_TextBox.DragLeave += TextBoxDragLeave;
            //m_TextBox.DragOver += TextBoxDragOver;
        }

        private void Detach()
        {
            m_TextBox.Loaded -= TextBoxLoaded;
            //m_TextBox.Unloaded -= TextBoxUnLoaded;

            m_TextBox.IsVisibleChanged -= TextBoxVisibleChanged;

            m_TextBox.GotFocus -= TextBoxFocusedChanged;
            m_TextBox.LostFocus -= TextBoxFocusedChanged;

            m_TextBox.TextChanged -= TextBoxTextChanged;

            //m_TextBox.DragEnter -= TextBoxDragEnter;
            //m_TextBox.DragLeave -= TextBoxDragLeave;
            //m_TextBox.DragOver -= TextBoxDragOver;
        }
        
        private void TextBoxVisibleChanged(object senderz, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            UpdateAdorner();
        }

        //private void TextBoxUnLoaded(object sender, RoutedEventArgs e)
        //{
        //    Detach();
        //}

        private void TextBoxLoaded(object sender, RoutedEventArgs e)
        {
            m_TextBlockAdorner = new TextBlockAdorner(m_TextBox, GetLabel(m_TextBox), GetLabelStyle(m_TextBox));
            UpdateAdorner();

            //            DependencyPropertyDescriptor focusProp = DependencyPropertyDescriptor.FromProperty(UIElement.IsFocusedProperty, typeof(TextBox));//FrameworkElement
            //            if (focusProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                focusProp.AddValueChanged(m_TextBox, OnValueChanged_IsFocusedProperty);
            //                m_TextBox.SetValue(IsFocusedPropertyDescriptor, focusProp);
            //#else
            //                focusProp.AddValueChanged(m_TextBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }

            //            DependencyPropertyDescriptor containsTextProp = DependencyPropertyDescriptor.FromProperty(HasTextProperty, typeof(TextBox));
            //            if (containsTextProp != null)
            //            {
            //#if HANDLE_MEM_LEAK
            //                containsTextProp.AddValueChanged(m_TextBox, OnValueChanged_HasTextProperty);
            //                m_TextBox.SetValue(HasTextPropertyDescriptor, containsTextProp);
            //#else
            //                containsTextProp.AddValueChanged(m_TextBox, (sender, args) => UpdateAdorner());
            //#endif //HANDLE_MEM_LEAK
            //            }
        }

        private void TextBoxFocusedChanged(object sender, RoutedEventArgs e)
        {
            UpdateAdorner();
        }

        //private void TextBoxDragLeave(object sender, DragEventArgs e)
        //{
        //    UpdateAdorner();
        //}

        //private void TextBoxDragEnter(object sender, DragEventArgs e)
        //{
        //    m_TextBox.TryRemoveAdorners<TextBlockAdorner>();
        //}

        //private void TextBoxDragOver(object sender, DragEventArgs e)
        //{
        //    m_TextBox.TryRemoveAdorners<TextBlockAdorner>();
        //}

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAdorner();
            //var hasText = !string.IsNullOrEmpty(m_TextBox.Text);
            //SetHasText(m_TextBox, hasText);
        }

        #endregion

        #region Attached Properties

        public static string GetLabel(TextBox obj)
        {
            return (string)obj.GetValue(LabelProperty);
        }

        public static void SetLabel(TextBox obj, string value)
        {
            obj.SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.RegisterAttached("Label", typeof(string), typeof(WatermarkTextBoxBehavior));

        public static Style GetLabelStyle(TextBox obj)
        {
            return (Style)obj.GetValue(LabelStyleProperty);
        }

        public static void SetLabelStyle(TextBox obj, Style value)
        {
            obj.SetValue(LabelStyleProperty, value);
        }

        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.RegisterAttached("LabelStyle", typeof(Style),
                typeof(WatermarkTextBoxBehavior));

        //public static bool GetHasText(TextBox obj)
        //{
        //    return (bool)obj.GetValue(HasTextProperty);
        //}

        //private static void SetHasText(TextBox obj, bool value)
        //{
        //    obj.SetValue(HasTextPropertyKey, value);
        //}

        //private static readonly DependencyPropertyKey HasTextPropertyKey =
        //    DependencyProperty.RegisterAttachedReadOnly("HasText", typeof(bool),
        //        typeof(WatermarkTextBoxBehavior), new UIPropertyMetadata(false));

        //public static readonly DependencyProperty HasTextProperty =
        //    HasTextPropertyKey.DependencyProperty;

        #endregion


#if HANDLE_MEM_LEAK

        private static readonly DependencyProperty IsFocusedPropertyDescriptor =
        DependencyProperty.RegisterAttached("IsFocusedPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(TextBox));
        private void OnValueChanged_IsFocusedProperty(object sender, EventArgs args)
        {
            if (WatermarkComboBoxBehavior.OnValueChanged(sender, args, UIElement.IsFocusedProperty, IsFocusedPropertyDescriptor, OnValueChanged_IsFocusedProperty))
            {
                UpdateAdorner();
            }
        }

        //

        private static readonly DependencyProperty HasTextPropertyDescriptor =
        DependencyProperty.RegisterAttached("HasTextPropertyDescriptor",
                                            typeof(DependencyPropertyDescriptor),
                                            typeof(TextBox));
        private void OnValueChanged_HasTextProperty(object sender, EventArgs args)
        {
            if (WatermarkComboBoxBehavior.OnValueChanged(sender, args, HasTextProperty, HasTextPropertyDescriptor, OnValueChanged_HasTextProperty))
            {
                UpdateAdorner();
            }
        }

#endif //HANDLE_MEM_LEAK

        private void UpdateAdorner()
        {
            if (m_TextBlockAdorner == null
                || !m_TextBox.IsVisible
                || !string.IsNullOrEmpty(m_TextBox.Text)
                //GetHasText(m_TextBox)
                || m_TextBox.IsFocused
                || m_TextBox.IsKeyboardFocused
                || m_TextBox.IsKeyboardFocusWithin)
            {
                // Hide the Watermark Label if the adorner layer is visible
                m_TextBox.ToolTip = GetLabel(m_TextBox);
                m_TextBox.TryRemoveAdorners<TextBlockAdorner>();
            }
            else
            {
                // Show the Watermark Label if the adorner layer is visible
                m_TextBox.ToolTip = null;
                m_TextBox.TryAddAdorner<TextBlockAdorner>(m_TextBlockAdorner);
            }
        }
    }
}
