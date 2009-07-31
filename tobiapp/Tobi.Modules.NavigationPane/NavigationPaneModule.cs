using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.NavigationPane
{
    ///<summary>
    /// The navigation center for the multimedia presentation (hierarchical table of contents, page numbers, footnotes, etc.)
    /// More or less corresponds to the NCX in DAISY3.
    ///</summary>
    /// [ModuleDependency("DocumentPaneModule")] ALREADY CONFIGURED IN THE MODULE CATALOG
    public class NavigationPaneModule : IModule
    {
        private readonly IUnityContainer _container;
        //private object _view;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public NavigationPaneModule(IUnityContainer container)
        {
            _container = container;
        }

        ///<summary>
        /// Registers the <see cref="NavigationPane"/> in the DI container as a singleton
        /// and injects it inside the '<c>NavigationPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            //_container.RegisterType<NavigationPaneView>(new ContainerControlledLifetimeManager());
            _container.RegisterType<NavigationPane>(new ContainerControlledLifetimeManager());

            var regionManager = _container.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.NavigationPane, typeof(NavigationPaneView));

            //var view = _container.Resolve<NavigationPaneView>();
            var view = _container.Resolve<NavigationPane>();
            IRegion targetRegion = regionManager.Regions[RegionNames.NavigationPane];
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
