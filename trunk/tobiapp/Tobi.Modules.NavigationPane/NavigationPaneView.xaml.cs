using System.Windows.Controls;

namespace Tobi.Modules.NavigationPane
{
    /// <summary>
    /// Interaction logic for NavigationPaneView.xaml
    /// </summary>
    public partial class NavigationPaneView : UserControl, INavigationPaneView
    {
        ///<summary>
        /// Default constructor, initializes an empty View without any PresentationModel
        ///</summary>
        public NavigationPaneView()
        {
            InitializeComponent();
        }

        public NavigationPanePresentationModel Model
        {
            get { return DataContext as NavigationPanePresentationModel; }
            set { DataContext = value; }
        }
    }

}
