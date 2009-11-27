using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.MetadataPane
{
    ///<summary>
    /// Metadata editor
    /// This plugin bootstrapper configures 
    ///</summary>
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class MetadataPanePlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
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

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;
        private readonly MetadataPaneView m_MetadataPaneView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="session">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="shellView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="view">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public MetadataPanePlugin(
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IMetadataPaneView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            MetadataPaneView view)
        {
            m_Logger = logger;
            m_UrakawaSession = session;
            m_ShellView = shellView;
            m_MetadataPaneView = view;


            CommandShowMetadataPane = new RichDelegateCommand(
                UserInterfaceStrings.ShowMetadata,
                UserInterfaceStrings.ShowMetadata_,
                UserInterfaceStrings.ShowMetadata_KEYS,
                m_ShellView.LoadTangoIcon(@"accessories-text-editor"),
                ShowDialog,
                CanShowDialog);

            m_ShellView.RegisterRichCommand(CommandShowMetadataPane);


            m_Logger.Log(@"Metadata plugin initializing...", Category.Debug, Priority.Medium);
        }

        public RichDelegateCommand CommandShowMetadataPane { get; private set; }

        private int m_ToolBarId_1;
        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { CommandShowMetadataPane }, PreferredPosition.Any);

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"Urakawa session commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private int m_MenuBarId_1;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Edit, null, new[] { CommandShowMetadataPane }, PreferredPosition.Last, true);

                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Urakawa session commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_ToolBarCommandsDone)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_ToolBarCommandsDone = false;

                m_Logger.Log(@"Urakawa session commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"Urakawa session commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        bool CanShowDialog()
        {
            return m_UrakawaSession.DocumentProject != null && m_UrakawaSession.DocumentProject.Presentations.Count > 0;
        }

        void ShowDialog()
        {
            m_MetadataPaneView.Popup();
        }

        public override string Name
        {
            get { return @"Metadata editor panel."; }
        }

        public override string Description
        {
            get { return @"The publication metadata editor"; }
        }
    }
}
