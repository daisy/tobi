
using System;
using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Modules.StatusBar.PresentationModels;
using Tobi.Modules.StatusBar.Views;

namespace Tobi.Modules.StatusBar.Controllers
{
    public class StatusBarController : IStatusBarController
    {
        private readonly IRegionManager m_regionManager;
        private IUnityContainer m_container;

        public StatusBarController(IUnityContainer container, IRegionManager regionManager)
        {
            m_regionManager = regionManager;
            m_container = container;
        }

        #region Implementation of IStatusBarController

        public void OnStatusBarDataChanged(StatusBarData data)
        {
            IRegion region = m_regionManager.Regions[RegionNames.StatusBar];
            IStatusBarView view = region.GetView(StatusBarData.ViewName) as StatusBarView;
            if (view == null)
            {
                var presenter = m_container.Resolve<StatusBarPresenter>();
                view = presenter.View;
                region.Add(view);
            }
            if (view.Model == null)
            {
                var model = new StatusBarPresentationModel();
                view.Model = model;
            }
            view.Model.CurrentStatusBarData = data;
            region.Activate(view);

            Console.WriteLine(view.Text);
        }

        #endregion
    }
}
