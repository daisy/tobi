using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Data;
using FlowDocumentXmlEditor.FlowDocumentExtraction;

namespace FlowDocumentXmlEditor
{
    /// <summary>
    /// A subclass of the Run element that exposes a DependencyProperty property 
    /// to allow data binding.
    /// </summary>
    public class BindableRun : Run
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(BindableRun), new PropertyMetadata(new PropertyChangedCallback(BindableRun.OnTextChanged)));
        
        public void InvalidateBinding() {


            BindingExpression be = GetBindingExpression(TextProperty);
            TextMediaBinding bind = be.ParentBinding as TextMediaBinding;
            bind.RemoveDataModelListener();
        }

        ~BindableRun()
        {
            InvalidateBinding();
        }
        public BindableRun()
            : base()
        {
            Binding b = new Binding("DataContext");
            b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FrameworkElement), 1);
            this.SetBinding(DataContextProperty, b);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Run)d).Text = (string)e.NewValue;
        }

        public new string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
    }
}