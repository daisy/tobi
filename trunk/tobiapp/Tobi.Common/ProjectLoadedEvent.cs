using Microsoft.Practices.Composite.Presentation.Events;
using urakawa;

namespace Tobi.Common
{
    public class ProjectLoadedEvent : CompositePresentationEvent<Project>
    {
    }

    public class ProjectUnLoadedEvent : CompositePresentationEvent<Project>
    {
    }
}
