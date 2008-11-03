using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.NavigationPane
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class NavigationPaneModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public NavigationPaneModule(IUnityContainer container, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _container = container;
        }

        ///<summary>
        /// Registers implementations for <see cref="INavigationPaneView"/> (
        /// <see cref="NavigationPaneView"/>), <see cref="INavigationPaneService"/> (
        /// <see cref="NavigationPaneService"/>) and <see cref="INavigationPanePresenter"/> (
        /// <see cref="NavigationPanePresenter"/>). Creates a <see cref="NavigationPanePresenter"/> and
        /// injects its associated <see cref="NavigationPaneView"/> inside the '<c>NavigationPane</c>'
        /// region.
        ///</summary>
        public void Initialize()
        {
            // TODO: should be a singleton;
            _container.RegisterType<INavigationPaneService, NavigationPaneService>(new ContainerControlledLifetimeManager());

            _container.RegisterType<INavigationPaneView, NavigationPaneView>();
            _container.RegisterType<INavigationPanePresenter, NavigationPanePresenter>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.NavigationPane];
            var presenter = _container.Resolve<INavigationPanePresenter>();
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
        }
    }
}
