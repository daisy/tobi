using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Tobi.Common.UI
{
        // DoEvent() equivalents
    public static class DispatcherObjectExtensions
    {
         public static void  DoEvents(this DispatcherObject obj)
         {
             DoEvents(obj, DispatcherPriority.Background);
         }

        public static void  DoEvents(this DispatcherObject obj, DispatcherPriority dispatcherPriority)
        {
            if (!obj.Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                //new ThreadStart();
                obj.Dispatcher.Invoke(DispatcherPriority.Normal, (Action<UIElement, DispatcherPriority>)DoEvents, obj, dispatcherPriority);
                return;
            }

            obj.Dispatcher.Invoke(dispatcherPriority, EmptyDelegate);

            //var frame = new DispatcherFrame();
            //obj.Dispatcher.BeginInvoke(dispatcherPriority,
            //                            new ExitFrameHandler(frm => frm.Continue = false), frame);
            //Dispatcher.PushFrame(frame); // blocks until Continue == false, which happens only when all operations with priority greater than Background have been processed by the Dispatcher.
        }

        private static  Action EmptyDelegate = delegate() { };
        //private delegate void VoidHandler();
        private delegate void ExitFrameHandler(DispatcherFrame frame);
    }
}
