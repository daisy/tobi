using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    /// Example of use:
    /// 
    /// <MenuItem Header="E_xit" x:Name="exitMenu" Command="{Binding ExitCommand}" >
    /// <wpfutil:MenuShortcutManager.Shortcut>
    /// <KeyBinding Key="X" Modifiers="Control"/>
    /// </wpfutil:MenuShortcutManager.Shortcut>
    /// </MenuItem>
    /// </summary>
    public class MenuShortcutManager
    {
        public static readonly DependencyProperty ShortcutProperty =
                DependencyProperty.RegisterAttached("Shortcut",
                    typeof(KeyBinding),
                    typeof(MenuShortcutManager),
                    new UIPropertyMetadata(null,
                            new PropertyChangedCallback(MenuShortcutManager.ShortcutProperty_Changed)));

        private static WeakReferenceCollection<MenuItem> menuItemsWithShortCuts = null;

        private static ICollection<MenuItem> MenuItemsWithShortCuts
        {
            get
            {
                if (menuItemsWithShortCuts == null)
                {
                    menuItemsWithShortCuts = new WeakReferenceCollection<MenuItem>();
                    InputManager.Current.PostNotifyInput += new NotifyInputEventHandler(Current_PostNotifyInput);
                }
                return menuItemsWithShortCuts;
            }
        }

        private static void ShortcutProperty_Changed(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            MenuItem menuItem = d as MenuItem;

            if (menuItem != null)
            {
                // Setting
                if ((args.NewValue != null) && (args.OldValue == null))
                {
                    MenuShortcutManager.MenuItemsWithShortCuts.Add(menuItem);
                }
                // Clearing
                else if ((args.NewValue == null) && (args.OldValue != null))
                {
                    MenuShortcutManager.MenuItemsWithShortCuts.Remove(menuItem);
                }
            }
        }

        static void Current_PostNotifyInput(object sender, NotifyInputEventArgs e)
        {
            KeyEventArgs args = e.StagingItem.Input as KeyEventArgs;
            if (args != null)
            {
                if (!args.Handled && args.IsDown)
                {
                    foreach (MenuItem item in MenuItemsWithShortCuts)
                    {
                        if (item != null)
                        {
                            KeyBinding binding = GetShortcut(item);
                            if ((binding != null) && (item.Command != null))
                            {
                                if (binding.Gesture.Matches(item, args))
                                {
                                    if (item.Command.CanExecute(item.CommandParameter))
                                    {
                                        item.Command.Execute(item.CommandParameter);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static KeyBinding GetShortcut(DependencyObject obj)
        {
            return obj.GetValue(MenuShortcutManager.ShortcutProperty) as KeyBinding;
        }

        public static void SetShortcut(DependencyObject obj, KeyBinding value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            obj.SetValue(MenuShortcutManager.ShortcutProperty, value);
        }
    }
}
