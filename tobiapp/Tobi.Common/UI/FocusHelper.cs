using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace Tobi.Common.UI
{
    public static class FocusHelper
    {
        public static void Focus(UIElement ui)
        {
            if (ui.Focusable
                &&
                (!ui.IsFocused || !ui.IsKeyboardFocused || Keyboard.FocusedElement != ui)
                )
            {
                ui.Focus();
            }

            // The method call above does all of the below:
            //FocusManager.SetFocusedElement(obj, ui);
            //Keyboard.Focus(ui);
        }

        public static void FocusBeginInvoke(UIElement ui)
        {
            ui.Dispatcher.BeginInvoke(new Action(() => Focus(ui)), DispatcherPriority.Normal);
        }

        public static void FocusThreadAndInvoke(UIElement ui)
        {
            ThreadPool.QueueUserWorkItem(delegate(Object foo) // or: (foo) => {} (LAMBDA)
            {
                var elem = (UIElement)foo;
                Debug.Assert(elem == ui);
                elem.Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)(() => Focus(elem))); // new Action(LAMBDA)
            }, ui);
        }
    }
}
