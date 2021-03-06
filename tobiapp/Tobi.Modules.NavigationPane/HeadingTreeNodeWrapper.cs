﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using AudioLib;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.core;
using urakawa.xuk;

namespace Tobi.Plugin.NavigationPane
{

    public class HeadingTreeNodeWrapper : PropertyChangedNotifyBase
    {
        private TreeNode m_TreeNodeLevel;
        public TreeNode WrappedTreeNode_Level
        {
            get
            {
                return m_TreeNodeLevel;
            }
        }

        private TreeNode m_TreeNodeHeading;
        public TreeNode WrappedTreeNode_LevelHeading
        {
            get
            {
                return m_TreeNodeHeading;
            }
        }

        private HeadingTreeNodeWrapper m_parent;

        private HeadingsNavigator m_navigator;
        private ObservableCollection<HeadingTreeNodeWrapper> m_children;
        private bool m_isExpanded;
        private bool m_isMatch;
        private bool m_isSelected;
#if LAZY_LOAD
        private HeadingTreeNodeWrapper m_dummyNode;
#endif
        public RichDelegateCommand CommandExpandAll { get; private set; }
        public RichDelegateCommand CommandCollapseAll { get; private set; }

        private TreeNode.Section m_Section = null;

        public HeadingTreeNodeWrapper(HeadingsNavigator navigator, TreeNode node, TreeNode.Section section, HeadingTreeNodeWrapper parent)
        {
            m_parent = parent;
            m_navigator = navigator;
            m_isExpanded = false;

            m_TreeNodeHeading = null;
            m_TreeNodeLevel = null;

            m_Section = section;

            if ((node == null && m_Section == null)
                || m_navigator == null)
            {
                return;
            }

            CommandExpandAll = m_navigator.ViewModel.CommandExpandAll;
            CommandCollapseAll = m_navigator.ViewModel.CommandCollapseAll;

            Tuple<TreeNode, TreeNode> nodes = ComputeLevelNodes(node, m_Section);
            m_TreeNodeLevel = nodes.Item1;
            m_TreeNodeHeading = nodes.Item2;
        }

        private static Tuple<TreeNode, TreeNode> ComputeLevelNodes(TreeNode node, TreeNode.Section section)
        {
            TreeNode level = null;
            TreeNode heading = null;

            if (section != null)
            {
                DebugFix.Assert(node == null);

                level = section.RealSectioningRootOrContent;
                heading = section.GetRealHeading();
            }
            else
            {
                DebugFix.Assert(node != null);

                if (!node.HasXmlProperty)
                {
#if NET40
                    return new Tuple<TreeNode, TreeNode>(null, null);
#else
                return new Tuple<TreeNode, TreeNode>();
#endif
                }

                string localName = node.GetXmlElementLocalName();

                if (TreeNode.IsLevel(localName))
                {
                    level = node;

                    if (level.Children.Count > 0)
                    {
                        TreeNode nd = level.Children.Get(0);
                        if (nd != null)
                        {
                            localName = nd.HasXmlProperty ? nd.GetXmlElementLocalName() : null;

                            if (localName != null && (localName == "pagenum" || localName == "img") && level.Children.Count > 1)
                            {
                                nd = level.Children.Get(1);
                                if (nd != null)
                                {
                                    localName = nd.GetXmlElementLocalName();
                                }
                            }

                            if (localName != null &&
                                (TreeNode.IsHeading(localName)))
                            {
                                heading = nd;
                            }
                            else
                            {
                                if (localName != null && (localName == "pagenum" || localName == "img") && level.Children.Count > 2)
                                {
                                    nd = level.Children.Get(2);
                                    if (nd != null)
                                    {
                                        localName = nd.GetXmlElementLocalName();
                                    }
                                }

                                if (localName != null &&
                                    (TreeNode.IsHeading(localName)))
                                {
                                    heading = nd;
                                }
                            }
                        }
                    }
                }
                else if (TreeNode.IsHeading(localName))
                {
                    heading = node;
                }
            }

            return new Tuple<TreeNode, TreeNode>(level, heading);
        }

