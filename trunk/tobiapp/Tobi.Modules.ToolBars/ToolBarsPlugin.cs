using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.ToolBars
{
    ///<summary>
    /// The tool bar contains groups of buttons (commands actually) from various parts of the application.
    /// (i.e. it is a host service, it doesn't own command data directly)
    ///</summary>
    public sealed class ToolBarsPlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        //private readonly ToolBarsView m_ToolBarsView;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="regionManager">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="toolBarsView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public ToolBarsPlugin(
            ILoggerFacade logger,
            IRegionManager regionManager
            //,[Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            //ToolBarsView toolBarsView
            )
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            //m_ToolBarsView = toolBarsView;

            //m_RegionManager.RegisterViewWithRegion(RegionNames.ToolBars, typeof(IToolBarsView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.ToolBars];
            //targetRegion.Add(m_ToolBarsView);
            //targetRegion.Activate(m_ToolBarsView);

            //m_Logger.Log(@"Toolbar pushed to region", Category.Debug, Priority.Medium);
        }

        protected override void OnToolBarReady()
        {
            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.ToolBars,
                   new PreferredPositionNamedView { m_viewInstance = m_ToolBarsView, m_viewName = @"ViewOf_" + RegionNames.ToolBars });
        }

        private int m_ToolBarId_1;
        protected override void OnMenuBarReady()
        {
            m_ToolBarId_1 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_View, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_Focus, PreferredPosition.Any, false,
                new[] { m_ToolBarsView.CommandFocus });

            m_Logger.Log(@"Toolbar commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Focus, m_ToolBarId_1);

                m_Logger.Log(@"Toolbar commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.ToolBars].Deactivate(m_ToolBarsView);
            m_RegionManager.Regions[RegionNames.ToolBars].Remove(m_ToolBarsView);

            m_Logger.Log(@"Toolbar removed from region", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return Tobi_Plugin_ToolBars_Lang.ToolbarsPlugin_Name; }     // TODO LOCALIZE ToolbarsPlugin_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_ToolBars_Lang.ToolbarsPlugin_Description; }   // TODO LOCALIZE ToolbarsPlugin_Description
        }
    }
}
