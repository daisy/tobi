using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.StatusBar
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class StatusBarModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public StatusBarModule(IUnityContainer container, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _container = container;
        }

        ///<summary>
        /// Registers implementations for <see cref="IStatusBarView"/> (
        /// <see cref="StatusBarView"/>), <see cref="IStatusBarService"/> (
        /// <see cref="StatusBarService"/>) and <see cref="IStatusBarPresenter"/> (
        /// <see cref="StatusBarPresenter"/>). Creates a <see cref="StatusBarPresenter"/> and
        /// injects its associated <see cref="StatusBarView"/> inside the '<c>StatusBar</c>'
        /// region.
        ///</summary>
        public void Initialize()
        {
            // TODO: should be a singleton;
            _container.RegisterType<IStatusBarService, StatusBarService>(new ContainerControlledLifetimeManager());

            _container.RegisterType<IStatusBarView, StatusBarView>();
            _container.RegisterType<IStatusBarPresenter, StatusBarPresenter>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.StatusBar];
            var presenter = _container.Resolve<IStatusBarPresenter>();
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
        }
    }
}
