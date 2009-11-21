using Tobi.Common;

namespace Tobi
{
    /// <summary>
    /// Interaction logic for IconsPreviewDebug.xaml
    /// </summary>
    public partial class IconsPreviewDebug
    {
        private IShellView ShellView
        {
            get; set;
        }

        public IconsPreviewDebug(IShellView shellView)
        {
            ShellView = shellView;
            DataContext = ShellView;

            InitializeComponent();
        }
    }
}
