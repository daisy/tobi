using Microsoft.Practices.Composite.Presentation.Events;
using urakawa;

namespace Tobi.Infrastructure
{
    public class ProjectLoadedEvent : CompositePresentationEvent<Project>
    {
    }

    public class ProjectUnLoadedEvent : CompositePresentationEvent<Project>
    {
    }
}
