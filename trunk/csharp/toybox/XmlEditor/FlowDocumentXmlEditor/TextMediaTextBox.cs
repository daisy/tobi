using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using urakawa.media;
using FlowDocumentXmlEditor.FlowDocumentExtraction;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace FlowDocumentXmlEditor
{
    class TextMediaTextBox : TextBox
    {
        private TextMedia mOriginalText = null;

        public TextMedia OriginalText
        {
            get { return mOriginalText; }
            set { mOriginalText = value; }
        }

        public void thisLoaded(object sender, RoutedEventArgs e)
        {
            SelectAll();
            Keyboard.Focus(this);
        }

        public void InvalidateBinding()
        {


            BindingExpression be = GetBindingExpression(TextProperty);
            TextMediaBinding bind = be.ParentBinding as TextMediaBinding;
            bind.RemoveDataModelListener();

            //Loaded -= new RoutedEventHandler(thisLoaded); CRASHES because of Thread ownership
        }

        ~TextMediaTextBox()
        {
            InvalidateBinding();

            KeyUp -= new KeyEventHandler(thisKeyUp);
        }
        public TextMediaTextBox(TextMedia originalText)
        {
            OriginalText = originalText;

            BorderThickness = new Thickness(2);
            Focusable = true;

            Loaded += new RoutedEventHandler(thisLoaded);

            KeyUp += new KeyEventHandler(thisKeyUp);

            //Text = originalText.getText();

            TextMediaBinding binding = new TextMediaBinding();
            binding.BoundTextMedia = OriginalText;
            binding.Mode = System.Windows.Data.BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
            SetBinding(TextProperty, binding);
        }

        private TextMediaBindableRun DoCloseSelf(bool saveContent, InlineUIContainer container, InlineCollection inlines)
        {

            if (saveContent)
            {
                BindingExpression be = GetBindingExpression(TextBox.TextProperty);
                be.UpdateSource();

                //mLastInlineBoxed.OriginalText.setText(mLastInlineBoxed.Text);

                if (Text != OriginalText.getText())
                {
                    //newRun.Text = mLastInlineBoxed.Text;
                }
            }

            TextMediaBindableRun newRun = new TextMediaBindableRun(OriginalText);

            InvalidateBinding();

            inlines.InsertAfter(container, newRun);
            inlines.Remove(container);

            return newRun;
        }

        public TextMediaBindableRun CloseSelf(bool saveContent)
        {
            InlineCollection inlines = null;

            InlineUIContainer container = Parent as InlineUIContainer;


            if (container.Parent is Span)
            {
                Span o = container.Parent as Span;
                inlines = o.Inlines;


            }
            else
                if (container.Parent is Paragraph)
                {

                    Paragraph o = container.Parent as Paragraph;

                    inlines = o.Inlines;
                }
                else if (container.Parent is TextBlock)
                {

                    TextBlock o = container.Parent as TextBlock;

                    inlines = o.Inlines;
                }
                else
                {
                    Debug.Print("Should never happen");
                    return null;
                }


            return DoCloseSelf(saveContent, container, inlines);

        }

        void thisKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                TextMediaBindableRun run = CloseSelf(e.Key == Key.Enter);
                App.mw2.addMouseButtonEventHandler(run);
                App.mw2.ResetLastInlineBoxed();
            }
        }
    }
}
