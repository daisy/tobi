using System;
using System.Windows;

namespace Tobi.Common.MVVM
{
    public class FocusActiveAwareAdapter : ActiveAware
    {
        private readonly UIElement m_UIElement;
        public FocusActiveAwareAdapter(UIElement uiElement)
        {
            IsActive = false;
            m_UIElement = uiElement;

            //m_UIElement.AddHandler(UIElement.GotFocusEvent,
            //    new RoutedEventHandler(OnGotFocus),
            //    true);

            //m_UIElement.AddHandler(UIElement.LostFocusEvent,
            //    new RoutedEventHandler(OnLostFocus),
            //    true);

            m_UIElement.AddHandler(UIElement.LostKeyboardFocusEvent,
                new RoutedEventHandler(OnLostKeyboardFocus),
                true);

            m_UIElement.AddHandler(UIElement.GotKeyboardFocusEvent,
                new RoutedEventHandler(OnGotKeyboardFocus),
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

        private void OnGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                //Console.WriteLine("OnGotFocus same source");
            }
            computeIsActive();
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                //Console.WriteLine("OnGotFocus same source");
            }
            computeIsActive();
        }

        private void OnLostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                //Console.WriteLine("OnLostFocus same source");
            }
            computeIsActive();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                //Console.WriteLine("OnLostFocus same source");
            }
            computeIsActive();
        }

        public FocusActiveAwareAdapter(FrameworkElement frameworkElement)
            : this((UIElement)frameworkElement)
        {
            //loaded and unloaded can occur multiple times in the life cycle of a WPF element
            frameworkElement.Loaded += delegate
            {
                computeIsActive();
            };

            frameworkElement.Unloaded += delegate
            {
                IsActive = false;
            };
        }

        private void computeIsActive()
        {
            IsActive = m_UIElement.IsKeyboardFocusWithin; // || m_UIElement.IsFocused;
        }
    }
}
