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
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class NavigationPanePlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            // If the toolbar has been resolved, we can push our commands into it.
            tryToolbarCommands();

            // If the menubar has been resolved, we can push our commands into it.
            tryMenubarCommands();
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IToolBarsView m_ToolBarsView;

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IMenuBarView m_MenuBarView;

#pragma warning restore 649

        
        private readonly ILoggerFacade m_Logger;
        private readonly IRegionManager m_RegionManager;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        private readonly NavigationPane m_NavPane;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
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

            m_Logger.Log(@"Navigation pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        //private int m_ToolBarId_1;
        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_NavPane.CommandFocus });

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"Navigation commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private int m_MenuBarId_1;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_View, RegionNames.MenuBar_Focus, new[] { m_NavPane.CommandFocus }, PreferredPosition.Last, false);

                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Navigation commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_ToolBarCommandsDone)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_ToolBarCommandsDone = false;

                m_Logger.Log(@"Navigation commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Focus, m_MenuBarId_1);

                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"Navigation commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.NavigationPane].Deactivate(m_NavPane);
            m_RegionManager.Regions[RegionNames.NavigationPane].Remove(m_NavPane);
        }

        public override string Name
        {
            get { return @"Navigation pane."; }
        }

        public override string Description
        {
            get { return @"The Navigation panel"; }
        }
    }
}
