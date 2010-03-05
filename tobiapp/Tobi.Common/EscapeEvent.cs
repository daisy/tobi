using Microsoft.Practices.Composite.Presentation.Events;

namespace Tobi.Common
{
    public class EscapeEvent : CompositePresentationEvent<object>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.UIThread;
    }
}
