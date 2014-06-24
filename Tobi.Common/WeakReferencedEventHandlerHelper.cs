using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;

namespace Tobi.Common
{
    // IMPLICIT REGISTRATION
    //        public event EventHandler<EventArgsx<EventPayLoad>> SomethingHappened;

    // EXPLICIT REGISTRATION
    //    private EventHandler<EventArgsx<EventPayLoad>> m_SomethingHappened;
    //    public event EventHandler<EventArgsx<EventPayLoad>> SomethingHappened
    //    {
    //        add
    //        {
    //            m_SomethingHappened += value;
    //        }   
    //        remove
    //        {
    //            m_SomethingHappened -= value;
    //        }
    //    }

    // BRIDGE OLD-NEW EVENT STYLE (old triggers/forwards-to new)
    //        public event SomeEventHandlerSubclass OldSomethingHappened; // handles SomeEventArgsSubclass
    //        public event EventHandler<SomeEventArgsSubclass> NewSomethingHappened;
    //      {
    //        OldSomethingHappened += new SomeEventHandlerSubclass(NewSomethingHappened.Invoke);
    //      }

    public class EventArgsx<T> : EventArgs where T : class //new()
    {
        public new static readonly EventArgsx<T> Empty;
        static EventArgsx()
        {
            Empty = new EventArgsx<T>();
        }

        private EventArgsx() { }

        private readonly T m_PayLoad;

        public EventArgsx(T payLoad)
        {
            m_PayLoad = payLoad;
        }

        public T PayLoad
        {
            get { return m_PayLoad; }
        }
    }

    [Serializable]
    public class WeakReference<T> : WeakReference where T : class//, new()
    {
        public WeakReference(T target)
            : base(target)
        { }

        public WeakReference(T target, bool trackResurrection)
            : base(target, trackResurrection)
        { }
        protected WeakReference(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public new T Target
        {
            get
            {
                return (T)base.Target;
            }
            set
            {
                base.Target = value;
            }
        }
    }

    public static class WeakReferencedEventHandlerHelper
    {
        private delegate void Caller<T>(List<WeakReference<EventHandler<T>>> list, object sender, T eventArgs) where T : EventArgs;

        public static void CallWeakReferenceHandlers_WithDispatchCheck<T>(List<WeakReference<EventHandler<T>>> weakReferencesOfEventHandlers, object sender, T eventArgs) where T : EventArgs
        {
            Dispatcher dispatcher = null;
            if (Application.Current != null)
            {
                dispatcher = Application.Current.Dispatcher;
            }
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                CallWeakReferenceHandlers<T>(weakReferencesOfEventHandlers, sender, eventArgs);
                return;
            }

            //Caller<T> caller = new Caller<T>(CallWeakReferenceHandlers_WithDispatchCheck);

            dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (Caller<T>)CallWeakReferenceHandlers_WithDispatchCheck<T>,
                weakReferencesOfEventHandlers, sender, eventArgs);
        }

        public static void CallWeakReferenceHandlers<T>(List<WeakReference<EventHandler<T>>> weakReferencesOfEventHandlers, object sender, T eventArgs) where T : EventArgs
        {
            if (weakReferencesOfEventHandlers == null)
                return;

            if (weakReferencesOfEventHandlers.Count == 0)
                return;

            // Take a snapshot of the handlers before we call out to them since the handlers
            // could cause the array to me modified while we are reading it.

            var callees = new EventHandler<T>[weakReferencesOfEventHandlers.Count];
            int count = 0;

            for (int i = weakReferencesOfEventHandlers.Count - 1; i >= 0; i--)
            {
                WeakReference<EventHandler<T>> reference = weakReferencesOfEventHandlers[i];
                EventHandler<T> handler = reference.Target;
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
                EventHandler<T> handler = callees[i];
                handler(sender, eventArgs);
            }
        }

        public static void AddWeakReferenceHandler<T>(ref List<WeakReference<EventHandler<T>>> handlers, EventHandler<T> handler) where T : EventArgs
        {
            AddWeakReferenceHandler(ref handlers, handler, -1);
        }

