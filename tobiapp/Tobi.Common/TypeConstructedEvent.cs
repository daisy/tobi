using System;
using Microsoft.Practices.Composite.Presentation.Events;

namespace Tobi.Common
{
    public class TypeConstructedEvent : CompositePresentationEvent<Type>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
