using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Modules.NavigationPane
{
    /// <summary>
    /// Interaction logic for NavigationPane.xaml
    /// </summary>
    public partial class NavigationPane // : INotifyPropertyChanged
    {
        public RichDelegateCommand<object> CommandFocus { get; private set; }

        public NavigationPane(IUnityContainer container)
        {
            InitializeComponent();

            var shellPresenter = container.Resolve<IShellPresenter>();

            CommandFocus = new RichDelegateCommand<object>(
                UserInterfaceStrings.Navigation_Focus,
                                    null,
                                    UserInterfaceStrings.Navigation_Focus_KEYS,
                                    null,
                                    obj => FocusHelper.Focus(this, FocusStart),
                                    obj => true);

            shellPresenter.RegisterRichCommand(CommandFocus);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
    }
}
