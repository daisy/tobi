using System.Collections.ObjectModel;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa;
using urakawa.core;
using urakawa.navigation;
using urakawa.xuk;

namespace Tobi.Modules.NavigationPane
{

    public class HeadingsNavigator : AbstractFilterNavigator
    {
        public HeadingsNavigator(Project project)
        {
            m_Project = project;
        }
        private ObservableCollection<HeadingTreeNodeWrapper> m_roots;
        private readonly Project m_Project;

        public void ExpandAll()
        {
            foreach (HeadingTreeNodeWrapper node in Roots)
            {
                node.IsExpanded = true;
            }
        }
        public void CollapseAll()
        {
            foreach (HeadingTreeNodeWrapper node in Roots)
            {
                node.IsExpanded = false;
            }
        }


        public ObservableCollection<HeadingTreeNodeWrapper> Roots
        {
            get
            {
                if (m_roots == null)
                {
                    m_roots = new ObservableCollection<HeadingTreeNodeWrapper>();
                    TreeNode presentationRootNode = m_Project.Presentations.Get(0).RootNode;
                    int n = GetChildCount(presentationRootNode);
                    for (int index = 0; index < n; index++)
                    {
                        TreeNode node = GetChild(presentationRootNode, index);
                        m_roots.Add(new HeadingTreeNodeWrapper(this, node, null));
                    }
                }
                return m_roots;
            }
        }

        //treeView.SelectedNode.EnsureVisible();

        public override bool IsIncluded(TreeNode node)
        {
            QualifiedName qname = node.GetXmlElementQName();
            return qname != null &&
                (qname.LocalName == "level1"
                || qname.LocalName == "level"
                || qname.LocalName == "level2"
                || qname.LocalName == "level3"
                || qname.LocalName == "level4"
                || qname.LocalName == "level5"
                || qname.LocalName == "level6"
                );
        }

        private HeadingTreeNodeWrapper findTreeNodeWrapper(TreeNode node)
        {
            foreach (HeadingTreeNodeWrapper root in Roots)
            {
                if (root.WrappedTreeNode_Level == node)
                {
                    return root;
                }
                HeadingTreeNodeWrapper wrapperChild = root.FindTreeNodeWrapper(node);
                if (wrapperChild != null) return wrapperChild;
            }
            return null;
        }

        public HeadingTreeNodeWrapper GetAncestorContainer(TreeNode node)
        {
            if (IsIncluded(node))
            {
                return findTreeNodeWrapper(node);
            }
            else
            {
                if (node.Parent != null)
                {
                    return GetAncestorContainer(node.Parent);
                }
            }
            return null;
        }
    }
    public class HeadingTreeNodeWrapper : PropertyChangedNotifyBase
    {
        private TreeNode m_TreeNode;
        private HeadingTreeNodeWrapper m_parent;
        private TreeNode m_TreeNodeHeading;
        private HeadingsNavigator m_navigator;
        private ObservableCollection<HeadingTreeNodeWrapper> m_children;
        private bool m_isExpanded;
        private HeadingTreeNodeWrapper m_dummyNode;

        public HeadingTreeNodeWrapper FindTreeNodeWrapper(TreeNode node)
        {
            if (m_TreeNode == node)
            {
                return this;
            }
            IsExpanded = true;
            foreach (HeadingTreeNodeWrapper child in Children)
            {
                HeadingTreeNodeWrapper wrapperChild = child.FindTreeNodeWrapper(node);
                if (wrapperChild != null) return wrapperChild;
            }
            return null;
        }

        private void LoadChildren()
        {
            if (m_TreeNode == null)
            {
                return;
            }
            if (m_children == null)
            {
                m_children = new ObservableCollection<HeadingTreeNodeWrapper>();

                int n = m_navigator.GetChildCount(m_TreeNode);
                for (int index = 0; index < n; index++)
                {
                    TreeNode node = m_navigator.GetChild(m_TreeNode, index);
                    m_children.Add(new HeadingTreeNodeWrapper(m_navigator, node, this));
                }

                OnPropertyChanged(() => Children);
            }
        }
        public bool HasChildren
        {
            get
            {
                if (m_TreeNode == null)
                {
                    return false;
                }
                return m_navigator.GetChildCount(m_TreeNode) > 0;
            }
        }
        public bool IsExpanded
        {
            get
            {
                return m_isExpanded;
            }
            set
            {
                if (value != m_isExpanded)
                {
                    m_isExpanded = value;

                    if (m_isExpanded)
                    {
                        if (m_parent != null)
                        {
                            m_parent.IsExpanded = true;
                        }
                        LoadChildren();
                    }
                    else
                    {
                        m_children = null;
                    }

                    OnPropertyChanged(() => IsExpanded);
                }
            }
        }
        public TreeNode WrappedTreeNode_Level
        {
            get
            {
                return m_TreeNode;
            }
        }
        public TreeNode WrappedTreeNode_LevelHeading
        {
            get
            {
                return m_TreeNodeHeading;
            }
        }

        public RichDelegateCommand<object> CommandExpandAll { get; private set; }
        public RichDelegateCommand<object> CommandCollapseAll { get; private set; }

        public HeadingTreeNodeWrapper(HeadingsNavigator navigator, TreeNode node, HeadingTreeNodeWrapper parent)
        {
            m_parent = parent;
            m_TreeNode = node;
            m_navigator = navigator;
            m_isExpanded = false;
            m_TreeNodeHeading = null;

            CommandExpandAll = HeadingPaneViewModel.CommandExpandAll;
            CommandCollapseAll = HeadingPaneViewModel.CommandCollapseAll;

            if (m_TreeNode == null)
            {
                return;
            }

            if (m_TreeNode.Children.Count > 0)
            {
                TreeNode nd = m_TreeNode.Children.Get(0);
                if (nd != null)
                {
                    QualifiedName qname = nd.GetXmlElementQName();
                    if (qname != null && qname.LocalName == "pagenum" && m_TreeNode.Children.Count > 1)
                    {
                        nd = m_TreeNode.Children.Get(1);
                        if (nd != null)
                        {
                            qname = nd.GetXmlElementQName();
                        }
                    }
                    if (qname != null && (qname.LocalName == "hd"
                                          || qname.LocalName == "h1"
                                          || qname.LocalName == "h2"
                                          || qname.LocalName == "h3"
                                          || qname.LocalName == "h4"
                                          || qname.LocalName == "h5"
                                          || qname.LocalName == "h6"
                                          || qname.LocalName == "doctitle"
                                         ))
                    {
                        m_TreeNodeHeading = nd;
                    }
                }
            }
        }
        public string Title
        {
            get
            {
                if (m_TreeNode == null)
                {
                    return "DUMMY";
                }
                string str = (m_TreeNodeHeading != null ? m_TreeNodeHeading.GetTextMediaFlattened() : "??" + m_TreeNode.GetXmlElementQName().LocalName);
                return str.Trim();
            }
        }
        public ObservableCollection<HeadingTreeNodeWrapper> Children
        {
            get
            {
                if (m_TreeNode == null)
                {
                    return new ObservableCollection<HeadingTreeNodeWrapper>();
                }
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
            }
        }
    }

}
