using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Plugin.AudioPane
{
    ///<summary>
    /// The audio pane contains the interactive waveform display and editor,
    /// as well as the audio peak/vu-meter. 
    ///</summary>
    public class AudioPaneModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public AudioPaneModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="AudioPaneView"/> in the DI container as a singleton
        /// and injects it inside the '<c>AudioPane</c>' region.
        ///</summary>
        public void Initialize()
        {
            // Although we don't need an AudioPaneView or AudioPaneViewModel instance
            // globally available in the DI Container to the rest of the applicatin,
            // we need the constructors to be injected with some dependencies,
            // and it's nice to benefit from the DIC lifetime management,
            // so we do the following:
            m_Container.RegisterType<IAudioPaneView, AudioPaneView>(new ContainerControlledLifetimeManager());
            m_Container.RegisterType<AudioPaneViewModel>(new ContainerControlledLifetimeManager());

            var regionManager = m_Container.Resolve<IRegionManager>();

            //regionManager.RegisterViewWithRegion(RegionNames.AudioPane, typeof(IAudioPaneView));

            var view = m_Container.Resolve<IAudioPaneView>();
            IRegion targetRegion = regionManager.Regions[RegionNames.AudioPane];

            targetRegion.Add(view);
            targetRegion.Activate(view);
        }
    }
}
