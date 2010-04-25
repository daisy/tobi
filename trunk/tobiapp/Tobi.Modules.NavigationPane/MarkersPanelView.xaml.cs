using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for MarkersPanelView.xaml
    /// </summary>
    [Export(typeof(MarkersPanelView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MarkersPanelView // : IMarkersPaneView, IActiveAware
    {
        //private bool _ignoreMarkersSelected = false;
        private bool _ignoreTreeNodeSelectedEvent = false;

        public MarkersPaneViewModel ViewModel
        {
            get; private set;
        }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;
        private readonly IUrakawaSession m_UrakawaSession;

        [ImportingConstructor]
        public MarkersPanelView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(MarkersPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            MarkersPaneViewModel viewModel)
        {
            m_UrakawaSession = urakawaSession;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
            ViewModel.SetView(this);
        }
        private void onMarkersSelected(object sender, SelectionChangedEventArgs e)
        {
            // do nothing here (to avoid selecting in the document and audio views whilst navigating/exploring the page list).
        }
        //public void UpdateMarkersListSelection(TreeNode node)
        //{
        //    //if (_ignoreMarkersSelected)
        //    //{
        //    //    _ignoreMarkersSelected = false;
        //    //    return;
        //    //}
        //    if (_ignoreTreeNodeSelectedEvent)
        //    {
        //        _ignoreTreeNodeSelectedEvent = false;
        //        return;
        //    }
        //    MarkedTreeNode prevMarkers = null;
        //    foreach (MarkedTreeNode mnode in ViewModel.MarkersNavigator.MarkedTreeNodes)
        //    {
        //        TreeNode treeNode = mnode.TreeNode;
        //        if (treeNode != null && treeNode.IsAfter(node))
        //        {
        //            MarkedTreeNode toSelect = prevMarkers ?? mnode;
        //            if (toSelect != ListView.SelectedItem)
        //            {
        //                //_ignoreMarkersSelected = true;
        //                ListView.SelectedItem = toSelect;
        //                ListView.ScrollIntoView(toSelect);
        //            }
        //            return;
        //        }
        //        prevMarkers = mnode;
        //    }
        //}

        public string ViewName
        {
            get { return Tobi_Plugin_NavigationPane_Lang.Marks; }
        }

        public void LoadProject()
        {
            m_LastListItemSelected = null;
        }
        public void UnloadProject()
        {
            m_LastListItemSelected = null;
            SearchBox.Text = "";
        }

        public UIElement ViewControl
        {
            get { return this; }
        }
        public UIElement ViewFocusStart
        {
            get { return ListView; }
        }


        private void handleListCurrentSelection()
        {
            MarkedTreeNode mnode = ListView.SelectedItem as MarkedTreeNode;
            if (mnode == null) return;
            TreeNode treeNode = mnode.TreeNode;

            if (treeNode == null) return;

            _ignoreTreeNodeSelectedEvent = true;

            //m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] MarkersPanelView.OnMarkersSelected", Category.Debug, Priority.Medium);

            m_UrakawaSession.PerformTreeNodeSelection(treeNode);
            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
        }

        private void OnKeyUp_ListItem(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                handleListCurrentSelection();
            }
        }

        private void OnMouseDoubleClick_ListItem(object sender, MouseButtonEventArgs e)
        {
            handleListCurrentSelection();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel.MarkersNavigator == null) { return; }
            ViewModel.MarkersNavigator.SearchTerm = SearchBox.Text;
        }

        internal ListViewItem m_LastListItemSelected;

        private void OnSelected_ListItem(object sender, RoutedEventArgs e)
        {
            Debug.Assert(sender == e.Source);
            m_LastListItemSelected = (ListViewItem)sender;
        }

        private void OnSearchBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && ViewModel.CommandFindNextMarkers.CanExecute())
            {
                ViewModel.CommandFindNextMarkers.Execute();
            }
        }

    }
}
