using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for PagePanelView.xaml
    /// </summary>
    [Export(typeof(IPagePaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class PagePanelView : IPagePaneView
    {
        private bool _ignorePageSelected = false;
        private bool _ignoreTreeNodeSelectedEvent = false;

        private readonly PagesPaneViewModel m_ViewModel;
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        [ImportingConstructor]
        public PagePanelView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(PagesPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            PagesPaneViewModel viewModel)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ViewModel = viewModel;
            m_ViewModel.SetView(this);
            InitializeComponent();
        }
        private void onPageSelected(object sender, SelectionChangedEventArgs e)
        {
            // do nothing here (to avoid selecting in the document and audio views whilst navigating/exploring the page list).
        }
        public void UpdatePageListSelection(TreeNode node)
        {
            if (_ignoreTreeNodeSelectedEvent)
            {
                _ignoreTreeNodeSelectedEvent = false;
                return;
            }
            Page prevPage = null;
            foreach (Page page in m_ViewModel.Pages)
            {
                TextElement textElement = page.TextElement;
                TreeNode treeNode = textElement.Tag as TreeNode;
                if (treeNode != null && treeNode.IsAfter(node))
                {
                    Page pageToSelect = prevPage ?? page;
                    if (pageToSelect != ListView.SelectedItem)
                    {
                        _ignorePageSelected = true;
                        ListView.SelectedItem = pageToSelect;
                        ListView.ScrollIntoView(pageToSelect);
                    }
                    return;
                }
                prevPage = page;
            }
        }


        public const string View_Name = "Pages";
        public string ViewName
        {
            get { return View_Name; }
        }

        public void LoadProject()
        {
            ListView.DataContext = m_ViewModel;
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
            if (_ignorePageSelected)
            {
                _ignorePageSelected = false;
                return;
            }
            Page page = ListView.SelectedItem as Page;
            if (page == null) return;
            TextElement textElement = page.TextElement;
            TreeNode treeNode = textElement.Tag as TreeNode;
            if (treeNode == null) return;

            _ignoreTreeNodeSelectedEvent = true;

            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] PagePanelView.OnPageSelected", Category.Debug, Priority.Medium);

            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
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
            m_ViewModel.SearchTerm = SearchBox.Text;
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
    }
}
