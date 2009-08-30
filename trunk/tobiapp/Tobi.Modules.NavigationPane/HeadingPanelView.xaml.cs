using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    /// <summary>
    /// Interaction logic for HeadingPanelView.xaml
    /// </summary>
    public partial class HeadingPanelView : IHeadingPaneView
    {
        private bool _ignoreTreeNodeSelectedEvent = false;
        private bool _ignoreHeadingSelected = false;
        public HeadingPaneViewModel ViewModel { get; private set; }

        public HeadingPanelView(HeadingPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.SetView(this);
            InitializeComponent();
        }
        private void onHeadingSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_ignoreHeadingSelected)
            {
                _ignoreHeadingSelected = false;
                return;
            }

            HeadingTreeNodeWrapper node = TreeView.SelectedItem as HeadingTreeNodeWrapper;
            if (node == null) return;
            TreeNode treeNode = (node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level.GetFirstDescendantWithText());
            if (treeNode == null) return;

            _ignoreTreeNodeSelectedEvent = true;

            m_SelectedTreeViewItem = TreeView.SelectItem(node, true);

            ViewModel.Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] HeadingPaneView.onHeadingSelected", Category.Debug, Priority.Medium);

            ViewModel.EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
        }

        private TreeViewItem m_SelectedTreeViewItem;
        private void updateContentTreeSelection(TreeNode node)
        {
            HeadingTreeNodeWrapper nodeTOC = ViewModel.HeadingsNavigator.GetAncestorContainer(node);
            if (nodeTOC == null || TreeView.SelectedItem == nodeTOC) return;
            _ignoreHeadingSelected = true;

            m_SelectedTreeViewItem = TreeView.SelectItem(nodeTOC, false);
        }

        public const string View_Name = "Headings";
        public string ViewName
        {
            get { return View_Name; }
        }

        public void SelectTreeNode(TreeNode node)
        {
            if (_ignoreTreeNodeSelectedEvent)
            {
                _ignoreTreeNodeSelectedEvent = false;
                return;
            }
            updateContentTreeSelection(node);
        }

        public void LoadProject()
        {
            m_SelectedTreeViewItem = null;
            TreeView.DataContext = ViewModel.HeadingsNavigator;
        }

        public void UnloadProject()
        {
            m_SelectedTreeViewItem = null;
            TreeView.DataContext = null;
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
    }
}
