using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
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

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="urakawaSession">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public UrakawaPlugin(
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            UrakawaSession urakawaSession)
        {
            m_Logger = logger;
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
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_File, null, new[] { m_UrakawaSession.OpenCommand }, PreferredPosition.First, true);
            m_MenuBarId_2 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_File, null, new[] { m_UrakawaSession.SaveCommand, m_UrakawaSession.SaveAsCommand, m_UrakawaSession.ExportCommand }, PreferredPosition.First, true);
            m_MenuBarId_3 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_File, null, new[] { m_UrakawaSession.CloseCommand }, PreferredPosition.First, true);

            m_MenuBarId_4 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Edit, null, new[] { m_UrakawaSession.UndoCommand, m_UrakawaSession.RedoCommand }, PreferredPosition.First, true);

            m_Logger.Log(@"Urakawa session commands pushed to menubar", Category.Debug, Priority.Medium);
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

                m_Logger.Log(@"Urakawa session commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return @"Urakawa SDK session manager."; }
        }

        public override string Description
        {
            get { return @"A context for opening and saving the data model of a Urakawa SDK project."; }
        }
    }
}
