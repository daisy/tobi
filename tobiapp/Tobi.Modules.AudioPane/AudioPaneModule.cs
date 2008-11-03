using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.AudioPane
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class AudioPaneModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        ///<param name="regionManager">The CAG-WPF region manager</param>
        public AudioPaneModule(IUnityContainer container, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _container = container;
        }

        ///<summary>
        /// Registers implementations for <see cref="IAudioPaneView"/> (
        /// <see cref="AudioPaneView"/>), <see cref="IAudioPaneService"/> (
        /// <see cref="AudioPaneService"/>) and <see cref="IAudioPanePresenter"/> (
        /// <see cref="AudioPanePresenter"/>). Creates a <see cref="AudioPanePresenter"/> and
        /// injects its associated <see cref="AudioPaneView"/> inside the '<c>AudioPane</c>'
        /// region.
        ///</summary>
        public void Initialize()
        {
            // TODO: should be a singleton;
            _container.RegisterType<IAudioPaneService, AudioPaneService>(new ContainerControlledLifetimeManager());

            _container.RegisterType<IAudioPaneView, AudioPaneView>();
            _container.RegisterType<IAudioPanePresenter, AudioPanePresenter>(new ContainerControlledLifetimeManager());

            IRegion targetRegion = _regionManager.Regions[RegionNames.AudioPane];
            var presenter = _container.Resolve<IAudioPanePresenter>();
            targetRegion.Add(presenter.View);
            targetRegion.Activate(presenter.View);
        }
    }
}
