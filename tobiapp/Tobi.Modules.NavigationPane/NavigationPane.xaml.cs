using System.Windows;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for NavigationPane.xaml
    /// </summary>
    public partial class NavigationPane // : INotifyPropertyChanged
    {
        public RichDelegateCommand CommandFocus { get; private set; }

        public NavigationPane(IUnityContainer container)
        {
            InitializeComponent();

            var shellView = container.Resolve<IShellView>();

            CommandFocus = new RichDelegateCommand(
                UserInterfaceStrings.Navigation_Focus,
                                    null,
                                    UserInterfaceStrings.Navigation_Focus_KEYS,
                                    null,
                                    () =>
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
                                                ui = pageView.ViewFocusStart;
                                                break;
                                            }
                                            if (view == headingView.ViewControl)
                                            {
                                                ui = headingView.ViewFocusStart;
                                                break;
                                            }
                                        }

                                        if (ui != null)
                                        {
                                            FocusHelper.Focus(this, ui);
                                        }
                                    },
                                    () => true);

            shellView.RegisterRichCommand(CommandFocus);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
    }
}
