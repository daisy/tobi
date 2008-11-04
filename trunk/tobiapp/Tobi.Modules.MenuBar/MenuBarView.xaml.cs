using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tobi.Modules.MenuBar
{
    /// <summary>
    /// Interaction logic for MenuBarView.xaml
    /// </summary>
    public partial class MenuBarView : UserControl, IMenuBarView
    {
        ///<summary>
        /// Default constructor, initializes an empty View without any PresentationModel
        ///</summary>
        public MenuBarView()
        {
            InitializeComponent();
        }

        public MenuBarPresentationModel Model
        {
            get { return DataContext as MenuBarPresentationModel; }
            set { DataContext = value; }
        }

        public void EnsureViewMenuCheckState(string regionName, bool visible)
        {
            //TODO make this generic using a mapping between RegionName and an actual menu trigger check box thing
            if (ZoomMenuItem.IsChecked != visible)
                ZoomMenuItem.IsChecked = visible;
        }
    }

}
