using System.Windows.Documents;
using Microsoft.Practices.Composite.Presentation.Events;
using urakawa.core;

namespace Tobi.Common
{
    public class PageFoundByFlowDocumentParserEvent : CompositePresentationEvent<TreeNode>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
