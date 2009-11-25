using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for HeadingPanelView.xaml
    /// </summary>
    [Export(typeof(IHeadingPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class HeadingPanelView : IHeadingPaneView
    {
        private TreeViewItem m_SelectedTreeViewItem;

        private bool m_ignoreTreeNodeSelectedEvent;
        private bool m_ignoreHeadingSelected;

        private readonly HeadingPaneViewModel m_ViewModel;
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        [ImportingConstructor]
        public HeadingPanelView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(HeadingPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            HeadingPaneViewModel viewModel)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ViewModel = viewModel;
            m_ViewModel.SetView(this);

            m_ignoreTreeNodeSelectedEvent = false;
            m_ignoreHeadingSelected = false;

            InitializeComponent();
        }


        public const string View_Name = "Headings";
        public string ViewName
        {
            get { return View_Name; }
        }

        public void SelectTreeNode(TreeNode node)
        {
            if (m_ignoreTreeNodeSelectedEvent)
            {
                m_ignoreTreeNodeSelectedEvent = false;
                return;
            }

            HeadingTreeNodeWrapper nodeTOC = m_ViewModel.HeadingsNavigator.GetAncestorContainer(node);
            if (nodeTOC == null || TreeView.SelectedItem == nodeTOC) return;
            m_ignoreHeadingSelected = true;

            m_SelectedTreeViewItem = TreeView.SelectItem(nodeTOC, false);
        }

        public void LoadProject()
        {
            m_SelectedTreeViewItem = null;
            TreeView.DataContext = m_ViewModel.HeadingsNavigator;
//            TreeView.ContextMenu = (ContextMenu)TreeView.Resources["TreeViewContext"];
        }

        public void UnloadProject()
        {
            m_SelectedTreeViewItem = null;
            TreeView.DataContext = null;
            TreeView.ContextMenu = null;
        }

        public UIElement ViewControl
        {
            get { return this; }
        }

        public UIElement ViewFocusStart
        {
            get
            {
                if (m_SelectedTreeViewItem != null)
                {
                    return m_SelectedTreeViewItem;
                }
                return TreeView;
            }
        }


        private void handleTreeViewCurrentSelection()
        {
            HeadingTreeNodeWrapper node = TreeView.SelectedItem as HeadingTreeNodeWrapper;
            if (node == null) return;

            TreeNode treeNode = (node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level.GetFirstDescendantWithText());
            if (treeNode == null) return;

            if (m_ignoreHeadingSelected)
            {
                m_ignoreHeadingSelected = false;
                return;
            }

            m_ignoreTreeNodeSelectedEvent = true;

            m_SelectedTreeViewItem = TreeView.SelectItem(node, true);

            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] HeadingPaneView.handleTreeViewCurrentSelection", Category.Debug, Priority.Medium);

            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
        }

        private void OnKeyDown_TreeViewItem(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                handleTreeViewCurrentSelection();
            }
        }

        private void OnSelectedItemChanged_TreeView(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // do nothing here (to avoid selecting in the document and audio views whilst navigating/exploring the TOC).
        }

        private void OnMouseDoubleClick_TreeItem(object sender, MouseButtonEventArgs e)
        {
            handleTreeViewCurrentSelection();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_ViewModel.HeadingsNavigator == null) { return; }
            m_ViewModel.HeadingsNavigator.SearchTerm = SearchBox.Text;
        }
    }
}
