using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.StatusBar
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class StatusBarModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public StatusBarModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="StatusBarView"/> in the Dependency Injection container
        /// and injects it inside the '<c>StatusBar</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<StatusBarView>(new ContainerControlledLifetimeManager());

            var regionManager = m_Container.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.StatusBar, typeof(StatusBarView));

            var view = m_Container.Resolve<StatusBarView>();

            IRegion targetRegion = regionManager.Regions[RegionNames.StatusBar];
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
