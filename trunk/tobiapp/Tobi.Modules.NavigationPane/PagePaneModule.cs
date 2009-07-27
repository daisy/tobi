using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.NavigationPane
{
    public class PagePaneModule:IModule
    {
        private readonly IUnityContainer _mContainer;

        public PagePaneModule(IUnityContainer container)
        {
            _mContainer = container;
        }

        public void Initialize()
        {
            _mContainer.RegisterType<IPagePaneView, PagePanelView>(new ContainerControlledLifetimeManager());
            _mContainer.RegisterType<PagesPaneViewModel>(new ContainerControlledLifetimeManager());

            var regionManager = _mContainer.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.AudioPane, typeof(IAudioPaneView));

            var view = _mContainer.Resolve<IPagePaneView>();
            IRegion targetRegion = regionManager.Regions[RegionNames.NavigationPanel];

            targetRegion.Add(view);
            //targetRegion.Activate(view);  //TCM - This region is not activated because we want the "Headings" tab to display by default.
        }
    }
}
