using System.Windows;
using System.Windows.Controls;

namespace Tobi.Infrastructure.UI
{
    public class MenuItemRichCommand : MenuItem
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(MenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menuItem = d as MenuItem;
            if (menuItem == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand<object>;
            if (command == null)
            {
                return;
            }
            menuItem.Command = command;

            menuItem.Header = command.ShortDescription;
            menuItem.ToolTip = command.LongDescription + (command.KeyGesture != null ? " [" + command.KeyGestureText + "]" : "");
            menuItem.InputGestureText = command.KeyGestureText;

            Image image = command.IconSmall;
            image.Margin = new Thickness(0, 2, 0, 2);
            menuItem.Icon = image;
        }

        public RichDelegateCommand<object> RichCommand
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandProperty);
            }
            set
            {
                SetValue(RichCommandProperty, value);
            }
        }
    }
}
