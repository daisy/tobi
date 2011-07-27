using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for NavigationPane.xaml
    /// </summary>
    [Export(typeof(NavigationPane)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class NavigationPane // : INotifyPropertyChanged
    {
        public RichDelegateCommand CommandFocus { get; private set; }

        private readonly IRegionManager m_RegionManager;
        private readonly IShellView m_ShellView;
        private readonly PagePanelView m_PagePaneView;
        private readonly HeadingPanelView m_HeadingPaneView;
        private readonly MarkersPanelView m_MarkersPaneView;

        [ImportingConstructor]
        public NavigationPane(
            IRegionManager regionManager,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(HeadingPanelView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            HeadingPanelView tocView,
            [Import(typeof(PagePanelView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            PagePanelView pagesView,
            [Import(typeof(MarkersPanelView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            MarkersPanelView markersView)
        {
            m_RegionManager = regionManager;
            m_ShellView = shellView;
            m_PagePaneView = pagesView;
            m_HeadingPaneView = tocView;
            m_MarkersPaneView = markersView;

            InitializeComponent();

            RegionManager.SetRegionManager(this, m_RegionManager);
            RegionManager.UpdateRegions();

            CommandFocus = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationFocus_ShortDesc,
                                    null,
                                    null, // KeyGesture obtained from settings (see last parameters below)
                                    m_ShellView.LoadTangoIcon("start-here"),
                                    () =>
                                    {
                                        IRegion tabRegion = m_RegionManager.Regions[RegionNames.NavigationPaneTabs];
                                        
                                        UIElement ui = null;
                                        
                                        foreach (var view in tabRegion.ActiveViews)
                                        {
                                            if (view == m_PagePaneView.ViewControl)
                                            {
                                                ui = m_PagePaneView.ViewFocusStart;
                                                break;
                                            }
                                            if (view == m_HeadingPaneView.ViewControl)
                                            {
                                                ui = m_HeadingPaneView.ViewFocusStart;
                                                break;
                                            }
                                            if (view == m_MarkersPaneView.ViewControl)
                                            {
                                                ui = m_MarkersPaneView.ViewFocusStart;
                                                break;
                                            }

                                            ui = FocusHelper.GetLeafFocusableChild((UIElement)view);

                                            //TODO: what about extensions ??
                                            // ViewFocusStart should be a common denominator implemented by every single tab content
                                        }

                                        if (ui != null)
                                        {
                                            FocusHelper.FocusBeginInvoke(ui);
                                        }
                                    },
                                    () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_Nav));

            m_ShellView.RegisterRichCommand(CommandFocus);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
    }
}
