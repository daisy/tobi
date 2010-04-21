using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using Tobi.Plugin.Validator.Metadata;

namespace Tobi.Plugin.MetadataPane
{
    ///<summary>
    /// Metadata editor
    /// This plugin bootstrapper configures 
    ///</summary>
    public sealed class MetadataPanePlugin : AbstractTobiPlugin
    {
        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;
        private readonly MetadataPaneView m_MetadataPaneView;
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;
        
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
            IEventAggregator eventAggregator,
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
            m_EventAggregator = eventAggregator;

            CommandShowMetadataPane = new RichDelegateCommand(
                Tobi_Plugin_MetadataPane_Lang.CmdShowMetadata_ShortDesc,
                Tobi_Plugin_MetadataPane_Lang.CmdShowMetadata_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"accessories-text-editor"),
                ShowDialog,
                CanShowDialog,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Metadata_Edit));

            m_ShellView.RegisterRichCommand(CommandShowMetadataPane);

            m_EventAggregator.GetEvent<LaunchMetadataEditorEvent>().Subscribe(OnLaunchMetadataEditor, LaunchMetadataEditorEvent.THREAD_OPTION);
            

            //m_Logger.Log(@"Metadata plugin initializing...", Category.Debug, Priority.Medium);
        }

        public RichDelegateCommand CommandShowMetadataPane { get; private set; }

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { CommandShowMetadataPane }, PreferredPosition.Any);

            m_Logger.Log(@"Urakawa session commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        private ValidationItem m_ErrorWithFocus;

        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_Edit, PreferredPosition.Any, true,
                null, PreferredPosition.Any, true,
                new[] { CommandShowMetadataPane });

            m_Logger.Log(@"Urakawa session commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_Logger.Log(@"Urakawa session commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_Logger.Log(@"Urakawa session commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        bool CanShowDialog()
        {
            return m_UrakawaSession.DocumentProject != null;
        }

        void ShowDialog()
        {
            m_MetadataPaneView.ErrorWithFocus = m_ErrorWithFocus;
            m_MetadataPaneView.Popup();
        }
        void OnLaunchMetadataEditor(ValidationItem error)
        {
            m_ErrorWithFocus = error;
            CommandShowMetadataPane.Execute();
            
        }
        public override string Name
        {
            get { return Tobi_Plugin_MetadataPane_Lang.MetadataPanePlugin_Name; }
        }

        public override string Description
        {
            get { return Tobi_Plugin_MetadataPane_Lang.MetadataPanePlugin_Description; } 
        }
    }
}
