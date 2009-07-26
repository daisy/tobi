using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.ToolBars
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class ToolBarsModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public ToolBarsModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="ToolBarsView"/> into the Dependency Injection container
        /// and injects it inside the '<c>ToolBars</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<ToolBarsView>(new ContainerControlledLifetimeManager());

            var regionManager = m_Container.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.ToolBars, typeof(ToolBarsView));

            var view = m_Container.Resolve<ToolBarsView>();

            IRegion targetRegion = regionManager.Regions[RegionNames.ToolBars];
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
