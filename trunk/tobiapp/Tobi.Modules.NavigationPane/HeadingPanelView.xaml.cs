using System;
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
    public partial class HeadingPanelView : ITobiViewFocusable // : IActiveAware
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
            DataContext = m_ViewModel;

            m_ignoreTreeNodeSelectedEvent = false;
            //m_ignoreHeadingSelected = false;

            InitializeComponent();

            m_ViewModel.SetView(this);
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
        }

        public void UnloadProject()
        {
            m_SelectedTreeViewItem = null;
            SearchBox.Text = "";
        }
        public UIElement FocusableItem
        {
            get
            {
                if (m_SelectedTreeViewItem != null)
                {
                    return m_SelectedTreeViewItem;
                }

                if (TreeView.Focusable) return TreeView;

                return null;
            }
        }
        //public UIElement ViewControl
        //{
        //    get { return this; }
        //}

        //public UIElement ViewFocusStart
        //{
        //    get
        //    {
        //        if (m_SelectedTreeViewItem != null)
        //        {
        //            return m_SelectedTreeViewItem;
        //        }
        //        return TreeView;
        //    }
        //}


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

            //m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] HeadingPaneView.handleTreeViewCurrentSelection", Category.Debug, Priority.Medium);

            m_UrakawaSession.PerformTreeNodeSelection(treeNode);
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

        //private void OnMouseDoubleClick_TreeItem(object sender, MouseButtonEventArgs e)
        //{
        //    handleTreeViewCurrentSelection();
        //}

        private void OnMouseDown_TreeItem(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                handleTreeViewCurrentSelection();
            }
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

        private void OnSearchBoxKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Return && m_ViewModel.CommandFindNext.CanExecute())
            {
                m_ViewModel.CommandFindNext.Execute();
            }

            if (key == Key.Escape)
            {
                SearchBox.Text = "";
                FocusHelper.FocusBeginInvoke(FocusableItem);
            }
        }

        private void OnSearchLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                m_ViewModel.IsSearchVisible = false;
            }
        }


        private void OnUILoaded(object sender, RoutedEventArgs e)
        {
            var item = FocusableItem;
            if (item != null)
            {
                FocusHelper.FocusBeginInvoke(item);
            }
        }
    }
}
