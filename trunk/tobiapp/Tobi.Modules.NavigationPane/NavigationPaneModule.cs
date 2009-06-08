using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Modules.DocumentPane;

namespace Tobi.Modules.NavigationPane
{
    ///<summary>
    /// The navigation center for the multimedia presentation (hierarchical table of contents, page numbers, footnotes, etc.)
    /// More or less corresponds to the NCX in DAISY3.
    ///</summary>
    /// [ModuleDependency("DocumentPaneModule")] ALREADY CONFIGURED IN THE MODULE CATALOG
    public class NavigationPaneModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public NavigationPaneModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="NavigationPaneView"/> in the DI container as a singleton
        /// and injects it inside the '<c>NavigationPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<NavigationPaneView>(new ContainerControlledLifetimeManager());

            var regionManager = m_Container.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.NavigationPane, typeof(NavigationPaneView));

            var view = m_Container.Resolve<NavigationPaneView>();

            IRegion targetRegion = regionManager.Regions[RegionNames.NavigationPane];
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
