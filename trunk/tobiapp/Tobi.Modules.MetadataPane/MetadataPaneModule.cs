using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.MetadataPane
{
    /// <summary>
    /// The metadata pane offers a viewer / editor for the Urakawa SDK data model's metadata.
    /// </summary>
    public class MetadataPaneModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public MetadataPaneModule(IUnityContainer container)
        {
            m_Container = container;
        }

        public void Initialize()
        {
            m_Container.RegisterType<MetadataPaneViewModel>(new ContainerControlledLifetimeManager());
            m_Container.RegisterType<IMetadataPaneView, MetadataPaneView>(new ContainerControlledLifetimeManager());

            /*
             * The popup window (modal or not) that contains the metadata editor does not provide a region.
             * It could, but it's not necessary here.
             * 
            var regionManager = m_Container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.MetadataPane];

            var view = m_Container.Resolve<MetadataPaneView>();
            targetRegion.Add(view);
            targetRegion.Activate(view);
             * */
        }
    }
}
