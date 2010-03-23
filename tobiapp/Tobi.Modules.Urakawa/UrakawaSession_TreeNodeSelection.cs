using System;
using System.Diagnostics;
using Tobi.Common;
using urakawa.core;


namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private readonly Object m_TreeNodeSelectionLock = new object();
        private TreeNode m_TreeNode;
        private TreeNode m_SubTreeNode;

        public Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode node)
        {
            if (node == null) throw new ArgumentNullException("node");

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
                var oldTreeNodeSelection = new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);
#if DEBUG
                // After audio edits, the old selection may be invalid.
                //verifyTreeNodeSelection();
#endif
                // performing a valid selection is based on branch logic, it depends on the position of the audio, if any
                bool nodeHasDirectAudio = node.GetManagedAudioMediaOrSequenceMedia() != null;
                TreeNode nodeDescendantAudio = node.GetFirstDescendantWithManagedAudio();
                TreeNode nodeAncestorAudio = node.GetFirstAncestorWithManagedAudio();

                if (m_TreeNode == null) // brand new selection
                {
                    if (nodeHasDirectAudio) // right on
                    {
                        m_TreeNode = node;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeAncestorAudio != null) // shift up
                    {
                        m_TreeNode = nodeAncestorAudio;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeDescendantAudio != null) // sub shift down
                    {
                        m_TreeNode = node;
                        m_SubTreeNode = nodeDescendantAudio;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else // no audio on this branch, always legal position
                    {
                        m_TreeNode = node;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    goto done;
                }

                // From here we know that m_TreeNode != null

                if (node == m_TreeNode)
                {
                    if (nodeHasDirectAudio) // right on
                    {
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeAncestorAudio != null) // shift up
                    {
                        m_TreeNode = nodeAncestorAudio;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeDescendantAudio != null) // shift down
                    {
                        if (m_SubTreeNode == null)
                        {
                            m_TreeNode = nodeDescendantAudio;
                            m_SubTreeNode = null;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else
                        {
                            bool subtreenodeHasDirectAudio = m_SubTreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
                            if (subtreenodeHasDirectAudio)
                            {
                                m_TreeNode = m_SubTreeNode;
                                m_SubTreeNode = null;
#if DEBUG
                                verifyTreeNodeSelection();
#endif
                            }
                            else
                            {
                                TreeNode subtreenodeDescendantAudio = m_SubTreeNode.GetFirstDescendantWithManagedAudio();

                                if (subtreenodeDescendantAudio == null)
                                {
                                    m_TreeNode = nodeDescendantAudio;
                                    m_SubTreeNode = null;
                                }
                                else
                                {
                                    m_TreeNode = subtreenodeDescendantAudio;
                                    m_SubTreeNode = null;
                                }
#if DEBUG
                                verifyTreeNodeSelection();
#endif
                            }
                        }
                    }
                    else // no audio on this branch, always legal position
                    {
                        if (m_SubTreeNode != null) // toggle
                        {
                            m_TreeNode = m_SubTreeNode;
                            m_SubTreeNode = null;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                    }
                    goto done;
                }

                bool treenodeHasDirectAudio = m_TreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
                TreeNode treenodeDescendantAudio = m_TreeNode.GetFirstDescendantWithManagedAudio();
                TreeNode treenodeAncestorAudio = m_TreeNode.GetFirstAncestorWithManagedAudio();

                if (m_SubTreeNode != null && node == m_SubTreeNode)
                {
                    if (nodeHasDirectAudio) // shift down
                    {
                        m_TreeNode = m_SubTreeNode;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else // toggle
                    {
                        m_TreeNode = m_TreeNode; // unchanged
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    goto done;
                }

                if (node.IsDescendantOf(m_TreeNode)) // or: m_TreeNode.IsAncestorOf(node)
                {
                    if (nodeHasDirectAudio) // right on
                    {
                        m_SubTreeNode = node;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeAncestorAudio != null) // shift up
                    {
                        m_SubTreeNode = nodeAncestorAudio;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeDescendantAudio == null) // legal shift down
                    {
                        m_SubTreeNode = node;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else
                    {
                        m_SubTreeNode = node;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    goto done;
                }

                if (node.IsAncestorOf(m_TreeNode)) // or: m_TreeNode.IsAncestorOf(node)
                {
                    if (m_SubTreeNode == null)
                    {
                        m_SubTreeNode = m_TreeNode;
                    }
                    m_TreeNode = node;
#if DEBUG
                    verifyTreeNodeSelection();
#endif
                    goto done;
                }

                //FINAL ELSE:

                m_TreeNode = node;
                if (nodeDescendantAudio != null)
                {
                    m_SubTreeNode = nodeDescendantAudio;
#if DEBUG
                    verifyTreeNodeSelection();
#endif
                }
                else
                {
                    m_SubTreeNode = null;
#if DEBUG
                    verifyTreeNodeSelection();
#endif
                }

            done:

                var treeNodeSelection = new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);

                if (oldTreeNodeSelection != treeNodeSelection)
                    m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Publish(new Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>(oldTreeNodeSelection, treeNodeSelection));

                return treeNodeSelection;
            }
        }

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
                Debug.Assert(m_SubTreeNode == null);
                return;
            }

            TreeNode nodeAncestorAudio = m_TreeNode.GetFirstAncestorWithManagedAudio();
            bool nodeHasDirectAudio = m_TreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
            TreeNode nodeDescendantAudio = m_TreeNode.GetFirstDescendantWithManagedAudio();

            Debug.Assert(nodeAncestorAudio == null);

            if (nodeHasDirectAudio)
            {
                Debug.Assert(nodeDescendantAudio == null);
            }

            if (nodeDescendantAudio != null)
            {
                Debug.Assert(!nodeHasDirectAudio);
            }

            if (m_SubTreeNode == null)
            {
                Debug.Assert(nodeDescendantAudio == null);
            }
            else
            {
                Debug.Assert(m_TreeNode.IsAncestorOf(m_SubTreeNode)); // nodes cannot be equal

                TreeNode subnodeAncestorAudio = m_SubTreeNode.GetFirstAncestorWithManagedAudio();
                bool subnodeHasDirectAudio = m_SubTreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
                TreeNode subnodeDescendantAudio = m_SubTreeNode.GetFirstDescendantWithManagedAudio();

                Debug.Assert(subnodeAncestorAudio == null);
                Debug.Assert(subnodeDescendantAudio == null);
            }
        }
    }
}
