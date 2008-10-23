using System;
using Microsoft.Practices.Composite.Events;
using Tobi.Modules.StatusBar.Controllers;
using Tobi.Modules.StatusBar.PresentationModels;

namespace Tobi.Modules.StatusBar.Views
{

    public class StatusBarPresenter
    {
        private IStatusBarView m_view;
        private IStatusBarController m_controller;

        public StatusBarPresenter(IStatusBarView view, IStatusBarController controller)
        {
            m_view = view;
            m_controller = controller;

            m_view.Model.StatusBarDataChanged += new EventHandler<DataEventArgs<StatusBarData>>(OnStatusBarDataChanged);
        }
        public IStatusBarView View
        {
            get { return m_view; }
            // set { m_view = value; }
        }

        private void OnStatusBarDataChanged(object sender, DataEventArgs<StatusBarData> e)
        {
            m_controller.OnStatusBarDataChanged(e.Value);
        }
    }
}
