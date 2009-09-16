using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace Tobi.Common.UI
{
    public static class FocusHelper
    {
        public static void Focus(DependencyObject obj, UIElement ui)
        {
            ui.Focusable = true;
            ui.Focus();
            FocusManager.SetFocusedElement(obj, ui);
            Keyboard.Focus(ui);
        }

        public static void FocusBeginInvoke(DependencyObject obj, UIElement ui)
        {
            ui.Dispatcher.BeginInvoke(new Action(() => Focus(obj, ui)), DispatcherPriority.Render);
        }

        public static void FocusThreadAndInvoke(DependencyObject obj, UIElement ui)
        {
            ThreadPool.QueueUserWorkItem(delegate(Object foo)
            {
                var elem = (UIElement)foo;
                elem.Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)(() => Focus(obj, elem)));
            },ui);
        }
    }
}
