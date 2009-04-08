using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Practices.Unity;
using Tobi.Modules.DocumentPane;
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
                    TreeNode presentationRootNode = m_Project.GetPresentation(0).RootNode;
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
    }
    public class HeadingTreeNodeWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private TreeNode m_TreeNode;
        private HeadingTreeNodeWrapper m_parent;
        private TreeNode m_TreeNodeHeading;
        private HeadingsNavigator m_navigator;
        private ObservableCollection<HeadingTreeNodeWrapper> m_children;
        private bool m_isExpanded;
        private HeadingTreeNodeWrapper m_dummyNode;

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

                OnPropertyChanged("Children");
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

                    OnPropertyChanged("IsExpanded");
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

        public HeadingTreeNodeWrapper(HeadingsNavigator navigator, TreeNode node, HeadingTreeNodeWrapper parent)
        {
            m_parent = parent;
            m_TreeNode = node;
            m_navigator = navigator;
            m_isExpanded = false;
            m_TreeNodeHeading = null;

            if (m_TreeNode == null)
            {
                return;
            }

            if (m_TreeNode.ChildCount > 0)
            {
                TreeNode nd = m_TreeNode.GetChild(0);
                if (nd != null)
                {
                    QualifiedName qname = nd.GetXmlElementQName();
                    if (qname != null && (qname.LocalName == "hd"
                                          || qname.LocalName == "h1"
                                          || qname.LocalName == "h2"
                                          || qname.LocalName == "h3"
                                          || qname.LocalName == "h4"
                                          || qname.LocalName == "h5"
                                          || qname.LocalName == "h6"
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
                return str;
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

    /// <summary>
    /// Interaction logic for NavigationPaneView.xaml
    /// </summary>
    public partial class NavigationPaneView : INotifyPropertyChanged
    {
        //private Dictionary<string, TextElement> m_idPageMarkers;


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private void OnPageSelected(object sender, SelectionChangedEventArgs e)
        {
            //Page page = GetPage(page.Id);
            Page page = ListView.SelectedItem as Page;
            if (page != null)
            {
                TextElement textElement = page.TextElement;
                //DocumentPaneView docView = m_container.Resolve<DocumentPaneView>();
                m_documentView.BringIntoViewAndHighlight(textElement);
            }
        }
        public void ResetNavigation(Project project)
        {
            m_HeadingsNavigator = new HeadingsNavigator(project);
            TreeView.DataContext = TOC;
            Pages.Clear();
        }

        public void AddPage(TextElement data)
        {
            Pages.Add(new Page(data));
        }

        private readonly DocumentPaneView m_documentView;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public NavigationPaneView(DocumentPaneView docView)
        {
            InitializeComponent();
            m_documentView = docView;
            DataContext = this;
        }

        private ObservableCollection<Page> _Pages = new ObservableCollection<Page>();
        private HeadingsNavigator m_HeadingsNavigator;

        public ObservableCollection<Page> Pages
        {
            get
            {
                return _Pages;
            }
        }
        public HeadingsNavigator TOC
        {
            get
            {
                return m_HeadingsNavigator;
            }
        }

        private void OnHeadingSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HeadingTreeNodeWrapper node = TreeView.SelectedItem as HeadingTreeNodeWrapper;
            if (node != null)
            {
                //DocumentPaneView docView = m_container.Resolve<DocumentPaneView>();
                m_documentView.BringIntoViewAndHighlight((node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level));
            }
        }

        private Page GetPage(string id)
        {
            foreach (Page page in Pages)
            {
                if (page.Id == id) return page;
            }
            return null;
        }

        public class Page
        {
            public Page(TextElement textElement)
            {
                TextElement = textElement;
            }

            public TextElement TextElement
            {
                get;
                private set;
            }
            public string Id
            {
                get
                {
                    return TextElement.Name;
                }
            }
            public string Name
            {
                get
                {
                    if (TextElement is Paragraph)
                    {
                        return extractString((Paragraph)TextElement);
                    }
                    return "??";
                }
            }

            private static string extractString(Paragraph para)
            {
                StringBuilder str = new StringBuilder();
                foreach (Inline inline in para.Inlines)
                {
                    if (inline is Run)
                    {
                        str.Append(((Run)inline).Text);
                    }
                    else if (inline is Span)
                    {
                        str.Append(extractString((Span)inline));
                    }
                }
                return str.ToString();
            }

            private static string extractString(Span span)
            {
                StringBuilder str = new StringBuilder();
                foreach (Inline inline in span.Inlines)
                {
                    if (inline is Run)
                    {
                        str.Append(((Run)inline).Text);
                    }
                    else if (inline is Span)
                    {
                        str.Append(extractString((Span)inline));
                    }
                }
                return str.ToString();
            }
        }

        private void OnExpandAll(object sender, RoutedEventArgs e)
        {
            if (m_HeadingsNavigator != null)
            {
                m_HeadingsNavigator.ExpandAll();
            }
        }
        private void OnCollapseAll(object sender, RoutedEventArgs e)
        {
            if (m_HeadingsNavigator != null)
            {
                m_HeadingsNavigator.CollapseAll();
            }
        }
    }

}
