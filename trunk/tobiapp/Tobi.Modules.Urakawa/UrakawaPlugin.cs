using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.Urakawa
{
    ///<summary>
    /// The active Urakawa SDK Project (and its Presentation) is hosted by a unique "session" instance.
    /// This plugin bootstrapper configures 
    ///</summary>
    public sealed class UrakawaPlugin : AbstractTobiPlugin
    {
        private readonly UrakawaSession m_UrakawaSession;

        private readonly ILoggerFacade m_Logger;
        private readonly IShellView m_ShellView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="urakawaSession">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public UrakawaPlugin(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            UrakawaSession urakawaSession)
        {
            m_Logger = logger;
            m_ShellView = shellView;
            m_UrakawaSession = urakawaSession;

            //m_Logger.Log(@"Urakawa module is initializing...", Category.Debug, Priority.Medium);
        }

        private int m_ToolBarId_1;
        private int m_ToolBarId_2;
        protected override void OnToolBarReady()
        {
            m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_UrakawaSession.OpenCommand, m_UrakawaSession.SaveCommand }, PreferredPosition.First);
            m_ToolBarId_2 = m_ToolBarsView.AddToolBarGroup(new[] { m_UrakawaSession.UndoCommand, m_UrakawaSession.RedoCommand }, PreferredPosition.First);

            m_Logger.Log(@"Urakawa session commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        private int m_MenuBarId_2;
        private int m_MenuBarId_3;
        private int m_MenuBarId_4;
        private int m_MenuBarId_5;
        private int m_MenuBarId_6 = -1;
        private int m_MenuBarId_7;
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_File, PreferredPosition.First, true,
                null, PreferredPosition.First, true,
                new[] { m_UrakawaSession.OpenCommand });

            int clearID = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_File, PreferredPosition.First, false,
                RegionNames.MenuBar_OpenRecent, PreferredPosition.Last, true,
                new[] { m_UrakawaSession.ClearRecentFilesCommand });

            resetRecentFilesSubMenu();

            m_UrakawaSession.RecentFiles.CollectionChanged += (sender, e) => resetRecentFilesSubMenu();

            m_MenuBarId_2 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_File, PreferredPosition.First, true,
                null, PreferredPosition.First, true,
                new[] { m_UrakawaSession.SaveCommand, m_UrakawaSession.SaveAsCommand, m_UrakawaSession.ExportCommand });

            m_MenuBarId_3 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_File, PreferredPosition.Any, true,
                null, PreferredPosition.First, true,
                new[] { m_UrakawaSession.CloseCommand });

            m_MenuBarId_4 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_Edit, PreferredPosition.First, true,
                null, PreferredPosition.First, true,
                new[] { m_UrakawaSession.UndoCommand, m_UrakawaSession.RedoCommand });

            m_MenuBarId_5 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_Edit, PreferredPosition.Any, true,
                null, PreferredPosition.First, true,
                new[] { m_UrakawaSession.DataCleanupCommand });


            m_MenuBarId_7 = m_MenuBarView.AddMenuBarGroup(
                    RegionNames.MenuBar_Tools, PreferredPosition.Last, true,
                    RegionNames.MenuBar_System, PreferredPosition.First, false,
                    new[] { m_UrakawaSession.OpenDocumentFolderCommand });

            m_Logger.Log(@"Urakawa session commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        private void resetRecentFilesSubMenu()
        {
            if (m_MenuBarId_6 != -1)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_File, m_MenuBarId_6);
            }

            var uriOpenCmds = new List<RichDelegateCommand>();

            foreach (var uri in m_UrakawaSession.RecentFiles)
            {
                RichDelegateCommand cmd = null;
                cmd = new RichDelegateCommand(
                    (uri.IsFile ? uri.LocalPath : uri.ToString()), //uri.Scheme.ToLower() == "file"
                    uri.ToString(),
                    null, null,
                    () =>
                    {
                        try
                        {
                            m_UrakawaSession.OpenFile(cmd.LongDescription);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandler.Handle(ex, false, m_ShellView);
                        }
                    },
                    () => true,
                    null, null
                    );
                uriOpenCmds.Add(cmd);
            }

            uriOpenCmds.Reverse();

            //uriOpenCmds.Add(clearCmd);

            m_MenuBarId_6 = m_MenuBarView.AddMenuBarGroup(
                RegionNames.MenuBar_File, PreferredPosition.First, true,
                RegionNames.MenuBar_OpenRecent, PreferredPosition.First, true,
                uriOpenCmds.ToArray());
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_2);

                m_Logger.Log(@"Urakawa session commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_File, m_MenuBarId_1);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_File, m_MenuBarId_2);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_File, m_MenuBarId_3);

                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Edit, m_MenuBarId_4);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Edit, m_MenuBarId_5);

                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Edit, m_MenuBarId_6);

                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_System, m_MenuBarId_7);

                m_Logger.Log(@"Urakawa session commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return Tobi_Plugin_Urakawa_Lang.UrakawaSDKSessionManager; }    // TODO LOCALIZE UrakawaSDKSessionManager
        }

        public override string Description
        {
            get { return Tobi_Plugin_Urakawa_Lang.ContextToOpenSaveDataModelOfSDKProject; }    // TODO LOCALIZE ContextToOpenSaveDataModelOfSDKProject
        }
    }
}
