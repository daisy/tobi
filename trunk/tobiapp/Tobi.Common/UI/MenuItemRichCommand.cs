using System;
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
                                        typeof(RichDelegateCommand),
                                        typeof(MenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menuItem = d as MenuItem;
            if (menuItem == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand;
            if (command == null)
            {
                return;
            }

            ConfigureMenuItemFromCommand(menuItem, command);
        }

        //protected override bool IsEnabledCore
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        public RichDelegateCommand RichCommand
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandProperty);
            }
            set
            {
                SetValue(RichCommandProperty, value);
            }
        }

        public static void ConfigureMenuItemFromCommand(MenuItem menuItem, RichDelegateCommand command)
        {
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
    }

    public class TwoStateMenuItemRichCommand : MenuItem
    {

        public static readonly DependencyProperty InputBindingManagerProperty =
            DependencyProperty.Register("InputBindingManager",
                                        typeof(IInputBindingManager),
                                        typeof(TwoStateMenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnInputBindingManagerChanged)));

        private static void OnInputBindingManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ;
        }

        public IInputBindingManager InputBindingManager
        {
            get
            {
                return (IInputBindingManager)GetValue(InputBindingManagerProperty);
            }
            set
            {
                SetValue(InputBindingManagerProperty, value);
            }
        }


        public static readonly DependencyProperty RichCommandOneProperty =
            DependencyProperty.Register("RichCommandOne",
                                        typeof(RichDelegateCommand),
                                        typeof(TwoStateMenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

        private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand RichCommandOne
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandOneProperty);
            }
            set
            {
                SetValue(RichCommandOneProperty, value);
            }
        }

        public static readonly DependencyProperty RichCommandTwoProperty =
            DependencyProperty.Register("RichCommandTwo",
                                        typeof(RichDelegateCommand),
                                        typeof(TwoStateMenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandTwoChanged)));

        private static void OnRichCommandTwoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand RichCommandTwo
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandTwoProperty);
            }
            set
            {
                SetValue(RichCommandTwoProperty, value);
            }
        }
        public static readonly DependencyProperty RichCommandActiveProperty =
                    DependencyProperty.Register("RichCommandActive",
                                                typeof(Boolean),
                                                typeof(TwoStateMenuItemRichCommand),
                                                new PropertyMetadata(true, OnRichCommandActiveChanged));

        public static void OnRichCommandActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menuItem = d as TwoStateMenuItemRichCommand;
            if (menuItem == null)
            {
                return;
            }
            var choice = (Boolean)e.NewValue;

            RichDelegateCommand command = menuItem.RichCommandOne;

            if (command.KeyGesture == null && menuItem.RichCommandTwo.KeyGesture != null)
            {
                command.KeyGestureText = menuItem.RichCommandTwo.KeyGestureText;
            }

            if (command.KeyGesture != null
                    && command.KeyGesture.Equals(menuItem.RichCommandTwo.KeyGesture)
                    && menuItem.InputBindingManager != null)
            {
                menuItem.InputBindingManager.RemoveInputBinding(menuItem.RichCommandTwo.KeyBinding);
                menuItem.InputBindingManager.AddInputBinding(command.KeyBinding);
            }

            if (!choice)
            {
                command = menuItem.RichCommandTwo;

                if (command.KeyGesture == null && menuItem.RichCommandOne.KeyGesture != null)
                {
                    command.KeyGestureText = menuItem.RichCommandOne.KeyGestureText;
                }

                if (command.KeyGesture != null
                   && command.KeyGesture.Equals(menuItem.RichCommandOne.KeyGesture)
                   && menuItem.InputBindingManager != null)
                {
                    menuItem.InputBindingManager.RemoveInputBinding(menuItem.RichCommandOne.KeyBinding);
                    menuItem.InputBindingManager.AddInputBinding(command.KeyBinding);
                }
            }

            MenuItemRichCommand.ConfigureMenuItemFromCommand(menuItem, command);
        }

        /// <summary>
        /// True => RichCommandOne (default one)
        /// False => RichCommandTwo (alternative one)
        /// </summary>
        public Boolean RichCommandActive
        {
            get
            {
                return (Boolean)GetValue(RichCommandActiveProperty);
            }
            set
            {
                SetValue(RichCommandActiveProperty, value);
            }
        }
    }
}