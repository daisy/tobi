using System.Windows.Controls;

namespace Tobi.Modules.ToolBars
{
    /// <summary>
    /// Interaction logic for ToolBarsView.xaml
    /// </summary>
    public partial class ToolBarsView : UserControl, IToolBarsView
    {
        ///<summary>
        /// Default constructor, initializes an empty View without any PresentationModel
        ///</summary>
        public ToolBarsView()
        {
            InitializeComponent();
        }

        public ToolBarsPresentationModel Model
        {
            get { return DataContext as ToolBarsPresentationModel; }
            set { DataContext = value; }
        }
    }

}
