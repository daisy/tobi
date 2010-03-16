﻿using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.NavigationPane
{
    public sealed class HeadingNavigationPlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        private readonly HeadingPanelView m_TocPane;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public HeadingNavigationPlugin(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(HeadingPanelView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            HeadingPanelView pane) //HeadingPaneViewModel
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_UrakawaSession = session;
            m_ShellView = shellView;
            m_TocPane = pane;

            // Remark: using direct access instead of delayed lookup (via the region registry)
            // generates an exception, because the region does not exist yet (see "parent" plugin constructor, RegionManager.SetRegionManager(), etc.)

            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.NavigationPaneTabs,
                new PreferredPositionNamedView { m_viewInstance = m_TocPane, m_viewName = @"ViewOf_" + RegionNames.NavigationPaneTabs + @"_Headings" });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.NavigationPaneTabs, typeof(IHeadingPaneView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.NavigationPaneTabs];
            //targetRegion.Add(m_TocPane);
            //targetRegion.Activate(m_TocPane);

            //m_Logger.Log(@"Navigation pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_TocPane.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext });

            m_Logger.Log(@"Navigation commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        //private int m_MenuBarId_1;
        protected override void OnMenuBarReady()
        {
            //m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, new[] { m_TocPane.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext }, UserInterfaceStrings.Menu_Navigation);

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
                //m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_Logger.Log(@"Navigation commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.NavigationPaneTabs].Deactivate(m_TocPane);
            m_RegionManager.Regions[RegionNames.NavigationPaneTabs].Remove(m_TocPane);
        }

        public override string Name
        {
            get { return Tobi_Plugin_NavigationPane_Lang.HeadingNavigationPlugin_Name; }    // TODO LOCALIZE HeadingNavigationPlugin_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_NavigationPane_Lang.HeadingNavigationPlugin_Description; }    // TODO LOCALIZE HeadingNavigationPlugin_Description
        }
    }
}
