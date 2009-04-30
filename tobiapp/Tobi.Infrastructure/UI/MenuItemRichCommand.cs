using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            var menuItem = d as MenuItemRichCommand;
            if (menuItem == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand<object>;
            if (command == null)
            {
                return;
            }
            menuItem.Header = command.ShortDescription;
            menuItem.ToolTip = command.LongDescription;
            menuItem.InputGestureText = command.KeyGestureText;
            menuItem.Icon = command.IconSmall;
        }

        //new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        //)

        public RichDelegateCommand<object> RichCommand
        {
            get { return (RichDelegateCommand<object>)GetValue(RichCommandProperty); }
            set
            {
                SetValue(RichCommandProperty, value);
                Command = value;
            }
        }
    }
}
