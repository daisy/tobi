using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Tobi.Common.UI
{
    public static class FocusHelper
    {
        public static void Focus(DependencyObject obj, UIElement ui)
        {
            ui.Focusable = true;
            ui.Focus();

            // The method call above does all of the below:
            //FocusManager.SetFocusedElement(obj, ui);
            //Keyboard.Focus(ui);
        }

        public static void FocusBeginInvoke(DependencyObject obj, UIElement ui)
        {
            ui.Dispatcher.BeginInvoke(new Action(() => Focus(obj, ui)), DispatcherPriority.Normal);
        }

        public static void FocusThreadAndInvoke(DependencyObject obj, UIElement ui)
        {
            ThreadPool.QueueUserWorkItem(delegate(Object foo) // or: (foo) => {} (LAMBDA)
            {
                var elem = (UIElement)foo;
                Debug.Assert(elem == ui);
                elem.Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)(() => Focus(obj, elem))); // new Action(LAMBDA)
            },ui);
        }
    }
}
