using System.Windows.Controls;

namespace Tobi.Modules.StatusBar
{
    /// <summary>
    /// Interaction logic for StatusBarView.xaml
    /// </summary>
    public partial class StatusBarView : UserControl, IStatusBarView
    {
        ///<summary>
        /// Default constructor, initializes an empty View without any PresentationModel
        ///</summary>
        public StatusBarView()
        {
            InitializeComponent();
        }

        public StatusBarPresentationModel Model
        {
            get { return DataContext as StatusBarPresentationModel; }
            set { DataContext = value; }
        }
    }

}
