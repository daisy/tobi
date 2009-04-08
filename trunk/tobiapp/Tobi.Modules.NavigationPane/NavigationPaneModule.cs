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
    [ModuleDependency("DocumentPaneModule")]
    public class NavigationPaneModule : IModule
    {
        private readonly IUnityContainer m_container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="docModule">The navigation depends on the documentr</param>
        public NavigationPaneModule(IUnityContainer container)
        {
            m_container = container;
        }

        ///<summary>
        /// Registers the <see cref="NavigationPaneView"/> in the DI container as a singleton
        /// and injects it inside the '<c>NavigationPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_container.RegisterType<NavigationPaneView>(new ContainerControlledLifetimeManager());

            var regionManager = m_container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.NavigationPane];

            var view = m_container.Resolve<NavigationPaneView>();
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
