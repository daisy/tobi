using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.NavigationPane
{
    public class HeadingPaneModule:IModule
    {
        private readonly IUnityContainer _mContainer;

        public HeadingPaneModule(IUnityContainer container)
        {
            _mContainer = container;
        }
        public void Initialize()
        {
            _mContainer.RegisterType<IHeadingPaneView, HeadingPanelView>(new ContainerControlledLifetimeManager());
            _mContainer.RegisterType<HeadingPaneViewModel>(new ContainerControlledLifetimeManager());

            var regionManager = _mContainer.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.AudioPane, typeof(IAudioPaneView));

            var view = _mContainer.Resolve<IHeadingPaneView>();
            IRegion targetRegion = regionManager.Regions[RegionNames.NavigationPanel];

            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
