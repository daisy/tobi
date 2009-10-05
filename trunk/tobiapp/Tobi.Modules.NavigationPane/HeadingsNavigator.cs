using System.Collections.ObjectModel;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
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
        public HeadingsNavigator(Project project, object shellPresenter)
        {
            m_Project = project;
            m_ShellPresenter = (IShellPresenter)shellPresenter;
        }
        private ObservableCollection<HeadingTreeNodeWrapper> m_roots;
        private readonly Project m_Project;
        private readonly IShellPresenter m_ShellPresenter;

        public void ExpandAll()
        {
            foreach (HeadingTreeNodeWrapper node in Roots) { ExpandNode(node); }
        }
        private static void ExpandNode(HeadingTreeNodeWrapper node)
        {
            node.IsExpanded = true;
            if (!node.HasChildren) return;
            foreach (HeadingTreeNodeWrapper child in node.Children){ExpandNode(child);}
        }
        public void Expand(HeadingTreeNodeWrapper node) { node.IsExpanded = true; }

        public void CollapseAll()
        {
            foreach (HeadingTreeNodeWrapper node in Roots) { node.IsExpanded = false; }
        }
        public void Collapse(HeadingTreeNodeWrapper node) { node.IsExpanded = false; }

        public void EditText(HeadingTreeNodeWrapper parameter)
        {
            if (parameter == null) { return; }
            string sTitle = parameter.Title;
            if (sTitle.StartsWith("[")) { sTitle = sTitle.Substring(sTitle.IndexOf("] ") + 1); }
            InputBox myTest = new InputBox(UserInterfaceStrings.HeadingEdit_, sTitle.Trim());
            var windowPopup = new PopupModalWindow(m_ShellPresenter,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                   UserInterfaceStrings.HeadingEdit),
                                                   myTest,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 350, 175);
            windowPopup.ShowDialog();
            if (windowPopup.ClickedDialogButton != PopupModalWindow.DialogButton.Ok) { return; }
            parameter.Title = myTest.tbInput.Text;
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

        public static bool IsLevel(string localXmlName)
        {
            return localXmlName == "level1"
                   || localXmlName == "level2"
                   || localXmlName == "level3"
                   || localXmlName == "level4"
                   || localXmlName == "level5"
                   || localXmlName == "level6"
                   || localXmlName == "level";
        }

        public static bool IsHeading(string localXmlName)
        {
            return localXmlName == "h1"
                   || localXmlName == "h2"
                   || localXmlName == "h3"
                   || localXmlName == "h4"
                   || localXmlName == "h5"
                   || localXmlName == "h6"
                   || localXmlName == "hd";
        }

        public override bool IsIncluded(TreeNode node)
        {
            QualifiedName qname = node.GetXmlElementQName();
            return qname != null && (IsLevel(qname.LocalName) || IsHeading(qname.LocalName));
        }

        private HeadingTreeNodeWrapper findTreeNodeWrapper(TreeNode node)
        {
            foreach (HeadingTreeNodeWrapper root in Roots)
            {
                if (root.WrappedTreeNode_Level == node || root.WrappedTreeNode_LevelHeading == node)
                {
                    return root;
                }
                HeadingTreeNodeWrapper wrapperChild = root.FindTreeNodeWrapper(node);
                if (wrapperChild != null)
                {
                    return wrapperChild;
                }
            }
            return null;
        }

        public HeadingTreeNodeWrapper GetAncestorContainer(TreeNode node)
        {
            if (IsIncluded(node))
            {
                return findTreeNodeWrapper(node);
            }
            if (node.Parent != null)
            {
                return GetAncestorContainer(node.Parent);
            }
            return null;
        }
    }
    public class HeadingTreeNodeWrapper : PropertyChangedNotifyBase
    {
        private TreeNode m_TreeNodeLevel;
        private TreeNode m_TreeNodeHeading;

        private HeadingTreeNodeWrapper m_parent;

        private HeadingsNavigator m_navigator;
        private ObservableCollection<HeadingTreeNodeWrapper> m_children;
        private bool m_isExpanded;
        private HeadingTreeNodeWrapper m_dummyNode;

        public RichDelegateCommand CommandExpandAll { get; private set; }
        //public RichDelegateCommand CommandExpand { get; private set; }
        public RichDelegateCommand CommandCollapseAll { get; private set; }
        //public RichDelegateCommand CommandCollapse { get; private set; }
        //public RichDelegateCommand CommandEditText { get; private set; }

        public HeadingTreeNodeWrapper(HeadingsNavigator navigator, TreeNode node, HeadingTreeNodeWrapper parent)
        {
            CommandExpandAll = HeadingPaneViewModel.CommandExpandAll;
            //CommandExpand = HeadingPaneViewModel.CommandExpand;
            CommandCollapseAll = HeadingPaneViewModel.CommandCollapseAll;
            //CommandCollapse = HeadingPaneViewModel.CommandCollapse;
//            CommandEditText = HeadingPaneViewModel.CommandEditText;

            m_parent = parent;
            m_navigator = navigator;
            m_isExpanded = false;

            m_TreeNodeHeading = null;
            m_TreeNodeLevel = null;


            if (node == null)
            {
                return;
            }

            QualifiedName qName = node.GetXmlElementQName();

            if (qName == null)
            {
                return;
            }

            if (HeadingsNavigator.IsLevel(qName.LocalName))
            {
                m_TreeNodeLevel = node; //WrappedTreeNode_Level
            }
            else if (HeadingsNavigator.IsHeading(qName.LocalName))
            {
                m_TreeNodeHeading = node; //WrappedTreeNode_LevelHeading
            }

            if (WrappedTreeNode_Level != null && WrappedTreeNode_Level.Children.Count > 0)
            {
                TreeNode nd = WrappedTreeNode_Level.Children.Get(0);
                if (nd != null)
                {
                    QualifiedName qname = nd.GetXmlElementQName();
                    if (qname != null && qname.LocalName == "pagenum" && WrappedTreeNode_Level.Children.Count > 1)
                    {
                        nd = WrappedTreeNode_Level.Children.Get(1);
                        if (nd != null)
                        {
                            qname = nd.GetXmlElementQName();
                        }
                    }
                    if (qname != null &&
                        (HeadingsNavigator.IsHeading(qname.LocalName) || qname.LocalName == "doctitle"))
                    {
                        m_TreeNodeHeading = nd;
                    }
                }
            }
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
                if (wrapperChild != null) return wrapperChild;
            }
            return null;
        }

        private void LoadChildren()
        {
            if (WrappedTreeNode_Level == null)
            {
                return;
            }
            if (m_children == null)
            {
                m_children = new ObservableCollection<HeadingTreeNodeWrapper>();

                int n = m_navigator.GetChildCount(WrappedTreeNode_Level);
                for (int index = 0; index < n; index++)
                {
                    TreeNode node = m_navigator.GetChild(WrappedTreeNode_Level, index);

                    if (WrappedTreeNode_Level != null && WrappedTreeNode_LevelHeading == node)
                    {
                        continue;
                    }

                    m_children.Add(new HeadingTreeNodeWrapper(m_navigator, node, this));
                }

                RaisePropertyChanged(() => Children);
            }
        }
        public bool HasChildren
        {
            get
            {
                if (WrappedTreeNode_Level == null)
                {
                    return false;
                }

                int n = m_navigator.GetChildCount(WrappedTreeNode_Level);
                int childrenCount = 0;
                for (int index = 0; index < n; index++)
                {
                    TreeNode node = m_navigator.GetChild(WrappedTreeNode_Level, index);

                    if (WrappedTreeNode_Level != null && WrappedTreeNode_LevelHeading == node)
                    {
                        continue;
                    }

                    childrenCount++;
                }

                return childrenCount > 0;
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
                if (value == m_isExpanded) return;
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

                RaisePropertyChanged(() => IsExpanded);
            }
        }
        public TreeNode WrappedTreeNode_Level
        {
            get
            {
                return m_TreeNodeLevel;
            }
        }
        public TreeNode WrappedTreeNode_LevelHeading
        {
            get
            {
                return m_TreeNodeHeading;
            }
        }


        public string Title
        {
            get
            {
                if (WrappedTreeNode_Level == null && WrappedTreeNode_LevelHeading == null)
                {
                    return "DUMMY";
                }
                string str = (WrappedTreeNode_LevelHeading != null ? "[" + WrappedTreeNode_LevelHeading.GetXmlElementQName().LocalName + "] " + WrappedTreeNode_LevelHeading.GetTextMediaFlattened()
                    : "[" + WrappedTreeNode_Level.GetXmlElementQName().LocalName + "] (No heading)");
                return str.Trim();
            }
            internal set
            {
                if (m_TreeNodeHeading == null || m_TreeNodeLevel==null) { return; }
                if (m_TreeNodeHeading.GetTextMediaFlattened() == value) { return; }
                if (m_TreeNodeHeading.GetTextMedia() != null)
                {
                    m_TreeNodeHeading.GetTextMedia().Text = value;
                }
                else
                {
                    if (m_TreeNodeHeading.Children.Count > 0)
                    {
                        m_TreeNodeHeading.Children.Get(0).GetTextMedia().Text = value;
                    }
                    else { return; }
                }
                RaisePropertyChanged(() => Title);
            }
        }
        public ObservableCollection<HeadingTreeNodeWrapper> Children
        {
            get
            {
                if (WrappedTreeNode_Level == null)
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
