
//-----------------------------------------------------------------------
// <copyright file="Placeholder.cs" company="Jeow Li Huan">
// Copyright (c) Jeow Li Huan. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Common._UnusedCode
{
    /// <summary>
    ///   Represents an adorner that adds placeholder text to a <see cref="T:System.Windows.Controls.TextBox"/>,
    ///   <see cref="T:System.Windows.Controls.RichTextBox"/> or <see cref="T:System.Windows.Controls.PasswordBox"/>.
    /// </summary>
    public class Placeholder : Adorner
    {
        #region Dependency Property
        /// <summary>
        ///   Identifies the <see cref="Placeholder" />.Text attached property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(Placeholder),
            new FrameworkPropertyMetadata(string.Empty, OnTextChanged));
 
        /// <summary>
        ///   Identifies the <see cref="Placeholder" />.HideOnFocus attached property.
        /// </summary>
        public static readonly DependencyProperty HideOnFocusProperty = DependencyProperty.RegisterAttached(
            "HideOnFocus",
            typeof(bool),
            typeof(Placeholder),
            new FrameworkPropertyMetadata(true, OnHideOnFocusChanged));
        #endregion
 
        /// <summary>
        ///   <see langword="true"/> when the placeholder text is visible, <see langword="false" /> otherwise.
        ///   Used to avoid calling <see cref="M:System.Windows.UIElement.InvalidateVisual"/> unnecessarily.
        /// </summary>
        private bool isPlaceholderVisible;
 
        #region Constructors
        /// <summary>
        ///   Initializes static members of the <see cref="Placeholder"/> class.
        /// </summary>
        static Placeholder()
        {
            IsHitTestVisibleProperty.OverrideMetadata(typeof(Placeholder), new FrameworkPropertyMetadata(false));
            ClipToBoundsProperty.OverrideMetadata(typeof(Placeholder), new FrameworkPropertyMetadata(true));
        }
 
        /// <summary>
        ///   Initializes a new instance of the <see cref="Placeholder"/> class.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element to bind the adorner to.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        public Placeholder(PasswordBox adornedElement)
            : this((Control)adornedElement)
        {
            if (!(adornedElement.IsFocused && (bool)adornedElement.GetValue(HideOnFocusProperty)))
                adornedElement.PasswordChanged += this.AdornedElement_ContentChanged;
        }
 
        /// <summary>
        ///   Initializes a new instance of the <see cref="Placeholder"/> class.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element to bind the adorner to.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        public Placeholder(TextBoxBase adornedElement)
            : this((Control)adornedElement)
        {
            if (!(adornedElement.IsFocused && (bool)adornedElement.GetValue(HideOnFocusProperty)))
                adornedElement.TextChanged += this.AdornedElement_ContentChanged;
        }
 
        /// <summary>
        ///   Initializes a new instance of the <see cref="Placeholder"/> class.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element to bind the adorner to.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        protected Placeholder(Control adornedElement)
            : base(adornedElement)
        {
            if ((bool)adornedElement.GetValue(HideOnFocusProperty))
            {
                adornedElement.GotFocus += this.AdornedElement_GotFocus;
                adornedElement.LostFocus += this.AdornedElement_LostFocus;
            }
        }
        #endregion
 
        #region Attached Property Getters and Setters
        /// <summary>
        ///   Gets the value of the <see cref="Placeholder" />.Text attached property for a specified element.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element from which the property value is read.
        /// </param>
        /// <returns>
        ///   The placeholder text property value for the element.
        /// </returns>
        /// <exception cref="T:ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        public static string GetText(Control adornedElement)
        {
            if (adornedElement == null)
                throw new ArgumentNullException("adornedElement");
            return (string)adornedElement.GetValue(TextProperty);
        }
 
        /// <summary>
        ///   Sets the value of the <see cref="Placeholder" />.Text attached property to a specified element.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element to which the attached property is written.
        /// </param>
        /// <param name="placeholderText">
        ///   The needed placeholder text value.
        /// </param>
        /// <exception cref="T:ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        /// <exception cref="T:InvalidOperationException">
        ///   Raised when adornedElement is not a <see cref="T:System.Windows.Controls.TextBox"/>,
        ///   <see cref="T:System.Windows.Controls.RichTextBox"/> or <see cref="T:System.Windows.Controls.PasswordBox"/>.
        /// </exception>
        public static void SetText(Control adornedElement, string placeholderText)
        {
            if (adornedElement == null)
                throw new ArgumentNullException("adornedElement");
            adornedElement.SetValue(TextProperty, placeholderText);
        }
 
        /// <summary>
        ///   Gets the value of the <see cref="Placeholder" />.HideOnFocus attached property for a specified element.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element from which the property value is read.
        /// </param>
        /// <returns>
        ///   A value indicating whether the control will be hidden when the element is in focus.
        /// </returns>
        /// <exception cref="T:ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        public static bool GetHideOnFocus(Control adornedElement)
        {
            if (adornedElement == null)
                throw new ArgumentNullException("adornedElement");
            return (bool)adornedElement.GetValue(HideOnFocusProperty);
        }
 
        /// <summary>
        ///   Sets the value of the <see cref="Placeholder" />.HideOnFocus attached property to a specified element.
        /// </summary>
        /// <param name="adornedElement">
        ///   The element to which the attached property is written.
        /// </param>
        /// <param name="hideOnFocus">
        ///   A value indicating whether to hide the placeholder text when the element is in focus.
        /// </param>
        /// <exception cref="T:ArgumentNullException">
        ///   Raised when adornedElement is null.
        /// </exception>
        /// <exception cref="T:InvalidOperationException">
        ///   Raised when adornedElement is not a <see cref="T:System.Windows.Controls.TextBox"/>,
        ///   <see cref="T:System.Windows.Controls.RichTextBox"/> or <see cref="T:System.Windows.Controls.PasswordBox"/>.
        /// </exception>
        public static void SetHideOnFocus(Control adornedElement, bool hideOnFocus)
        {
            if (adornedElement == null)
                throw new ArgumentNullException("adornedElement");
            adornedElement.SetValue(HideOnFocusProperty, hideOnFocus);
        }
        #endregion
 
        /// <summary>
        ///   Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="Placeholder"/> element.
        /// </summary>
        /// <param name="drawingContext">
        ///   The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw. This context is provided to the layout system.
        /// </param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Control adornedElement = this.AdornedElement as Control;
            string placeholderText;
            bool hideOnFocus = (bool)adornedElement.GetValue(HideOnFocusProperty);
 
            if (adornedElement == null ||
                (adornedElement.IsFocused && hideOnFocus) ||
                !this.IsElementEmpty() ||
                string.IsNullOrEmpty(placeholderText = (string)adornedElement.GetValue(TextProperty)))
 
                this.isPlaceholderVisible = false;
            else
            {
                this.isPlaceholderVisible = true;
                Size size = adornedElement.RenderSize;
                double maxHeight = size.Height - adornedElement.BorderThickness.Top - adornedElement.BorderThickness.Bottom - adornedElement.Padding.Top - adornedElement.Padding.Bottom;
                double maxWidth = size.Width - adornedElement.BorderThickness.Left - adornedElement.BorderThickness.Right - adornedElement.Padding.Left - adornedElement.Padding.Right - 4.0;
                if (maxHeight <= 0 || maxWidth <= 0)
                    return;
 
                TextAlignment computedTextAlignment = this.ComputedTextAlignment();
 
                // Foreground brush does not need to be dynamic. OnRender called when SystemColors changes.
                Brush foreground = SystemColors.GrayTextBrush.Clone();
 
                foreground.Opacity = adornedElement.Foreground.Opacity;
                Typeface typeface = new Typeface(adornedElement.FontFamily, FontStyles.Italic, adornedElement.FontWeight, adornedElement.FontStretch);
                FormattedText formattedText = new FormattedText(
                    placeholderText,
                    CultureInfo.CurrentCulture,
                    adornedElement.FlowDirection,
                    typeface,
                    adornedElement.FontSize,
                    foreground);
                formattedText.TextAlignment = computedTextAlignment;
                formattedText.MaxTextHeight = maxHeight;
                formattedText.MaxTextWidth = maxWidth;
 
                double left;
                double top = 0.0;
                if (adornedElement.FlowDirection == FlowDirection.RightToLeft)
                    left = adornedElement.BorderThickness.Right + adornedElement.Padding.Right + 2.0;
                else
                    left = adornedElement.BorderThickness.Left + adornedElement.Padding.Left + 2.0;
                switch (adornedElement.VerticalContentAlignment)
                {
                    case VerticalAlignment.Top:
                    case VerticalAlignment.Stretch:
                        top = adornedElement.BorderThickness.Top + adornedElement.Padding.Top;
                        break;
                    case VerticalAlignment.Bottom:
                        top = size.Height - adornedElement.BorderThickness.Bottom - adornedElement.Padding.Bottom - formattedText.Height;
                        break;
                    case VerticalAlignment.Center:
                        top = (size.Height + adornedElement.BorderThickness.Top - adornedElement.BorderThickness.Bottom + adornedElement.Padding.Top - adornedElement.Padding.Bottom - formattedText.Height) / 2.0;
                        break;
                }
 
                if (adornedElement.FlowDirection == FlowDirection.RightToLeft)
                {
                    // Somehow everything got drawn reflected. Add a transform to correct.
                    drawingContext.PushTransform(new ScaleTransform(-1.0, 1.0, RenderSize.Width / 2.0, 0.0));
                    drawingContext.DrawText(formattedText, new Point(left, top));
                    drawingContext.Pop();
                }
                else
                    drawingContext.DrawText(formattedText, new Point(left, top));
            }
        }
 
        /// <summary>
        ///   Adds a <see cref="Placeholder"/> to the adorner layer.
        /// </summary>
        /// <param name="adornedElement">
        ///   The adorned element.
        /// </param>
        private static void AddAdorner(Control adornedElement)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            if (adornerLayer == null)
                return;
            Adorner[] adorners = adornerLayer.GetAdorners(adornedElement);
            if (adorners != null)
                foreach (Adorner adorner in adorners)
                    if (adorner is Placeholder)
                    {
                        adorner.InvalidateVisual();
                        return;
                    }
 
            TextBox textBox = adornedElement as TextBox;
            if (textBox != null)
            {
                adornerLayer.Add(new Placeholder(textBox));
                return;
            }
 
            RichTextBox richTextBox = adornedElement as RichTextBox;
            if (richTextBox != null)
            {
                adornerLayer.Add(new Placeholder(richTextBox));
                return;
            }
 
            PasswordBox passwordBox = adornedElement as PasswordBox;
            if (passwordBox != null)
            {
                adornerLayer.Add(new Placeholder(passwordBox));
                return;
            }
 
            // TextBox is hidden in template. Search for it.
            TextBox templateTextBox = null;
            templateTextBox = FindTextBox(adornedElement);
            if (templateTextBox != null)
                Placeholder.SetText(templateTextBox, (string)Placeholder.GetText(adornedElement));
        }
 
        /// <summary>
        ///   Finds a <see cref="T:System.Windows.Controls.TextBox"/> in the visual tree of the adorned element using a breadth first search.
        /// </summary>
        /// <param name="adornedElement">The adorned element which visual tree is searched for a <see cref="T:System.Windows.Controls.TextBox"/>.</param>
        /// <returns>The <see cref="T:System.Windows.Controls.TextBox"/> if one is found, <see langword="null"/> if none exists.</returns>
        private static TextBox FindTextBox(Control adornedElement)
        {
            TextBox templateTextBox = null;
            Queue<DependencyObject> queue = new Queue<DependencyObject>();
            queue.Enqueue(adornedElement);
            while (queue.Count > 0)
            {
                DependencyObject element = queue.Dequeue();
                templateTextBox = element as TextBox;
                if (templateTextBox != null)
                    break;
 
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); ++i)
                    queue.Enqueue(VisualTreeHelper.GetChild(element, i));
            }
 
            return templateTextBox;
        }
 
        /// <summary>
        ///   Event handler for AdornedElement.Loaded.
        /// </summary>
        /// <param name="sender">
        ///   The AdornedElement where the event handler is attached.
        /// </param>
        /// <param name="e">
        ///   Provides data about the event.
        /// </param>
        private static void AdornedElement_Loaded(object sender, RoutedEventArgs e)
        {
            Control adornedElement = (Control)sender;
            adornedElement.Loaded -= AdornedElement_Loaded;
            AddAdorner(adornedElement);
        }
 
        /// <summary>
        ///   Invoked whenever <see cref="Placeholder" />.Text attached property is changed.
        /// </summary>
        /// <param name="sender">
        ///   The object where the event handler is attached.
        /// </param>
        /// <param name="e">
        ///   Provides data about the event.
        /// </param>
        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (((string)e.OldValue).Equals((string)e.NewValue, StringComparison.Ordinal))
                return;
 
            Control adornedElement = sender as Control;
            if (adornedElement == null)
                return;
 
            if (adornedElement.IsLoaded)
                AddAdorner(adornedElement);
            else
                adornedElement.Loaded += AdornedElement_Loaded;
        }
 
        /// <summary>
        ///   Invoked whenever <see cref="Placeholder" />.HideOnFocus attached property is changed.
        /// </summary>
        /// <param name="sender">
        ///   The object where the event handler is attached.
        /// </param>
        /// <param name="e">
        ///   Provides data about the event.
        /// </param>
        private static void OnHideOnFocusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            bool hideOnFocus = (bool)e.NewValue;
            if ((bool)e.OldValue == hideOnFocus)
                return;
 
            Control adornedElement = sender as Control;
 
            if (!(adornedElement is TextBox || adornedElement is PasswordBox || adornedElement is RichTextBox))
            {
                if (!adornedElement.IsLoaded)
                    adornedElement.Loaded += ChangeHideOnFocus;
                else
                {
                    TextBox templateTextBox = FindTextBox(adornedElement);
                    if (templateTextBox != null)
                        Placeholder.SetHideOnFocus(templateTextBox, (bool)e.NewValue);
                }
 
                return;
            }
 
            Placeholder placeholder = null;
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            if (adornerLayer == null)
                return;
 
            Adorner[] adorners = adornerLayer.GetAdorners(adornedElement);
            if (adorners != null)
                foreach (Adorner adorner in adorners)
                {
                    placeholder = adorner as Placeholder;
                    if (placeholder != null)
                        break;
                }
 
            if (placeholder == null)
                return;
 
            if (adornedElement.IsLoaded)
            {
                if (hideOnFocus)
                {
                    adornedElement.GotFocus += placeholder.AdornedElement_GotFocus;
                    adornedElement.LostFocus += placeholder.AdornedElement_LostFocus;
                    if (adornedElement.IsFocused && placeholder.isPlaceholderVisible)
                        placeholder.InvalidateVisual();
                }
                else
                {
                    adornedElement.GotFocus -= placeholder.AdornedElement_GotFocus;
                    adornedElement.LostFocus -= placeholder.AdornedElement_LostFocus;
                    placeholder.AdornedElement_LostFocus(adornedElement, new RoutedEventArgs(UIElement.LostFocusEvent, placeholder));
                }
            }
            else
                adornedElement.Loaded += AdornedElement_Loaded;
        }
 
        /// <summary>
        ///   Changes the HideOnFocus property of the text box in the visual tree of the sender.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">Provides data about the event.</param>
        private static void ChangeHideOnFocus(object sender, RoutedEventArgs e)
        {
            Control adornedElement = sender as Control;
            if (adornedElement == null)
                return;
 
            TextBox templateTextBox = FindTextBox(adornedElement);
            if (templateTextBox != null)
                Placeholder.SetHideOnFocus(templateTextBox, Placeholder.GetHideOnFocus(adornedElement));
        }
 
        #region Event Handlers
        /// <summary>
        ///   Event handler for AdornedElement.GotFocus.
        /// </summary>
        /// <param name="sender">
        ///   The AdornedElement where the event handler is attached.
        /// </param>
        /// <param name="e">
        ///   Provides data about the event.
        /// </param>
        private void AdornedElement_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxBase textBoxBase = AdornedElement as TextBoxBase;
            if (textBoxBase != null)
                textBoxBase.TextChanged -= this.AdornedElement_ContentChanged;
            else
            {
                PasswordBox passwordBox = AdornedElement as PasswordBox;
                if (passwordBox != null)
                    passwordBox.PasswordChanged -= this.AdornedElement_ContentChanged;
            }
 
            if (this.isPlaceholderVisible)
                this.InvalidateVisual();
        }
 
        /// <summary>
        ///   Event handler for AdornedElement.LostFocus.
        /// </summary>
        /// <param name="sender">
        ///   The AdornedElement where the event handler is attached.
        /// </param>
        /// <param name="e">
        ///   Provides data about the event.
        /// </param>
        private void AdornedElement_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxBase textBoxBase = AdornedElement as TextBoxBase;
            if (textBoxBase != null)
                textBoxBase.TextChanged += this.AdornedElement_ContentChanged;
            else
            {
                PasswordBox passwordBox = AdornedElement as PasswordBox;
                if (passwordBox != null)
                    passwordBox.PasswordChanged += this.AdornedElement_ContentChanged;
            }
 
            if (!this.isPlaceholderVisible && this.IsElementEmpty())
                this.InvalidateVisual();
        }
 
        /// <summary>
        ///   Event handler for AdornedElement.ContentChanged.
        /// </summary>
        /// <param name="sender">
        ///   The AdornedElement where the event handler is attached.
        /// </param>
        /// <param name="e">
        ///   Provides data about the event.
        /// </param>
        private void AdornedElement_ContentChanged(object sender, RoutedEventArgs e)
        {
            if (this.isPlaceholderVisible ^ this.IsElementEmpty())
                this.InvalidateVisual();
        }
        #endregion
 
        /// <summary>
        ///   Checks if the content of the adorned element is empty.
        /// </summary>
        /// <returns>
        ///   Returns <see langword="true" /> if the content is empty, <see langword="false" /> otherwise.
        /// </returns>
        private bool IsElementEmpty()
        {
            UIElement adornedElement = AdornedElement;
            TextBox textBox = adornedElement as TextBox;
            if (textBox != null)
                return string.IsNullOrEmpty(textBox.Text);
            PasswordBox passwordBox = adornedElement as PasswordBox;
            if (passwordBox != null)
                return string.IsNullOrEmpty(passwordBox.Password);
            RichTextBox richTextBox = adornedElement as RichTextBox;
            if (richTextBox != null)
            {
                BlockCollection blocks = richTextBox.Document.Blocks;
                if (blocks.Count == 0)
                    return true;
                if (blocks.Count == 1)
                {
                    Paragraph paragraph = blocks.FirstBlock as Paragraph;
                    if (paragraph == null)
                        return false;
                    if (paragraph.Inlines.Count == 0)
                        return true;
                    if (paragraph.Inlines.Count == 1)
                    {
                        Run run = paragraph.Inlines.FirstInline as Run;
                        return run != null && string.IsNullOrEmpty(run.Text);
                    }
                }
 
                return false;
            }
 
            return false;
        }
 
        /// <summary>
        ///   Computes the text alignment of the adorned element.
        /// </summary>
        /// <returns>
        ///   Returns the computed text alignment.
        /// </returns>
        private TextAlignment ComputedTextAlignment()
        {
            Control adornedElement = AdornedElement as Control;
            TextBox textBox = adornedElement as TextBox;
            if (textBox != null)
            {
                if (DependencyPropertyHelper.GetValueSource(textBox, TextBox.HorizontalContentAlignmentProperty)
                        .BaseValueSource != BaseValueSource.Local ||
                    DependencyPropertyHelper.GetValueSource(textBox, TextBox.TextAlignmentProperty)
                        .BaseValueSource == BaseValueSource.Local)
 
                    // TextAlignment dominates
                    return textBox.TextAlignment;
            }
 
            RichTextBox richTextBox = adornedElement as RichTextBox;
            if (richTextBox != null)
            {
                BlockCollection blocks = richTextBox.Document.Blocks;
                TextAlignment textAlignment = richTextBox.Document.TextAlignment;
                if (blocks.Count == 0)
                    return textAlignment;
                if (blocks.Count == 1)
                {
                    Paragraph paragraph = blocks.FirstBlock as Paragraph;
                    if (paragraph == null)
                        return textAlignment;
                    return paragraph.TextAlignment;
                }
 
                return textAlignment;
            }
 
            switch (adornedElement.HorizontalContentAlignment)
            {
                case HorizontalAlignment.Left:
                    return TextAlignment.Left;
                case HorizontalAlignment.Right:
                    return TextAlignment.Right;
                case HorizontalAlignment.Center:
                    return TextAlignment.Center;
                case HorizontalAlignment.Stretch:
                    return TextAlignment.Justify;
            }
 
            return TextAlignment.Left;
        }
    }
}