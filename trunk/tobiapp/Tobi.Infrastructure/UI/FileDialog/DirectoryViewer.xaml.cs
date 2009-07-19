using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tobi.Infrastructure.UI.FileDialog
{
    /// <summary>
    /// Interaction logic for DirectoryViewer.xaml
    /// </summary>
    public partial class DirectoryViewer : UserControl
    {
        #region // Private members
        private ExplorerWindowViewModel _viewModel;
        #endregion

        #region // .ctor
        public DirectoryViewer()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(DirectoryViewer_Loaded);
        }
        #endregion

        #region // Event Handlers
        void DirectoryViewer_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = this.DataContext as ExplorerWindowViewModel;
        }

        private void dirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.DirViewVM.OpenCurrentObject();
        }

        private void dirList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.DirViewVM.OpenCurrentObject();
            }
        }
        #endregion
    }
}
