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
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public MenuBarModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="MenuBarView"/> into the Dependecy Container
        /// and injects it inside the '<c>MenuBar</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<MenuBarView>(new ContainerControlledLifetimeManager());

            var regionManager = m_Container.Resolve<IRegionManager>();
            var view = m_Container.Resolve<MenuBarView>();

            IRegion targetRegion = regionManager.Regions[RegionNames.MenuBar];
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
