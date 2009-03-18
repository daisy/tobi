using System.Windows;
using System.Windows.Controls;

namespace Tobi.Modules.DocumentPane
{
    /// <summary>
    /// Interaction logic for DocumentPaneView.xaml
    /// </summary>
    public partial class DocumentPaneView : UserControl, IDocumentPaneView
    {
        ///<summary>
        /// Default constructor, initializes an empty View without any PresentationModel
        ///</summary>
        public DocumentPaneView()
        {
            InitializeComponent();
        }

        public DocumentPanePresentationModel Model
        {
            get { return DataContext as DocumentPanePresentationModel; }
            set { DataContext = value; }
        }
    }

}
