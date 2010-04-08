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
    [Export(typeof(HeadingPanelView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class HeadingPanelView // : IActiveAware
    {
        private TreeViewItem m_SelectedTreeViewItem;

        private bool m_ignoreTreeNodeSelectedEvent;
        //private bool m_ignoreHeadingSelected;

        private readonly HeadingPaneViewModel m_ViewModel;
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;
        private readonly IUrakawaSession m_UrakawaSession;

        [ImportingConstructor]
        public HeadingPanelView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(HeadingPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            HeadingPaneViewModel viewModel)
        {
            m_UrakawaSession = urakawaSession;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ViewModel = viewModel;
            m_ViewModel.SetView(this);

            m_ignoreTreeNodeSelectedEvent = false;
            //m_ignoreHeadingSelected = false;

            InitializeComponent();
        }

        public string ViewName
        {
            get { return Tobi_Plugin_NavigationPane_Lang.Headings; }
        }

        public void SelectTreeNode(TreeNode node, bool focus)
        {
            if (m_ignoreTreeNodeSelectedEvent)
            {
                m_ignoreTreeNodeSelectedEvent = false;
                return;
            }

            HeadingTreeNodeWrapper nodeTOC = m_ViewModel.HeadingsNavigator.GetAncestorContainer(node);

            SelectTreeNodeWrapper(nodeTOC, focus);
        }

        public void SelectTreeNodeWrapper(HeadingTreeNodeWrapper nodeTOC, bool focus)
        {
            if (nodeTOC == null || TreeView.SelectedItem == nodeTOC) return;
            //m_ignoreHeadingSelected = true;

            m_SelectedTreeViewItem = TreeView.SelectItem(nodeTOC, false);

            if (m_SelectedTreeViewItem != null && focus)
            {
                FocusHelper.FocusBeginInvoke(m_SelectedTreeViewItem);
            }
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
            SearchBox.Text = "";
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

            TreeNode treeNode = (node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level.GetFirstDescendantWithText(true));
            if (treeNode == null) return;

            //if (m_ignoreHeadingSelected)
            //{
            //    m_ignoreHeadingSelected = false;
            //    return;
            //}

            m_ignoreTreeNodeSelectedEvent = true;

            m_SelectedTreeViewItem = TreeView.SelectItem(node, true);

            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] HeadingPaneView.handleTreeViewCurrentSelection", Category.Debug, Priority.Medium);

            m_UrakawaSession.PerformTreeNodeSelection(treeNode);
            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
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
            if (m_ViewModel.HeadingsNavigator == null)
            {
                return;
            }
            m_ViewModel.HeadingsNavigator.ExpandAll();
            m_ViewModel.HeadingsNavigator.SearchTerm = SearchBox.Text;
        }

        //#region IActiveAware implementation
        //private bool _isActive;
        //public bool IsActive
        //{
        //    get { return _isActive; }
        //    set
        //    {
        //        if (_isActive == value) { return; }
        //        _isActive = value;
        //        OnIsActiveChanged(EventArgs.Empty);
        //    }
        //}

        //event EventHandler isActiveChanged;
        //public event EventHandler IsActiveChanged
        //{
        //    add { isActiveChanged += value; }
        //    remove { isActiveChanged -= value; }
        //}
        //protected void OnIsActiveChanged(EventArgs e)
        //{
        //    if (isActiveChanged != null) { isActiveChanged(this, e); }
        //}
        
        //#endregion
    }
}
