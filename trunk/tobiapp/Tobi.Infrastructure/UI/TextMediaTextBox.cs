using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using urakawa.media;

namespace Tobi.Infrastructure.UI
{
    public class TextMediaTextBox : TextBox
    {
        public TextMediaTextBox(TextMedia originalText)
        {
            OriginalText = originalText;

            BorderThickness = new Thickness(2);
            Focusable = true;

            Loaded += OnLoaded;
            KeyUp += OnKeyUp;

            //Text = originalText.getText();

            var binding = new TextMediaBinding
                              {
                                  BoundTextMedia = OriginalText,
                                  Mode = BindingMode.TwoWay,
                                  UpdateSourceTrigger = UpdateSourceTrigger.Explicit
                              };
            SetBinding(TextProperty, binding);
        }

        ~TextMediaTextBox()
        {
            InvalidateBinding();
            KeyUp -= OnKeyUp;
            Loaded -= OnLoaded;
        }

        public void InvalidateBinding()
        {
            BindingExpression bindExpr = GetBindingExpression(TextProperty);
            if (bindExpr != null)
            {
                var bind = bindExpr.ParentBinding as TextMediaBinding;
                if (bind != null)
                {
                    bind.RemoveDataModelListener();
                }
            }
        }

        public TextMedia OriginalText
        {
            get;
            set;
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            SelectAll();
            Keyboard.Focus(this);
        }

        private BindableRunTextMedia DoCloseSelf(bool saveContent, Inline inline, TextElementCollection<Inline> inlines)
        {
            if (saveContent)
            {
                BindingExpression bindExpr = GetBindingExpression(TextProperty);
                if (bindExpr != null)
                {
                    bindExpr.UpdateSource();
                }

                //mLastInlineBoxed.OriginalText.setText(mLastInlineBoxed.Text);

                if (Text != OriginalText.Text)
                {
                    //newRun.Text = mLastInlineBoxed.Text;
                }
            }

            var newRun = new BindableRunTextMedia(OriginalText);

            InvalidateBinding();

            inlines.InsertAfter(inline, newRun);
            inlines.Remove(inline);

            return newRun;
        }

        public BindableRunTextMedia CloseSelf(bool saveContent)
        {
            var container = Parent as InlineUIContainer;
            if (container == null)
            {
                return null;
            }

            InlineCollection inlines;

            if (container.Parent is Span)
            {
                var o = container.Parent as Span;
                inlines = o.Inlines;
            }
            else if (container.Parent is Paragraph)
            {
                var o = container.Parent as Paragraph;
                inlines = o.Inlines;
            }
            else if (container.Parent is TextBlock)
            {
                var o = container.Parent as TextBlock;
                inlines = o.Inlines;
            }
            else
            {
                Debug.Print("Should never happen");
                return null;
            }

            return DoCloseSelf(saveContent, container, inlines);
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                BindableRunTextMedia run = CloseSelf(e.Key == Key.Enter);

                // TODO: 
                //App.mw2.addMouseButtonEventHandler(run);
                //App.mw2.ResetLastInlineBoxed();
            }
        }
    }
}
