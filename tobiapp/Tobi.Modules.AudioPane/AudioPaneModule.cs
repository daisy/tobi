using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.AudioPane
{
    ///<summary>
    /// The audio pane contains the interactive waveform display and editor,
    /// as well as the audio peak/vu-meter. 
    ///</summary>
    public class AudioPaneModule : IModule
    {
        private readonly IUnityContainer m_container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public AudioPaneModule(IUnityContainer container)
        {
            m_container = container;
        }

        ///<summary>
        /// Registers the <see cref="AudioPaneView"/> in the DI container as a singleton
        /// and injects it inside the '<c>AudioPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            m_container.RegisterType<AudioPaneView>(new ContainerControlledLifetimeManager());

            var regionManager = m_container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.AudioPane];

            var view = m_container.Resolve<AudioPaneView>();
            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
