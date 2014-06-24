using System.Windows;
using System.Windows.Controls;

namespace Tobi.Infrastructure.UI.FileDialog
{
    /// <summary>
    /// Interaction logic for FileSystemTree.xaml
    /// </summary>
    public partial class FileSystemTree
    {
        #region // Private Variables
        private ExplorerWindowViewModel myViewModel;
        #endregion

        #region // .ctor
        public FileSystemTree()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(ViewLoaded);
        }
        #endregion

        #region // Event Handlers
        private void ViewLoaded(object sender, RoutedEventArgs r)
        {
            myViewModel = this.DataContext as ExplorerWindowViewModel;
            (DirectoryTree.Items[0] as DirInfo).IsSelected = true;
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            myViewModel.FileTreeVM.CurrentTreeItem = DirectoryTree.SelectedItem as DirInfo;
        }

        private void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem currentTreeNode = sender as TreeViewItem;
            if (currentTreeNode == null)
                return;

            if (currentTreeNode.ItemsSource == null)
                return;

            DirInfo parentDirectory = currentTreeNode.Header as DirInfo;
            if (parentDirectory == null)
                return;

            foreach (DirInfo d in currentTreeNode.ItemsSource)
            {
                if (myViewModel.CurrentDirectory.Path.Equals(d.Path))
                {
                    d.IsSelected = true;
                    d.IsExpanded = true;
                    break;
                }
            }
            e.Handled = true;
        }
        #endregion
    }
}
