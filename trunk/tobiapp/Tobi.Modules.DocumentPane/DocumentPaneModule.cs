using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.DocumentPane
{
    ///<summary>
    /// The document pane contains the text part of the multimedia presentation.
    ///</summary>
    public class DocumentPaneModule : IModule
    {
        private readonly IUnityContainer m_container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public DocumentPaneModule(IUnityContainer container)
        {
            m_container = container;
        }

        ///<summary>
        /// Registers the <see cref="DocumentPaneView"/> in the DI container as a singleton
        /// and injects it inside the '<c>DocumentPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_container.RegisterType<DocumentPaneView>(new ContainerControlledLifetimeManager());

            var regionManager = m_container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.DocumentPane];

            var view = m_container.Resolve<DocumentPaneView>();
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
