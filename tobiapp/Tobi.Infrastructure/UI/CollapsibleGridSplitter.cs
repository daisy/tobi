using System.Windows;
using System.Windows.Controls;

namespace Tobi.Infrastructure.UI
{
    [TemplatePart(Name = CollapsibleGridSplitter.VerticalCollapseButtonElement, Type = typeof(Button))]
    [TemplatePart(Name = CollapsibleGridSplitter.HorizontalCollapseButtonElement, Type = typeof(Button))]
    public class CollapsibleGridSplitter : GridSplitter
    {
        private const string VerticalCollapseButtonElement = "VerticalCollapseButton";
        private const string HorizontalCollapseButtonElement = "HorizontalCollapseButton";

        protected Button VerticalCollapseButton;
        protected Button HorizontalCollapseButton;

        public CollapsibleGridSplitter()
        {
            this.DefaultStyleKey = typeof(CollapsibleGridSplitter);
        }

        #region CollapseButtonString

        public static readonly DependencyProperty CollapseButtonStringProperty = DependencyProperty.Register("CollapseButtonString", typeof(string), typeof(CollapsibleGridSplitter), null);

        public string CollapseButtonString
        {
            get { return (string)GetValue(CollapseButtonStringProperty); }
            set
            {
                SetValue(CollapseButtonStringProperty, value);
                VerticalCollapseButton.Content = value;
                HorizontalCollapseButton.Content = value;
            }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            VerticalCollapseButton = this.GetTemplateChild(VerticalCollapseButtonElement) as Button;
            if (VerticalCollapseButton != null)
            {
                VerticalCollapseButton.Click += new RoutedEventHandler(OnCollapseButtonClickEvent);
            }

            HorizontalCollapseButton = this.GetTemplateChild(HorizontalCollapseButtonElement) as Button;
            if (HorizontalCollapseButton != null)
            {
                HorizontalCollapseButton.Click += new RoutedEventHandler(OnCollapseButtonClickEvent);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (VerticalCollapseButton != null)
                VerticalCollapseButton.Width = availableSize.Width - 2;
            if (HorizontalCollapseButton != null)
            HorizontalCollapseButton.Height = availableSize.Height - 2;
            return base.MeasureOverride(availableSize);
        }

        public delegate void CollapseButtonClickEventHandler(object sender);
        public event CollapseButtonClickEventHandler CollapseButtonClickEvent;
        void OnCollapseButtonClickEvent(object sender, RoutedEventArgs e)
        {
            if (CollapseButtonClickEvent != null) CollapseButtonClickEvent(sender);
        }

    }
}