        public HeadingTreeNodeWrapper FindTreeNodeWrapper(TreeNode node)
        {
            if (WrappedTreeNode_Level == node || WrappedTreeNode_LevelHeading == node)
            {
                return this;
            }
            IsExpanded = true;
            foreach (HeadingTreeNodeWrapper child in Children)
            {
                HeadingTreeNodeWrapper wrapperChild = child.FindTreeNodeWrapper(node);
                if (wrapperChild != null)
                {
                    return wrapperChild;
                }
            }
            return null;
        }

        private void LoadChildren()
        {
            if (m_children != null)
            {
                return;
            }

            //TreeNode nody = WrappedTreeNode_Level ?? WrappedTreeNode_LevelHeading;
            //bool html5_outlining = nody.Presentation.RootNode.GetXmlElementLocalName().Equals("body", StringComparison.OrdinalIgnoreCase);

            if (m_Section != null)
            {
                m_children = new ObservableCollection<HeadingTreeNodeWrapper>();

                foreach (var section in m_Section.SubSections)
                {
                    m_children.Add(new HeadingTreeNodeWrapper(m_navigator, null, section, this));
                }
            }
            else
            {
                if (WrappedTreeNode_Level == null)
                {
                    return;
                }

                m_children = new ObservableCollection<HeadingTreeNodeWrapper>();

                int n = m_navigator.GetChildCount(WrappedTreeNode_Level);

                for (int index = 0; index < n; index++)
                {
                    TreeNode node = m_navigator.GetChild(WrappedTreeNode_Level, index);

                    if (WrappedTreeNode_LevelHeading != null
                        && (WrappedTreeNode_LevelHeading == node
                        //|| HeadingsNavigator.IsHeading(node.GetXmlElementLocalName())
                           )
                        )
                    {
                        continue;
                    }

                    m_children.Add(new HeadingTreeNodeWrapper(m_navigator, node, null, this));
                }

                //if (m_children.Count == 0)
                //{
                //    m_children = null;
                //    return;
                //}
            }

            RaisePropertyChanged(() => Children);

            //if (m_children != null)
            //{
            //    m_navigator.FlagSearchMatches(m_children);
            //}
        }

        public int ChildrenCount
        {
            get
            {
                //TreeNode nody = WrappedTreeNode_Level ?? WrappedTreeNode_LevelHeading;
                //bool html5_outlining = nody.Presentation.RootNode.GetXmlElementLocalName().Equals("body", StringComparison.OrdinalIgnoreCase);

                if (m_Section != null)
                {
                    return m_Section.SubSections.Count;
                }
                else
                {
                    if (WrappedTreeNode_Level == null)
                    {
                        return 0;
                    }

                    int n = m_navigator.GetChildCount(WrappedTreeNode_Level);
                    int childrenCount = 0;
                    for (int index = 0; index < n; index++)
                    {
                        TreeNode node = m_navigator.GetChild(WrappedTreeNode_Level, index);

                        if (WrappedTreeNode_LevelHeading == node)
                        {
                            continue;
                        }

                        childrenCount++;
                    }

                    return childrenCount;
                }
            }
        }

        public bool HasChildren
        {
            get
            {
                return ChildrenCount > 0;
            }
        }

        public bool HasMatches
        {
            get
            {
                if (string.IsNullOrEmpty(m_navigator.SearchTerm))
                {
                    return false;
                }

                if (m_Section != null)
                {
                    return CheckMatches(null, m_Section);
                }
                else
                {
                    if (WrappedTreeNode_Level == null)
                    {
                        return false;
                    }
                    return CheckMatches(WrappedTreeNode_Level, null);
                }
            }
        }

