using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.MenuBar
{
    ///<summary>
    /// The menu bar contains commands from various parts of the application, organized in predefined regions.
    /// (i.e. it is a host service, it doesn't own command data directly)
    ///</summary>
    public sealed class MenuBarPlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="regionManager">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        /////<param name="menuBarsView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public MenuBarPlugin(
            ILoggerFacade logger,
            IRegionManager regionManager
            //,[Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            //MenuBarView menuBarsView
            )
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            //m_MenuBarView = menuBarsView;

            
            //m_RegionManager.RegisterViewWithRegion(RegionNames.MenuBar, typeof(IMenuBarView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.MenuBar];
            //targetRegion.Add(m_MenuBarView);
            //targetRegion.Activate(m_MenuBarView);

            //m_Logger.Log(@"MenuBar pushed to region", Category.Debug, Priority.Medium);
        }

        protected override void OnMenuBarReady()
        {
            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.MenuBar,
                   new PreferredPositionNamedView { m_viewInstance = m_MenuBarView, m_viewName = @"ViewOf_" + RegionNames.MenuBar });
        }

        public override void Dispose()
        {
            m_RegionManager.Regions[RegionNames.MenuBar].Deactivate(m_MenuBarView);
            m_RegionManager.Regions[RegionNames.MenuBar].Remove(m_MenuBarView);

            m_Logger.Log(@"MenuBar removed from region", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return Tobi_Plugin_MenuBar_Lang.ApplicationMenubar; }      // TODO LOCALIZE ApplicationMenubar
        }

        public override string Description
        {
            get { return Tobi_Plugin_MenuBar_Lang.MenuContainingAppCommands; }     // TODO LOCALIZE  MenuContainingAppCommands
        }
    }
}
