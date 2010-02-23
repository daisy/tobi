using System;
using System.Windows;

namespace Tobi.Common.MVVM
{
    public class FocusActiveAwareAdapter : ActiveAware
    {
        public FocusActiveAwareAdapter(UIElement uiElement)
        {
            uiElement.AddHandler(UIElement.GotFocusEvent,
                new RoutedEventHandler(OnGotFocus),
                true);

            uiElement.AddHandler(UIElement.LostFocusEvent,
                new RoutedEventHandler(OnLostFocus),
                true);

            //uiElement.IsKeyboardFocusWithinChanged += delegate
            //{
            //    IsActive = uiElement.IsKeyboardFocusWithin;
            //};

            //uiElement.PreviewGotKeyboardFocus += delegate
            //{
            //    IsActive = true;
            //};
            //uiElement.GotFocus += delegate
            //{
            //    IsActive = true;
            //};

            //uiElement.PreviewLostKeyboardFocus += delegate
            //{
            //    IsActive = false;
            //};
            //uiElement.LostFocus += delegate
            //{
            //    IsActive = false;
            //};
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                Console.WriteLine("OnGotFocus same source");
            }

            //Console.WriteLine("OnGotFocus IsActive TRUE");
            IsActive = true;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                Console.WriteLine("OnLostFocus same source");
            }

            //Console.WriteLine("OnLostFocus IsActive FALSE");
            IsActive = false;
        }

        public FocusActiveAwareAdapter(FrameworkElement frameworkElement)
            : this((UIElement)frameworkElement)
        {
            //loaded and unloaded can occur multiple times in the life cycle of a WPF element
            frameworkElement.Loaded += delegate
            {
                IsActive = frameworkElement.IsKeyboardFocusWithin || frameworkElement.IsFocused;
            };

            frameworkElement.Unloaded += delegate
            {
                IsActive = false;
            };
        }
    }
}