        private bool CheckMatches(TreeNode baseNode, TreeNode.Section section)
        {
            //TreeNode nody = WrappedTreeNode_Level ?? WrappedTreeNode_LevelHeading;
            //bool html5_outlining = nody.Presentation.RootNode.GetXmlElementLocalName().Equals("body", StringComparison.OrdinalIgnoreCase);

            bool bResult = false;

            if (section != null)
            {
                DebugFix.Assert(baseNode == null);

                foreach (var subSection in section.SubSections)
                {
                    var tuple = ComputeLevelNodes(null, subSection);
                    string sText = ComputeNodeText(tuple.Item1, tuple.Item2);

                    if (string.IsNullOrEmpty(sText))
                    {
                        continue;
                    }
                    bResult |= sText.IndexOf(m_navigator.SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!bResult)
                    {
                        bResult |= CheckMatches(null, subSection);
                    }
                    if (bResult)
                    {
                        break;
                    }
                }
            }
            else
            {
                DebugFix.Assert(baseNode != null);

                int n = m_navigator.GetChildCount(baseNode);
                for (int index = 0; index < n; index++)
                {
                    TreeNode node = m_navigator.GetChild(baseNode, index);
                    if (WrappedTreeNode_LevelHeading != null && WrappedTreeNode_LevelHeading == node)
                    {
                        continue;
                    }
                    var tuple = ComputeLevelNodes(node, null);
                    string sText = ComputeNodeText(tuple.Item1, tuple.Item2);

                    if (string.IsNullOrEmpty(sText))
                    {
                        continue;
                    }
                    bResult |= sText.IndexOf(m_navigator.SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!bResult)
                    {
                        bResult |= CheckMatches(node, null);
                    }
                    if (bResult)
                    {
                        break;
                    }
                }
            }

            return bResult;
        }

        private static string ComputeNodeText(TreeNode level, TreeNode heading)
        {
            if (level == null && heading == null)
            {
                Debug.Fail("WTF ?");
                return "!??!";
            }

            StringBuilder strBuilder = null;

            if (heading != null)
            {
                TreeNode.StringChunkRange range = heading.GetTextFlattened_();

                if (range == null || range.First == null || string.IsNullOrEmpty(range.First.Str))
                {
                    return "";
                }

                strBuilder = new StringBuilder(range.GetLength());
                TreeNode.ConcatStringChunks(range, -1, strBuilder);

                strBuilder.Insert(0, "] ");
                strBuilder.Insert(0, heading.GetXmlElementLocalName());
                strBuilder.Insert(0, "[");
            }
            else
            {
                strBuilder = new StringBuilder();
                strBuilder.Append("[");
                strBuilder.Append(level.GetXmlElementLocalName());
                strBuilder.Append("] ");
                strBuilder.Append(Tobi_Plugin_NavigationPane_Lang.NoHeading);
            }

            return strBuilder.ToString().Trim();

            //string sResult = string.Empty;
            //QualifiedName qName = node.GetXmlElementQName();
            //if (qName == null) { return sResult; }
            //if (HeadingsNavigator.IsLevel(qName.LocalName))
            //{
            //    if (node.Children.Count > 0)
            //    {
            //        TreeNode nd = node.Children.Get(0);
            //        if (nd != null)
            //        {
            //            QualifiedName qname = nd.GetXmlElementQName();
            //            if (qname != null && qname.LocalName == "pagenum" && node.Children.Count > 1)
            //            {
            //                nd = node.Children.Get(1);
            //                if (nd != null)
            //                {
            //                    qname = nd.GetXmlElementQName();
            //                }
            //            }
            //            if (qname != null &&
            //                (HeadingsNavigator.IsHeading(qname.LocalName) || qname.LocalName == "doctitle"))
            //            {
            //                sResult = nd.GetTextMediaFlattened(true);
            //            }
            //        }
            //    }
            //}
            //else if (HeadingsNavigator.IsHeading(qName.LocalName))
            //{
            //    sResult = node.GetTextMediaFlattened(true);
            //}
            //return sResult;
        }

        public bool IsExpanded
        {
            get
            {
                return m_isExpanded;
            }
            set
            {
                if (value == m_isExpanded) { return; }
                m_isExpanded = value;
#if LAZY_LOAD
                if (m_isExpanded)
                {
                    if (m_parent != null && !m_parent.IsExpanded)
                    {
                        m_parent.IsExpanded = true;
                    }
                    LoadChildren();
                }
                else
                {
                    m_children = null;
                    RaisePropertyChanged(() => Children);
                    //                    m_childSelected = false;
                }
#else
                if (m_parent != null && !m_parent.IsExpanded)
                {
                    m_parent.IsExpanded = true;
                }
#endif

                RaisePropertyChanged(() => IsExpanded);
            }
        }
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if (m_isSelected == value) { return; }
                m_isSelected = value;

