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
        private readonly IRegionManager m_regionManager;
        private readonly IUnityContainer m_container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public MenuBarModule(IUnityContainer container, IRegionManager regionManager)
        {
            m_regionManager = regionManager;
            m_container = container;
        }

        ///<summary>
        /// Registers the <see cref="MenuBarView"/> into the Dependecy Container
        /// and injects it inside the '<c>MenuBar</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_container.RegisterType<MenuBarView>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = m_regionManager.Regions[RegionNames.MenuBar];
            var view = m_container.Resolve<MenuBarView>();

            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
