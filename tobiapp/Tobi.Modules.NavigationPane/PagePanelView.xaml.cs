using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    /// <summary>
    /// Interaction logic for PagePanelView.xaml
    /// </summary>
    public partial class PagePanelView : IPagePaneView
    {
        private bool _ignorePageSelected = false;
        private bool _ignoreTreeNodeSelectedEvent = false;
        private const string TAB_HEADING = "Pages";

        public PagesPaneViewModel ViewModel { get; private set; }
        public PagePanelView(PagesPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.SetView(this);
            InitializeComponent();
        }
        private void onPageSelected(object sender, SelectionChangedEventArgs e)
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

            ViewModel.Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] PagePanelView.OnPageSelected", Category.Debug, Priority.Medium);

            ViewModel.EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
        }
        public void UpdatePageListSelection(TreeNode node)
        {
            if (_ignoreTreeNodeSelectedEvent)
            {
                _ignoreTreeNodeSelectedEvent = false;
                return;
            }
            Page prevPage = null;
            foreach (Page page in ViewModel.Pages)
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

 
        public string ViewName
        {
            get { return TAB_HEADING; }
        }

        public void LoadProject()
        {
            ListView.DataContext = ViewModel;
        }
    }
}
