using System.Windows;
using Microsoft.Practices.Composite.Regions;
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
                                    obj =>
                                    {

                                        var regionManager = container.Resolve<IRegionManager>();
                                        IRegion tabRegion = regionManager.Regions[RegionNames.NavigationPaneTabs];
                                        
                                        UIElement ui = null;
                                        var pageView = container.Resolve<IPagePaneView>();
                                        var headingView = container.Resolve<IHeadingPaneView>();
                                        foreach (var view in tabRegion.ActiveViews)
                                        {
                                            if (view == pageView.ViewControl)
                                            {
                                                ui = pageView.ViewControl;
                                                break;
                                            }
                                            if (view == headingView.ViewControl)
                                            {
                                                ui = headingView.ViewControl;
                                                break;
                                            }
                                        }

                                        if (ui != null)
                                        {
                                            FocusHelper.Focus(this, ui);
                                        }
                                    },
                                    obj => true);

            shellPresenter.RegisterRichCommand(CommandFocus);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
    }
}
