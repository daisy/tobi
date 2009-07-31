namespace Tobi
{
    /// <summary>
    /// Interaction logic for IconsPreviewDebug.xaml
    /// </summary>
    public partial class IconsPreviewDebug
    {
        private ShellPresenter ShellPresenter
        {
            get; set;
        }

        public IconsPreviewDebug(ShellPresenter shellPresenter)
        {
            ShellPresenter = shellPresenter;
            DataContext = ShellPresenter;

            InitializeComponent();
        }
    }
}
