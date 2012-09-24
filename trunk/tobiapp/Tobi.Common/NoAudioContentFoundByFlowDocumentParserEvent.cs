using System;
using AudioLib;
using Microsoft.Practices.Composite.Presentation.Events;
using urakawa.core;
using urakawa.daisy;

namespace Tobi.Common
{
    public class NoAudioContentFoundByFlowDocumentParserEvent : CompositePresentationEvent<TreeNode>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}