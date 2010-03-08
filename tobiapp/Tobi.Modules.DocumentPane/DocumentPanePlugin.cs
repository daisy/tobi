using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.DocumentPane
{
    ///<summary>
    /// The document pane contains the text part of the multimedia presentation.
    ///</summary>
    public sealed class DocumentPanePlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        private readonly DocumentPaneView m_DocView;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
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

            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.DocumentPane,
                new PreferredPositionNamedView { m_viewInstance = m_DocView, m_viewName = @"ViewOf_" + RegionNames.DocumentPane });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.DocumentPane, typeof(DocumentPaneView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.DocumentPane];
            //targetRegion.Add(m_DocView);
            //targetRegion.Activate(m_DocView);

            //m_Logger.Log(@"Document pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_DocView.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext }, PreferredPosition.Any);

            m_Logger.Log(@"Document commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_Structure, PreferredPosition.First, true,
                RegionNames.MenuBar_Navigation, PreferredPosition.First, true,
                new[] { m_DocView.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext });

            m_Logger.Log(@"Document commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_Logger.Log(@"Document commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_Logger.Log(@"Document commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.DocumentPane].Deactivate(m_DocView);
            m_RegionManager.Regions[RegionNames.DocumentPane].Remove(m_DocView);
        }

        public override string Name
        {
            get { return Tobi_Plugin_DocumentPane_Lang.DocumentPane; }      // TODO: LOCALIZE DocumentPane
        }

        public override string Description
        {
            get { return Tobi_Plugin_DocumentPane_Lang.PublicationDocumentViewer; }    // TODO: LOCALIZE PublicationDocumentViewer
        }
    }
}
