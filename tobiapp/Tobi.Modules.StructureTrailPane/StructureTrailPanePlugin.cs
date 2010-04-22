using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.StructureTrailPane
{
    ///<summary>
    /// The document pane contains the text part of the multimedia presentation.
    ///</summary>
    public sealed class StructureTrailPanePlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        private readonly StructureTrailPaneView m_StructureView;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public StructureTrailPanePlugin(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(StructureTrailPaneView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            StructureTrailPaneView docView)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_UrakawaSession = session;
            m_ShellView = shellView;
            m_StructureView = docView;

            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.StructureTrailPane,
                new PreferredPositionNamedView { m_viewInstance = m_StructureView, m_viewName = @"ViewOf_" + RegionNames.StructureTrailPane });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.StructureTrailPane, typeof(StructureTrailPaneView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.StructureTrailPane];
            //targetRegion.Add(m_StructureView);
            //targetRegion.Activate(m_StructureView);

            //m_Logger.Log(@"Document pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_StructureView.CommandSwitchPhrasePrevious, m_StructureView.CommandStructureSelectDown }, PreferredPosition.Any);

            m_Logger.Log(@"Structure commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_2;
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_2 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_View, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_Focus, PreferredPosition.First, false,
                new[] { m_StructureView.CommandFocus });

            m_Logger.Log(@"Structure commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_Logger.Log(@"Structure commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Tools, m_MenuBarId_2);

                m_Logger.Log(@"Structure commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.StructureTrailPane].Deactivate(m_StructureView);
            m_RegionManager.Regions[RegionNames.StructureTrailPane].Remove(m_StructureView);
        }

        public override string Name
        {
            get { return Tobi_Plugin_StructureTrailPane_Lang.StructureTrailPanePlugin_Name; }    // TODO LOCALIZE StructureTrailPanePlugin_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_StructureTrailPane_Lang.StructureTrailPanePlugin_Description; }    // TODO LOCALIZE StructureTrailPanePlugin_Description
        }
    }
}
