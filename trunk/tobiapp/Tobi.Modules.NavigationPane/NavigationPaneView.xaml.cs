using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using urakawa;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    /// <summary>
    /// Interaction logic for NavigationPaneView.xaml
    /// </summary>
    public partial class NavigationPaneView : INotifyPropertyChanged
    {
        //private Dictionary<string, TextElement> m_idPageMarkers;


        private ObservableCollection<Page> m_Pages = new ObservableCollection<Page>();
        private HeadingsNavigator m_HeadingsNavigator;


        protected IEventAggregator EventAggregator { private set; get; }
        protected ILoggerFacade Logger { private set; get; }
        protected IUnityContainer Container { private set; get; }

        private bool m_ignoreTreeNodeSelectedEvent = false;
        private bool m_ignoreHeadingSelected = false;
        private bool m_ignorePageSelected = false;
        

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

        private void OnProjectLoaded(Project project)
        {
            m_HeadingsNavigator = new HeadingsNavigator(project);
            TreeView.DataContext = HeadingsNavigator;
            Pages.Clear();
        }

        public void OnPageFoundByFlowDocumentParser(TextElement data)
        {
            Pages.Add(new Page(data));
        }

        public static RichDelegateCommand<object> CommandExpandAll { get; private set; }
        public static RichDelegateCommand<object> CommandCollapseAll { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public NavigationPaneView(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            EventAggregator = eventAggregator;
            Logger = logger;
            Container = container;

            DataContext = this;

            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            CommandExpandAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
                null,
                (VisualBrush)Application.Current.FindResource("list-add"),
                obj => OnExpandAll(null, null), obj => true);

            shellPresenter.RegisterRichCommand(CommandExpandAll);
            //
            CommandCollapseAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                (VisualBrush)Application.Current.FindResource("list-remove"),
                obj => OnCollapseAll(null, null), obj => true);

            shellPresenter.RegisterRichCommand(CommandCollapseAll);

            InitializeComponent();

            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
            EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Subscribe(OnPageFoundByFlowDocumentParser, ThreadOption.UIThread);
        }

        private void OnHeadingSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (m_ignoreHeadingSelected)
            {
                m_ignoreHeadingSelected = false;
                return;
            }

            HeadingTreeNodeWrapper node = TreeView.SelectedItem as HeadingTreeNodeWrapper;
            if (node != null)
            {
                TreeNode treeNode = (node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level);

                UpdatePageListSelection(treeNode);

                m_ignoreTreeNodeSelectedEvent = true;

                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] NavigationPaneView.OnHeadingSelected", Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }
        }

        private void OnPageSelected(object sender, SelectionChangedEventArgs e)
        {
            if (m_ignorePageSelected)
            {
                m_ignorePageSelected = false;
                return;
            }
            //Page page = GetPage(page.Id);
            Page page = ListView.SelectedItem as Page;
            if (page != null)
            {
                TextElement textElement = page.TextElement;
                TreeNode treeNode = textElement.Tag as TreeNode;
                if (treeNode != null)
                {
                    UpdateContentTreeSelection(treeNode);

                    m_ignoreTreeNodeSelectedEvent = true;

                    Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] NavigationPaneView.OnPageSelected", Category.Debug, Priority.Medium);

                    EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
                }
            }
        }

        private void UpdateContentTreeSelection(TreeNode node)
        {
            HeadingTreeNodeWrapper nodeTOC = m_HeadingsNavigator.GetAncestorContainer(node);
            if (nodeTOC != null)
            {
                if (TreeView.SelectedItem != nodeTOC)
                {
                    m_ignoreHeadingSelected = true;
                    TreeView.SelectItem(nodeTOC);
                }
            }
        }
        private void UpdatePageListSelection(TreeNode node)
        {
            Page prevPage = null;
            foreach (Page page in Pages)
            {
                TextElement textElement = page.TextElement;
                TreeNode treeNode = textElement.Tag as TreeNode;
                if (treeNode != null && treeNode.IsAfter(node))
                {
                    Page pageToSelect = prevPage ?? page;
                    if (pageToSelect != ListView.SelectedItem)
                    {
                        m_ignorePageSelected = true;
                        ListView.SelectedItem = pageToSelect;
                        ListView.ScrollIntoView(pageToSelect);
                    }
                    return;
                }
                prevPage = page;
            }
        }

        private void OnSubTreeNodeSelected(TreeNode node)
        {
            OnTreeNodeSelected(node);
        }

        private void OnTreeNodeSelected(TreeNode node)
        {
            if (m_ignoreTreeNodeSelectedEvent)
            {
                m_ignoreTreeNodeSelectedEvent = false;
                return;
            }
            UpdateContentTreeSelection(node);
            UpdatePageListSelection(node);
        }


        public ObservableCollection<Page> Pages
        {
            get
            {
                return m_Pages;
            }
        }
        public HeadingsNavigator HeadingsNavigator
        {
            get
            {
                return m_HeadingsNavigator;
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
