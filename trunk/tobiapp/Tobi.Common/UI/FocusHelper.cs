using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
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

        /// <summary>
        /// Locate the first real focusable child.  We keep going down
        /// the visual tree until we hit a leaf node.
        /// </summary>
        /// <param name="fe"></param>
        /// <returns></returns>
        public static UIElement GetLeafFocusableChild(UIElement fe)
        {
            UIElement ie = GetFirstFocusableChild(fe), final = ie;
            while (final != null)
            {
                ie = final;
                final = GetFirstFocusableChild(final);
            }

            return ie;
        }

        /// <summary>
        /// This searches the Visual Tree looking for a valid child which can have focus.
        /// </summary>
        /// <param name="fe"></param>
        /// <returns></returns>
        public static UIElement GetFirstFocusableChild(UIElement fe)
        {
            var dpo = fe as DependencyObject;
            return dpo == null ? null : (from vc in EnumerateVisualTree(dpo, c => !FocusManager.GetIsFocusScope(c))
                                         let iic = vc as UIElement
                                         where iic != null && iic.Focusable && iic.IsEnabled &&
                                               (!(iic is FrameworkElement) || (((FrameworkElement)iic).IsVisible))
                                         select iic).FirstOrDefault();
        }

        /// <summary>
        /// A simple iterator method to expose the visual tree to LINQ
        /// </summary>
        /// <param name="start"></param>
        /// <param name="eval"></param>
        /// <returns></returns>
       public static IEnumerable<T> EnumerateVisualTree<T>(T start, Predicate<T> eval) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++)
            {
                var child = VisualTreeHelper.GetChild(start, i) as T;
                if (child != null && (eval != null && eval(child)))
                {
                    yield return child;
                    foreach (var childOfChild in EnumerateVisualTree(child, eval))
                        yield return childOfChild;
                }
            }
        }
    }
}
