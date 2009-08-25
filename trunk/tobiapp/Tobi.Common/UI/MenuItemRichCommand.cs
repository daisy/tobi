using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common.UI
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
            menuItem.ToolTip = command.LongDescription + (command.KeyGesture != null ? " " + command.KeyGestureText + " " : "");

            menuItem.SetValue(AutomationProperties.NameProperty, menuItem.ToolTip);
            //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

            menuItem.InputGestureText = command.KeyGestureText;

            //Image image = command.IconProvider.IconSmall;
            //image.Margin = new Thickness(0, 2, 0, 2);
            //image.VerticalAlignment = VerticalAlignment.Center;


            command.IconProvider.IconMargin_Small = new Thickness(0, 2, 0, 2);
            
            //menuItem.Icon = image;

            var binding = new Binding
                              {
                                  Mode = BindingMode.OneWay,
                                  Source = command.IconProvider,
                                  Path = new PropertyPath("IconSmall")
                              };

            var expr = menuItem.SetBinding(MenuItem.IconProperty, binding);
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