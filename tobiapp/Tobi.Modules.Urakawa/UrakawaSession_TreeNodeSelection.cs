using System;
using System.Diagnostics;
using AudioLib;
using Tobi.Common;
using urakawa.core;


namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private readonly Object m_TreeNodeSelectionLock = new object();
        private TreeNode m_TreeNode;
        private TreeNode m_SubTreeNode;

        public Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode clickedNode)
        {
            return PerformTreeNodeSelection(clickedNode, true, null);
        }

        public Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode clickedNode, bool allowAutoSubNodeAndToggle, TreeNode subClickedNode)
        {
            if (clickedNode == null) throw new ArgumentNullException("clickedNode");

            // Reminder: the "lock" block statement is equivalent to:
            //Monitor.Enter(LOCK);
            //try
            //{
            //    
            //}
            //finally
            //{
            //    Monitor.Exit(LOCK);
            //}
            lock (m_TreeNodeSelectionLock)
            {
                var oldSelection = new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);
#if DEBUG
                // After audio edits, the old selection may be invalid, so we don't check for errors at this point.
                //verifyTreeNodeSelection();
#endif
                // performing a valid selection is based on branch logic, it depends on the position of the audio, if any
                

#if ENABLE_SEQ_MEDIA
                bool clickedHasDirectAudio = clickedNode.GetManagedAudioMediaOrSequenceMedia() != null;
#else
                bool clickedHasDirectAudio = clickedNode.GetManagedAudioMedia() != null;
#endif
                TreeNode clickedDescendantAudio = clickedNode.GetFirstDescendantWithManagedAudio();
                TreeNode clickedAncestorAudio = clickedNode.GetFirstAncestorWithManagedAudio();

                
#if ENABLE_SEQ_MEDIA
                bool subClickedHasDirectAudio = subClickedNode != null && subClickedNode.GetManagedAudioMediaOrSequenceMedia() != null;
#else
                bool subClickedHasDirectAudio = subClickedNode != null && subClickedNode.GetManagedAudioMedia() != null;
#endif

                TreeNode subClickedDescendantAudio = subClickedNode != null ? subClickedNode.GetFirstDescendantWithManagedAudio() : null;
                TreeNode subClickedAncestorAudio = subClickedNode != null ? subClickedNode.GetFirstAncestorWithManagedAudio() : null;

                if (subClickedNode != null)
                {
                    if (subClickedDescendantAudio != null || subClickedAncestorAudio != null
                        || !subClickedNode.IsDescendantOf(clickedNode))
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        subClickedNode = null;
                    }
                }

                if (m_TreeNode == null) // brand new selection
                {
                    DebugFix.Assert(m_SubTreeNode == null);

                    if (clickedHasDirectAudio) // right on
                    {
                        m_TreeNode = clickedNode;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (clickedAncestorAudio != null) // shift up
                    {
                        m_TreeNode = clickedAncestorAudio;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (clickedDescendantAudio != null) // sub shift down
                    {
                        m_TreeNode = clickedNode;
                        m_SubTreeNode = subClickedNode ?? clickedDescendantAudio;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else // no audio on self, no audio on path from self to root, no audio in self's subtree
                    {
                        m_TreeNode = clickedNode;
                        m_SubTreeNode = subClickedNode;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                }
                else // we know that: m_TreeNode != null 
                {
                    
#if ENABLE_SEQ_MEDIA
                bool treenodeHasDirectAudio = m_TreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
#else
                    bool treenodeHasDirectAudio = m_TreeNode.GetManagedAudioMedia() != null;
#endif

                    TreeNode treenodeDescendantAudio = m_TreeNode.GetFirstDescendantWithManagedAudio();
                    TreeNode treenodeAncestorAudio = m_TreeNode.GetFirstAncestorWithManagedAudio();

                    
#if ENABLE_SEQ_MEDIA
                bool subtreenodeHasDirectAudio = m_SubTreeNode != null &&
                                                     m_SubTreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
#else
                    bool subtreenodeHasDirectAudio = m_SubTreeNode != null &&
                                                     m_SubTreeNode.GetManagedAudioMedia() != null;
#endif

                    TreeNode subtreenodeDescendantAudio = m_SubTreeNode == null
                                                              ? null
                                                              : m_SubTreeNode.GetFirstDescendantWithManagedAudio();
                    TreeNode subtreenodeAncestorAudio = m_SubTreeNode == null
                                                            ? null
                                                            : m_SubTreeNode.GetFirstAncestorWithManagedAudio();

                    bool clickedIsTreeNode = clickedNode == m_TreeNode;
                    bool clickedIsTreeNodeAncestor = clickedNode.IsAncestorOf(m_TreeNode);
                    bool clickedIsTreeNodeDescendant = clickedNode.IsDescendantOf(m_TreeNode);

                    bool clickedIsSubTreeNode = m_SubTreeNode != null && clickedNode == m_SubTreeNode;
                    bool clickedIsSubTreeNodeAncestor = m_SubTreeNode != null && clickedNode.IsAncestorOf(m_SubTreeNode);
                    bool clickedIsSubTreeNodeDescendant = m_SubTreeNode != null &&
                                                          clickedNode.IsDescendantOf(m_SubTreeNode);

                    if (clickedHasDirectAudio) // right on
                    {
                        if (allowAutoSubNodeAndToggle && subClickedNode == null && clickedIsTreeNodeDescendant && !clickedIsSubTreeNode)
                        {
                            m_SubTreeNode = clickedNode;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else
                        {
                            m_TreeNode = clickedNode;
                            m_SubTreeNode = null;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                    }
                    else if (clickedAncestorAudio != null) // shift up
                    {
                        if (allowAutoSubNodeAndToggle && subClickedNode == null && clickedAncestorAudio.IsDescendantOf(m_TreeNode))
                        {
                            m_SubTreeNode = clickedAncestorAudio;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else
                        {
                            m_TreeNode = clickedAncestorAudio;
                            m_SubTreeNode = null;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                    }
                    else if (clickedDescendantAudio != null) // shift down
                    {
                        if (subtreenodeHasDirectAudio && clickedIsSubTreeNodeAncestor)
                        {
                            if (allowAutoSubNodeAndToggle && subClickedNode == null && clickedIsTreeNode) // toggle
                            {
                                m_TreeNode = m_SubTreeNode;
                                m_SubTreeNode = null;
#if DEBUG
                                verifyTreeNodeSelection();
#endif
                            }
                            else
                            {
                                m_TreeNode = clickedNode;
                                if (subClickedNode != null)
                                {
                                    m_SubTreeNode = subClickedNode;
                                }
#if DEBUG
                                verifyTreeNodeSelection();
#endif
                            }
                        }
                        else if (treenodeHasDirectAudio && clickedIsTreeNodeAncestor)
                        {
                            m_SubTreeNode = subClickedNode ?? m_TreeNode;
                            m_TreeNode = clickedNode;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else
                        {
                            m_TreeNode = clickedNode;
                            if (m_SubTreeNode == null || !m_SubTreeNode.IsDescendantOf(m_TreeNode) || subClickedNode != null)
                            {
                                m_SubTreeNode = subClickedNode ?? clickedDescendantAudio;
                            }
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                    }
                    else  // no audio on self, no audio on path from self to root, no audio in self's subtree
                    {
                        if (clickedIsSubTreeNode)// toggle
                        {
                            if (allowAutoSubNodeAndToggle && subClickedNode == null)
                            {
                                if (treenodeDescendantAudio != null)
                                {
                                    m_SubTreeNode = treenodeDescendantAudio;
                                }
                                else
                                {
                                    m_SubTreeNode = null;
                                }
                            }
                            else
                            {
                                m_TreeNode = clickedNode;
                                m_SubTreeNode = subClickedNode;
                            }
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else if (clickedIsTreeNode)// toggle
                        {
                            if (allowAutoSubNodeAndToggle && subClickedNode == null)
                            {
                                if (m_SubTreeNode != null)
                                {
                                    m_TreeNode = m_SubTreeNode;
                                    m_SubTreeNode = null;
                                }
                            }
                            else
                            {
                                m_TreeNode = clickedNode;
                                m_SubTreeNode = subClickedNode;
                            }
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else if (clickedNode.IsDescendantOf(m_TreeNode))
                        {
                            if (allowAutoSubNodeAndToggle && subClickedNode == null)
                            {
                                m_SubTreeNode = clickedNode;
                            }
                            else
                            {
                                m_TreeNode = clickedNode;
                                m_SubTreeNode = subClickedNode;
                            }
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else if (clickedNode.IsAncestorOf(m_TreeNode))
                        {
                            if (allowAutoSubNodeAndToggle && subClickedNode == null)
                            {
                                if (m_SubTreeNode == null)
                                {
                                    m_SubTreeNode = m_TreeNode;
                                }
                                m_TreeNode = clickedNode;
                            }
                            else
                            {
                                m_TreeNode = clickedNode;
                                m_SubTreeNode = subClickedNode;
                            }
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else
                        {
                            m_TreeNode = clickedNode;
                            m_SubTreeNode = subClickedNode;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                    }
                }

                var treeNodeSelection = new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);

                if (oldSelection != treeNodeSelection)
                {
                    if (m_EventAggregator != null)
                    {
                        m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Publish(
                            new Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>(oldSelection,
                                                                                            treeNodeSelection));
                    }
                }

                return treeNodeSelection;
            }
        }

        //public void ForceInitTreeNodeSelection(TreeNode treeNode)
        //{
        //    m_TreeNode = treeNode;
        //    m_SubTreeNode = null;
        //}

        public Tuple<TreeNode, TreeNode> GetTreeNodeSelection()
        {
            lock (m_TreeNodeSelectionLock)
            {
                return new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);
            }
        }

        [Conditional("DEBUG")]
        private void verifyTreeNodeSelection()
        {
            if (m_TreeNode == null)
            {
                DebugFix.Assert(m_SubTreeNode == null);
                return;
            }

            TreeNode nodeAncestorAudio = m_TreeNode.GetFirstAncestorWithManagedAudio();
            

#if ENABLE_SEQ_MEDIA
            bool nodeHasDirectAudio = m_TreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
#else
            bool nodeHasDirectAudio = m_TreeNode.GetManagedAudioMedia() != null;
#endif

            TreeNode nodeDescendantAudio = m_TreeNode.GetFirstDescendantWithManagedAudio();

            DebugFix.Assert(nodeAncestorAudio == null);

            if (nodeHasDirectAudio)
            {
                DebugFix.Assert(nodeDescendantAudio == null);
                DebugFix.Assert(m_SubTreeNode == null);
            }

            if (nodeDescendantAudio != null)
            {
                DebugFix.Assert(!nodeHasDirectAudio);
            }

            if (m_SubTreeNode == null)
            {
                DebugFix.Assert(nodeDescendantAudio == null);
            }
            else
            {
                DebugFix.Assert(m_TreeNode.IsAncestorOf(m_SubTreeNode)); // nodes cannot be equal

                TreeNode subnodeAncestorAudio = m_SubTreeNode.GetFirstAncestorWithManagedAudio();
                
#if ENABLE_SEQ_MEDIA
            bool subnodeHasDirectAudio = m_SubTreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
#else
                bool subnodeHasDirectAudio = m_SubTreeNode.GetManagedAudioMedia() != null;
#endif

                TreeNode subnodeDescendantAudio = m_SubTreeNode.GetFirstDescendantWithManagedAudio();

                DebugFix.Assert(subnodeAncestorAudio == null);
                DebugFix.Assert(subnodeDescendantAudio == null);
            }
        }
    }
}
