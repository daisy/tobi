using System.Windows.Controls;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// Interaction logic for AudioPaneView.xaml
    /// </summary>
    public partial class AudioPaneView : UserControl, IAudioPaneView
    {
        ///<summary>
        /// Default constructor, initializes an empty View without any PresentationModel
        ///</summary>
        public AudioPaneView()
        {
            InitializeComponent();
        }

        public AudioPanePresentationModel Model
        {
            get { return DataContext as AudioPanePresentationModel; }
            set { DataContext = value; }
        }
    }

}
