using Microsoft.Practices.Composite.Presentation.Events;
using urakawa.media.timing;

namespace Tobi.Common
{
    public class TotalAudioDurationComputedByFlowDocumentParserEvent : CompositePresentationEvent<Time>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
