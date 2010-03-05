using Microsoft.Practices.Composite.Presentation.Events;

namespace Tobi.Common
{
    public class StatusBarMessageUpdateEvent : CompositePresentationEvent<string>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
