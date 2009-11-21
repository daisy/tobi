using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.DocumentPane
{
    ///<summary>
    /// The document pane contains the text part of the multimedia presentation.
    ///</summary>
    public class DocumentPaneModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public DocumentPaneModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="DocumentPaneView"/> in the DI container as a singleton
        /// and injects it inside the '<c>DocumentPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<DocumentPaneView>(new ContainerControlledLifetimeManager());

            var regionManager = m_Container.Resolve<IRegionManager>();

            regionManager.RegisterViewWithRegion(RegionNames.DocumentPane, typeof(DocumentPaneView));

            //var view = m_Container.Resolve<DocumentPaneView>();

            //IRegion targetRegion = regionManager.Regions[RegionNames.DocumentPane];
            //targetRegion.Add(view);
            //targetRegion.Activate(view);
        }
    }
}
