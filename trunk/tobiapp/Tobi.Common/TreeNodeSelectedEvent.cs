using System;
using Microsoft.Practices.Composite.Presentation.Events;
using urakawa.core;

namespace Tobi.Common
{
    public class TreeNodeSelectionChangedEvent : CompositePresentationEvent<
        Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
    //public class TreeNodeSelectedEvent : CompositePresentationEvent<TreeNode>
    //{
    //    public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    //}
    //public class SubTreeNodeSelectedEvent : CompositePresentationEvent<TreeNode>
    //{
    //    public static ThreadOption THREAD_OPTION = TreeNodeSelectedEvent.THREAD_OPTION;
    //}
}
