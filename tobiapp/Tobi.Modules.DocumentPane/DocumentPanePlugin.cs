using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Plugin.DocumentPane
{
    ///<summary>
    /// The document pane contains the text part of the multimedia presentation.
    ///</summary>
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class DocumentPanePlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
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

        private readonly DocumentPaneView m_DocView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        [ImportingConstructor]
        public DocumentPanePlugin(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(DocumentPaneView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DocumentPaneView docView)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_UrakawaSession = session;
            m_ShellView = shellView;
            m_DocView = docView;
            
            m_RegionManager.RegisterViewWithRegion(RegionNames.DocumentPane, typeof(DocumentPaneView));

            m_Logger.Log(@"Document pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        private int m_ToolBarId_1;
        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_DocView.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext });

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"Document commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private int m_MenuBarId_1;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, new[] { m_DocView.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext }, UserInterfaceStrings.Menu_Navigation);

                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Document commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_ToolBarCommandsDone)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_ToolBarCommandsDone = false;

                m_Logger.Log(@"Document commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"Document commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return @"Document pane."; }
        }

        public override string Description
        {
            get { return @"The publication document viewer"; }
        }
    }
}