                if (m_parent != null && m_isSelected)
                {
                    if (!m_parent.IsExpanded)
                    {
                        m_parent.IsExpanded = true;
                    }
                }
                RaisePropertyChanged(() => IsSelected);
            }
        }
        public bool ChildSelected
        {
            get
            {
                bool bResult = false;
                if (m_children != null)
                {
                    foreach (HeadingTreeNodeWrapper child in m_children)
                    {
                        bResult |= (child.IsSelected || child.ChildSelected);
                        if (bResult)
                        {
                            break;
                        }
                    }
                }
                return bResult;
            }
        }


        internal HeadingTreeNodeWrapper GetChildMatch()
        {
            HeadingTreeNodeWrapper htnwResult = null;
            if (m_children == null)
            {
                LoadChildren();
            }
            foreach (HeadingTreeNodeWrapper child in m_children)
            {
                if (child.SearchMatch)
                {
                    htnwResult = child;
                    break;
                }
                if (!child.HasMatches)
                {
                    continue;
                }
                htnwResult = child.GetChildMatch();
                break;
            }
            return htnwResult;
        }

        internal HeadingTreeNodeWrapper GetPreviousChildMatch()
        {
            HeadingTreeNodeWrapper htnwResult = null;
            if (m_children == null)
            {
                LoadChildren();
            }
            for (int i = m_children.Count - 1; i >= 0; i--)
            {
                if (m_children[i].HasMatches)
                {
                    htnwResult = m_children[i].GetPreviousChildMatch();
                    break;
                }
                if (!m_children[i].SearchMatch)
                {
                    continue;
                }
                htnwResult = m_children[i];
                break;
            }
            return htnwResult;
        }

        public void InvalidateTitle()
        {
            m_Title = null;
            RaisePropertyChanged(() => Title);
        }

        private string m_Title;
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Title))
                {
                    return m_Title;
                }

                m_Title = ComputeNodeText(WrappedTreeNode_Level, WrappedTreeNode_LevelHeading);
                return m_Title;
            }
            //internal set
            //{
            //    if (m_TreeNodeHeading == null || m_TreeNodeLevel == null) { return; }
            //    if (m_TreeNodeHeading.GetTextMediaFlattened(true) == value) { return; }
            //    if (m_TreeNodeHeading.GetTextMedia() != null)
            //    {
            //        m_TreeNodeHeading.GetTextMedia().Text = value;
            //    }
            //    else
            //    {
            //        if (m_TreeNodeHeading.Children.Count > 0)
            //        {
            //            m_TreeNodeHeading.Children.Get(0).GetTextMedia().Text = value;
            //        }
            //        else { return; }
            //    }
            //    RaisePropertyChanged(() => Title);
            //}
        }

        public ObservableCollection<HeadingTreeNodeWrapper> Children
        {
            get
            {
                //TreeNode nody = WrappedTreeNode_Level ?? WrappedTreeNode_LevelHeading;
                //bool html5_outlining = nody.Presentation.RootNode.GetXmlElementLocalName().Equals("body", StringComparison.OrdinalIgnoreCase);

                if (m_Section == null && WrappedTreeNode_Level == null)
                {
                    return new ObservableCollection<HeadingTreeNodeWrapper>();
                }
#if LAZY_LOAD
                if (IsExpanded)
                {
                    if (m_children == null)
                    {
                        LoadChildren();
                    }
                    return m_children;
                }
                var col = new ObservableCollection<HeadingTreeNodeWrapper>();
                if (HasChildren)
                {
                    if (m_dummyNode == null)
                    {
                        m_dummyNode = new HeadingTreeNodeWrapper(null, null, null);
                    }
                    col.Add(m_dummyNode);
                }
                return col;
#else
                if (m_children == null)
                {
                    LoadChildren();
                }
                return m_children;
#endif
            }
        }

        public bool SearchMatch
        {
            get { return m_isMatch; }
            set
            {
                if (m_isMatch == value) { return; }
                m_isMatch = value;
                RaisePropertyChanged(() => SearchMatch);
            }
        }
    }
}
