using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for PagePanelView.xaml
    /// </summary>
    [Export(typeof(PagePanelView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class PagePanelView // : IPagePaneView, IActiveAware
    {
        //private bool _ignorePageSelected = false;
        private bool _ignoreTreeNodeSelectedEvent = false;

        private readonly PagesPaneViewModel m_ViewModel;
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;
        private readonly IUrakawaSession m_UrakawaSession;

        [ImportingConstructor]
        public PagePanelView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(PagesPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            PagesPaneViewModel viewModel)
        {
            m_UrakawaSession = urakawaSession;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ViewModel = viewModel;
            DataContext = m_ViewModel;

            InitializeComponent();

            m_ViewModel.SetView(this);
        }
        private void onPageSelected(object sender, SelectionChangedEventArgs e)
        {
            // do nothing here (to avoid selecting in the document and audio views whilst navigating/exploring the page list).
        }
        public void UpdatePageListSelection(TreeNode node)
        {
            //if (_ignorePageSelected)
            //{
            //    _ignorePageSelected = false;
            //    return;
            //}
            if (_ignoreTreeNodeSelectedEvent)
            {
                _ignoreTreeNodeSelectedEvent = false;
                return;
            }
            Page prevPage = null;
            foreach (Page page in m_ViewModel.PagesNavigator.Pages)
            {
                TextElement textElement = page.TextElement;
                TreeNode treeNode = textElement.Tag as TreeNode;
                if (treeNode != null && treeNode.IsAfter(node))
                {
                    Page pageToSelect = prevPage ?? page;
                    if (pageToSelect != ListView.SelectedItem)
                    {
                        //_ignorePageSelected = true;
                        ListView.SelectedItem = pageToSelect;
                        ListView.ScrollIntoView(pageToSelect);
                    }
                    return;
                }
                prevPage = page;
            }
            if (prevPage != ListView.SelectedItem)
            {
                //_ignorePageSelected = true;
                ListView.SelectedItem = prevPage;
                ListView.ScrollIntoView(prevPage);
            }
        }

        public string ViewName
        {
            get { return Tobi_Plugin_NavigationPane_Lang.Pages; }
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
            Page page = ListView.SelectedItem as Page;
            if (page == null) return;
            TextElement textElement = page.TextElement;
            TreeNode treeNode = textElement.Tag as TreeNode;
            if (treeNode == null) return;

            _ignoreTreeNodeSelectedEvent = true;

            //m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] PagePanelView.OnPageSelected", Category.Debug, Priority.Medium);

            m_UrakawaSession.PerformTreeNodeSelection(treeNode);
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
            if (m_ViewModel.PagesNavigator == null) { return; }
            m_ViewModel.PagesNavigator.SearchTerm = SearchBox.Text;
        }

        //private void OnMouseDoubleClick_List(object sender, MouseButtonEventArgs e)
        //{
        //    //grab the original element that was doubleclicked on and search from child to parent until
        //    //you find either a ListViewItem or the top of the tree
        //    DependencyObject originalSource = (DependencyObject)e.OriginalSource;
        //    while ((originalSource != null) && !(originalSource is ListViewItem))
        //    {
        //        originalSource = VisualTreeHelper.GetParent(originalSource);
        //    }
        //    //if it didn’t find a ListViewItem anywhere in the hierarch, it’s because the user
        //    //didn’t click on one. Therefore, if the variable isn’t null, run the code
        //    if (originalSource != null && originalSource is ListViewItem)
        //    {
        //        handleListCurrentSelection();
        //    }
        //}

        internal ListViewItem m_LastListItemSelected;

        private void OnSelected_ListItem(object sender, RoutedEventArgs e)
        {
            Debug.Assert(sender == e.Source);
            m_LastListItemSelected = (ListViewItem)sender;
        }
    }
}
