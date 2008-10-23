
using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Modules.StatusBar.Controllers;
using Tobi.Modules.StatusBar.Views;


namespace Tobi.Modules.StatusBar
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class StatusBarModule : IModule
    {
        private readonly IRegionManager m_regionManager;
        private IUnityContainer m_container;

        private IRegion m_targetRegion;

        public StatusBarModule(IUnityContainer container, IRegionManager regionManager)
        {
            m_regionManager = regionManager;
            m_container = container;
        }

        public void Initialize()
        {
            m_container.RegisterType<IStatusBarController, StatusBarController>();
            m_container.RegisterType<IStatusBarView, StatusBarView>();

            // TODO: should be a singleton;
            //m_container.RegisterType<IStatusBarService, StatusBarService>();

            var presenter = m_container.Resolve<StatusBarPresenter>();

            m_targetRegion = m_regionManager.Regions[RegionNames.StatusBar];

            m_targetRegion.Add(presenter.View, StatusBarData.ViewName);
            m_targetRegion.Activate(presenter.View);
        }
    }
}
