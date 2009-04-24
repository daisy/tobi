using System.Windows;
using System.Windows.Documents;
using System.Windows.Data;

namespace Tobi.Infrastructure.UI
{
    /*
     * Example of use:
     * <TextBlock x:Name="tb">
     *                        Name:
     *     <local:BindableRun
     *           DataContext="{Binding DataContext, ElementName=tb}"
     *           Text="{Binding Name}"/>
     * </TextBlock>
     */
    public class BindableRun : Run
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text",
            typeof(string),
            typeof(BindableRun),
            new PropertyMetadata(new PropertyChangedCallback(OnTextChanged)));
        
        public BindableRun()
        {
            var b = new Binding("DataContext");
            b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FrameworkElement), 1);
            SetBinding(DataContextProperty, b);
        }

        ~BindableRun()
        {
            InvalidateBinding();
        }

        public virtual void InvalidateBinding()
        {
            ;
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