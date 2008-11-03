using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.UserInterfaceZoom
{
    ///<summary>
    /// This module provides a view (slider control) to zoom the application's graphical user interface in and out.
    /// It also provides associated commands and keyboard shortcuts.
    ///</summary>
    public class UserInterfaceZoomModule : IModule
    {
        ///<summary>
        /// The DI container, injected in the constructor by the DI container himself !
        ///</summary>
        protected IUnityContainer Container { get; private set; }

        ///<summary>
        /// The application Logger, injected in the constructor by the DI container.
        ///</summary>
        protected ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// The RegionManager, injected in the constructor by the DI container.
        ///</summary>
        protected IRegionManager RegionManager { get; private set; }

        ///<summary>
        /// Default DI-friendly constructor
        ///</summary>
        ///<param name="container"></param>
        ///<param name="logger"></param>
        ///<param name="regionManager"></param>
        public UserInterfaceZoomModule(IUnityContainer container, ILoggerFacade logger, IRegionManager regionManager)
        {
            Logger = logger;
            Container = container;
            RegionManager = regionManager;
        }

        ///<summary>
        /// Registers implementations for <see cref="IUserInterfaceZoomView"/> (
        /// <see cref="UserInterfaceZoomView"/>), and <see cref="IUserInterfaceZoomPresenter"/> (
        /// <see cref="UserInterfaceZoomPresenter"/>). Creates the <see cref="UserInterfaceZoomPresenter"/> singleton but does not
        /// adds+activate its associated <see cref="UserInterfaceZoomView"/> (also a singleton) inside the '<c>UserInterfaceZoom</c>'
        /// region declared in the shell. The ZoomToggleCommand must be called explicitly for this to happen.
        /// The fact that an instance of <see cref="UserInterfaceZoomPresenter"/> is created means that the command and associated keyboard shortcut is available despite the slider control not being visible.
        ///</summary>
        public void Initialize()
        {
            Container.RegisterType<IUserInterfaceZoomView, UserInterfaceZoomView>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IUserInterfaceZoomPresenter, UserInterfaceZoomPresenter>(new ContainerControlledLifetimeManager());
            var presenter = Container.Resolve<IUserInterfaceZoomPresenter>();
            /*
            IRegion targetRegion = RegionManager.Regions[RegionNames.UserInterfaceZoom];
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
            */
        }
    }
}
