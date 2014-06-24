using Microsoft.Practices.Composite.Presentation.Events;

namespace Tobi.Common
{
    public class ValidationReportRequestEvent : CompositePresentationEvent<object>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }

    public class EscapeEvent : CompositePresentationEvent<object>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
