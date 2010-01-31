using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace Tobi.Common
{
    public static class WeakReferencedEventHandlerHelper
    {
        public static void CallWeakReferenceHandlers_WithDispatchCheck(List<WeakReference> weakReferencesOfEventHandlers)
        {
            Dispatcher dispatcher = null;
            if (Application.Current != null)
            {
                dispatcher = Application.Current.Dispatcher;
            }
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                CallWeakReferenceHandlers(weakReferencesOfEventHandlers);
                return;
            }

            dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<List<WeakReference>>)CallWeakReferenceHandlers_WithDispatchCheck, weakReferencesOfEventHandlers);
        }

        public static void CallWeakReferenceHandlers(List<WeakReference> weakReferencesOfEventHandlers)
        {
            if (weakReferencesOfEventHandlers == null)
                return;

            if (weakReferencesOfEventHandlers.Count == 0)
                return;

            // Take a snapshot of the handlers before we call out to them since the handlers
            // could cause the array to me modified while we are reading it.

            EventHandler[] callees = new EventHandler[weakReferencesOfEventHandlers.Count];
            int count = 0;

            for (int i = weakReferencesOfEventHandlers.Count - 1; i >= 0; i--)
            {
                WeakReference reference = weakReferencesOfEventHandlers[i];
                EventHandler handler = reference.Target as EventHandler;
                if (handler == null)
                {
                    // Clean up old handlers that have been collected
                    weakReferencesOfEventHandlers.RemoveAt(i);
                }
                else
                {
                    callees[count] = handler;
                    count++;
                }
            }

            // Call the handlers that we snapshotted
            for (int i = 0; i < count; i++)
            {
                EventHandler handler = callees[i];
                handler(null, EventArgs.Empty);
            }
        }

        public static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler)
        {
            AddWeakReferenceHandler(ref handlers, handler, -1);
        }

        public static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler, int defaultListSize)
        {
            if (handlers == null)
            {
                handlers = (defaultListSize > 0 ? new List<WeakReference>(defaultListSize) : new List<WeakReference>());
            }

            handlers.Add(new WeakReference(handler));
        }

        public static void RemoveWeakReferenceHandler(List<WeakReference> handlers, EventHandler handler)
        {
            if (handlers == null || handlers.Count == 0)
                return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                WeakReference reference = handlers[i];
                EventHandler existingHandler = reference.Target as EventHandler;
                if ((existingHandler == null) || (existingHandler == handler))
                {
                    // Clean up old handlers that have been collected
                    // in addition to the handler that is to be removed.
                    handlers.RemoveAt(i);
                }
            }
        }
    }
}
