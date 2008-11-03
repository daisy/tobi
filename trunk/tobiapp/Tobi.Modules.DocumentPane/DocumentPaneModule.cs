using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.DocumentPane
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class DocumentPaneModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public DocumentPaneModule(IUnityContainer container, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _container = container;
        }

        ///<summary>
        /// Registers implementations for <see cref="IDocumentPaneView"/> (
        /// <see cref="DocumentPaneView"/>), <see cref="IDocumentPaneService"/> (
        /// <see cref="DocumentPaneService"/>) and <see cref="IDocumentPanePresenter"/> (
        /// <see cref="DocumentPanePresenter"/>). Creates a <see cref="DocumentPanePresenter"/> and
        /// injects its associated <see cref="DocumentPaneView"/> inside the '<c>DocumentPane</c>'
        /// region.
        ///</summary>
        public void Initialize()
        {
            // TODO: should be a singleton;
            _container.RegisterType<IDocumentPaneService, DocumentPaneService>(new ContainerControlledLifetimeManager());

            _container.RegisterType<IDocumentPaneView, DocumentPaneView>();
            _container.RegisterType<IDocumentPanePresenter, DocumentPanePresenter>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.DocumentPane];
            var presenter = _container.Resolve<IDocumentPanePresenter>();
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
        }
    }
}
