using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.ToolBars
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class ToolBarsModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public ToolBarsModule(IUnityContainer container, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _container = container;
        }

        ///<summary>
        /// Registers the <see cref="ToolBarsView"/> into the Dependency Injection container
        /// and injects it inside the '<c>ToolBars</c>' region.
        ///</summary>
        public void Initialize()
        {
            _container.RegisterType<ToolBarsView>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.ToolBars];
            var view = _container.Resolve<ToolBarsView>();

            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