        public static void AddWeakReferenceHandler<T>(ref List<WeakReference<EventHandler<T>>> handlers, EventHandler<T> handler, int defaultListSize) where T : EventArgs
        {
            if (handlers == null)
            {
                handlers = (defaultListSize > 0
                    ? new List<WeakReference<EventHandler<T>>>(defaultListSize)
                    : new List<WeakReference<EventHandler<T>>>());
            }

            handlers.Add(new WeakReference<EventHandler<T>>(handler));
        }

        public static void RemoveWeakReferenceHandler<T>(List<WeakReference<EventHandler<T>>> handlers, EventHandler<T> handler) where T : EventArgs
        {
            if (handlers == null || handlers.Count == 0)
                return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                WeakReference<EventHandler<T>> reference = handlers[i];
                var existingHandler = reference.Target; // as EventHandler<T>;
                if ((existingHandler == null) || (existingHandler == handler))
                {
                    // Clean up old handlers that have been collected
                    // in addition to the handler that is to be removed.
                    handlers.RemoveAt(i);
                }
            }
        }


        private delegate void Caller(List<WeakReference<EventHandler>> list, object sender, EventArgs eventArgs);

        public static void CallWeakReferenceHandlers_WithDispatchCheck(List<WeakReference<EventHandler>> weakReferencesOfEventHandlers, object sender, EventArgs eventArgs)
        {
            Dispatcher dispatcher = null;
            if (Application.Current != null)
            {
                dispatcher = Application.Current.Dispatcher;
            }
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                CallWeakReferenceHandlers(weakReferencesOfEventHandlers, sender, eventArgs);
                return;
            }

            //Action<List<WeakReference>>

            dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (Caller)CallWeakReferenceHandlers_WithDispatchCheck,
                weakReferencesOfEventHandlers, sender, eventArgs);
        }

        public static void CallWeakReferenceHandlers(List<WeakReference<EventHandler>> weakReferencesOfEventHandlers, object sender, EventArgs eventArgs)
        {
            if (weakReferencesOfEventHandlers == null)
                return;

            if (weakReferencesOfEventHandlers.Count == 0)
                return;

            // Take a snapshot of the handlers before we call out to them since the handlers
            // could cause the array to me modified while we are reading it.

            var callees = new EventHandler[weakReferencesOfEventHandlers.Count];
            int count = 0;

            for (int i = weakReferencesOfEventHandlers.Count - 1; i >= 0; i--)
            {
                WeakReference<EventHandler> reference = weakReferencesOfEventHandlers[i];
                var handler = reference.Target;
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
                handler(sender, eventArgs);
            }
        }

        public static void AddWeakReferenceHandler(ref List<WeakReference<EventHandler>> handlers, EventHandler handler)
        {
            AddWeakReferenceHandler(ref handlers, handler, -1);
        }

        public static void AddWeakReferenceHandler(ref List<WeakReference<EventHandler>> handlers, EventHandler handler, int defaultListSize)
        {
            if (handlers == null)
            {
                handlers = (defaultListSize > 0 ? new List<WeakReference<EventHandler>>(defaultListSize) : new List<WeakReference<EventHandler>>());
            }

            handlers.Add(new WeakReference<EventHandler>(handler));
        }

        public static void RemoveWeakReferenceHandler(List<WeakReference<EventHandler>> handlers, EventHandler handler)
        {
            if (handlers == null || handlers.Count == 0)
                return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                WeakReference<EventHandler> reference = handlers[i];
                var existingHandler = reference.Target; // as EventHandler;
                if ((existingHandler == null) || (existingHandler == handler))
                {
                    // Clean up old handlers that have been collected
                    // in addition to the handler that is to be removed.
                    handlers.RemoveAt(i);
                }
            }
        }
    }


    public class WeakDelegatesManager
    {
        private readonly List<DelegateReference> m_Listeners = new List<DelegateReference>();

        public void AddListener(Delegate listener)
        {
            m_Listeners.Add(new DelegateReference(listener, false));
        }

        public void RemoveListener(Delegate listener)
        {
            m_Listeners.RemoveAll(reference =>
            {
                //Remove the listener, and prune collected listeners
                Delegate target = reference.Target;
                return listener.Equals(target) || target == null;
            });
        }

        public void Raise(params object[] args)
        {
            m_Listeners.RemoveAll(listener => listener.Target == null);

            foreach (Delegate handler in m_Listeners.ToList().Select(listener => listener.Target).Where(listener => listener != null))
            {
                handler.DynamicInvoke(args);
            }
        }
    }
}
