using System.Windows.Documents;
using Microsoft.Practices.Composite.Presentation.Events;

namespace Tobi.Common
{
    public class PageFoundByFlowDocumentParserEvent : CompositePresentationEvent<TextElement>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
