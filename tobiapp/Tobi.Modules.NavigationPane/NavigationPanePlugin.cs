using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.NavigationPane
{
    ///<summary>
    /// The navigation center for the multimedia presentation (hierarchical table of contents, page numbers, footnotes, etc.)
    /// More or less corresponds to the NCX in DAISY3.
    ///</summary>
    public sealed class NavigationPanePlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        private readonly NavigationPane m_NavPane;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public NavigationPanePlugin(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(NavigationPane), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            NavigationPane pane)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_UrakawaSession = session;
            m_ShellView = shellView;
            m_NavPane = pane;
            
            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.NavigationPane,
                new PreferredPositionNamedView { m_viewInstance = m_NavPane, m_viewName = @"ViewOf_" + RegionNames.NavigationPane });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.NavigationPane, typeof(NavigationPane));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.NavigationPane];
            //targetRegion.Add(m_NavPane);
            //targetRegion.Activate(m_NavPane);

            //m_Logger.Log(@"Navigation pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_NavPane.CommandFocus });

            m_Logger.Log(@"Navigation commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_View, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_Focus, PreferredPosition.First, false,
                new[] { m_NavPane.CommandFocus });

            m_Logger.Log(@"Navigation commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_Logger.Log(@"Navigation commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Focus, m_MenuBarId_1);

                m_Logger.Log(@"Navigation commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.NavigationPane].Deactivate(m_NavPane);
            m_RegionManager.Regions[RegionNames.NavigationPane].Remove(m_NavPane);
        }

        public override string Name
        {
            get { return Tobi_Plugin_NavigationPane_Lang.NavigationPanePlugin_Name; }
        }

        public override string Description
        {
            get { return Tobi_Plugin_NavigationPane_Lang.NavigationPanePlugin_Description; }
        }
    }
}
