using Microsoft.Practices.Composite.Presentation.Events;
using urakawa;

namespace Tobi.Common
{
    public class ProjectLoadedEvent : CompositePresentationEvent<Project>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }

    public class ProjectUnLoadedEvent : CompositePresentationEvent<Project>
    {
        public static ThreadOption THREAD_OPTION = ProjectLoadedEvent.THREAD_OPTION;
    }
}
