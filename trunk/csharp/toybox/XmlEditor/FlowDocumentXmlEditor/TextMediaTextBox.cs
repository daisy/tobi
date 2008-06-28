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

        public void InvalidateBinding() {


            BindingExpression be = GetBindingExpression(TextProperty);
            TextMediaBinding bind = be.ParentBinding as TextMediaBinding;
            bind.RemoveDataModelListener();
        }

        ~TextMediaTextBox()
        {
            InvalidateBinding();
        }
        public TextMediaTextBox(TextMedia originalText)
        {
            OriginalText = originalText;

            BorderThickness = new Thickness(1);
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

        public TextMediaBindableRun CloseSelf(bool saveContent)
        {

            if (Dispatcher.CheckAccess())
            {

                InlineUIContainer container = Parent as InlineUIContainer;

                if (container.Parent is Paragraph)
                {

                    Paragraph paraOld = container.Parent as Paragraph;

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

                    paraOld.Inlines.InsertAfter(container, newRun);
                    paraOld.Inlines.Remove(container);

                    return newRun;

                }



            }
            else
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(CloseSelf));
                int i = 0;
            }


            return null;
        }

        void thisKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextMediaBindableRun run = CloseSelf(true);
                App.mw2.addMouseButtonEventHandler(run);
            }
            else if (e.Key == Key.Escape)
            {
                TextMediaBindableRun run = CloseSelf(false);
                App.mw2.addMouseButtonEventHandler(run);
            }
        }
    }
}
