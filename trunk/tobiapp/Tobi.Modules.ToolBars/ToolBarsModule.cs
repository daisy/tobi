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
        /// Registers implementations for <see cref="IToolBarsView"/> (
        /// <see cref="ToolBarsView"/>), <see cref="IToolBarsService"/> (
        /// <see cref="ToolBarsService"/>) and <see cref="IToolBarsPresenter"/> (
        /// <see cref="ToolBarsPresenter"/>). Creates a <see cref="ToolBarsPresenter"/> and
        /// injects its associated <see cref="ToolBarsView"/> inside the '<c>ToolBars</c>'
        /// region.
        ///</summary>
        public void Initialize()
        {
            // TODO: should be a singleton;
            _container.RegisterType<IToolBarsService, ToolBarsService>(new ContainerControlledLifetimeManager());

            _container.RegisterType<IToolBarsView, ToolBarsView>();
            _container.RegisterType<IToolBarsPresenter, ToolBarsPresenter>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.ToolBars];
            var presenter = _container.Resolve<IToolBarsPresenter>();
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
        }
    }
}
