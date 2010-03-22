using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;

namespace Tobi.Plugin.MenuBarDebug
{
    public sealed class MenuBarDebugPlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly ILoggerFacade m_Logger;

        [ImportingConstructor]
        public MenuBarDebugPlugin(
            ILoggerFacade logger,
            IRegionManager regionManager
            //,[Import(typeof(IMenuBarView), AllowRecomposition = true, RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            //MenuBarDebugView menuBarsView
            )
        {
            Debugger.Break();

            m_Logger = logger;
            m_RegionManager = regionManager;

            //m_MenuBarView = menuBarsView;

            //m_RegionManager.RegisterNamedViewWithRegion(RegionNames.MenuBar,
            //    new PreferredPositionNamedView { m_viewInstance = m_MenuBarView, m_viewName = @"ViewOf_" + RegionNames.MenuBar });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.MenuBar, typeof(IMenuBarView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.MenuBar];
            //targetRegion.Add(m_MenuBarView);
            //targetRegion.Activate(m_MenuBarView);

            //m_Logger.Log(@"MenuBar pushed to region", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            //m_RegionManager.Regions[RegionNames.MenuBar].Deactivate(m_MenuBarView);
            //m_RegionManager.Regions[RegionNames.MenuBar].Remove(m_MenuBarView);

            //m_Logger.Log(@"MenuBarDebug removed from region", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return "MENU DEBUG"; }
        }

        public override string Description
        {
            get { return "MENU DEBUG _"; }
        }
    }
}
