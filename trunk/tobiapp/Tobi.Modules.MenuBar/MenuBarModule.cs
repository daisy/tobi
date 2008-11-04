using System.Windows.Input;
using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.MenuBar
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class MenuBarModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public MenuBarModule(IUnityContainer container, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _container = container;
        }

        ///<summary>
        /// Registers implementations for <see cref="IMenuBarView"/> (
        /// <see cref="MenuBarView"/>), <see cref="IMenuBarService"/> (
        /// <see cref="MenuBarService"/>) and <see cref="IMenuBarPresenter"/> (
        /// <see cref="MenuBarPresenter"/>). Creates a <see cref="MenuBarPresenter"/> and
        /// injects its associated <see cref="MenuBarView"/> inside the '<c>MenuBar</c>'
        /// region.
        ///</summary>
        public void Initialize()
        {
            _container.RegisterType<IMenuBarService, MenuBarService>(new ContainerControlledLifetimeManager());

            _container.RegisterType<IMenuBarView, MenuBarView>();
            _container.RegisterType<IMenuBarPresenter, MenuBarPresenter>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.MenuBar];
            var presenter = _container.Resolve<IMenuBarPresenter>();
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
        }
    }
}
