﻿using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;

namespace Tobi.Plugin.ToolBars
{
    ///<summary>
    /// The tool bar contains groups of buttons (commands actually) from various parts of the application.
    /// (i.e. it is a host service, it doesn't own command data directly)
    ///</summary>
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ToolBarsPlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            // If the menubar has been resolved, we can push our commands into it.
            tryMenubarCommands();
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IMenuBarView m_MenuBarView;

#pragma warning restore 649

        private readonly ILoggerFacade m_Logger;
        private readonly IRegionManager m_RegionManager;

        private readonly ToolBarsView m_ToolBarsView;

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
            IRegionManager regionManager,
            [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            ToolBarsView toolBarsView)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_ToolBarsView = toolBarsView;

            m_RegionManager.RegisterViewWithRegion(RegionNames.ToolBars, typeof(IToolBarsView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.ToolBars];
            //targetRegion.Add(m_ToolBarsView);
            //targetRegion.Activate(m_ToolBarsView);

            m_Logger.Log(@"Toolbar pushed to region", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_View, new[] { m_ToolBarsView.CommandFocus }, RegionNames.MenuBar_Focus, false);

                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Toolbar commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Focus, m_MenuBarId_1);

                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"Toolbar commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.ToolBars].Deactivate(m_ToolBarsView);
            m_RegionManager.Regions[RegionNames.ToolBars].Remove(m_ToolBarsView);

            m_Logger.Log(@"Toolbar removed from region", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return @"Application toolbar."; }
        }

        public override string Description
        {
            get { return @"The visual host for command buttons."; }
        }
    }
}
