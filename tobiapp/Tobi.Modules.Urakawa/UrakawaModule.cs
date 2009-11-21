using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;

namespace Tobi.Modules.Urakawa
{
    ///<summary>
    /// The active Urakawa SDK Project (and its Presentation) is hosted by a unique "session" instance.
    /// This plugin bootstrapper configures 
    ///</summary>
    [Export(typeof(ITobiModule)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class UrakawaModule : ITobiModule, IPartImportsSatisfiedNotification
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
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IToolBarsView m_ToolBarsView;

#pragma warning restore 649

        private readonly ILoggerFacade m_Logger;

        private readonly UrakawaSession m_UrakawaSession;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="urakawaSession">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public UrakawaModule(
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            UrakawaSession urakawaSession)
        {
            m_Logger = logger;

            m_UrakawaSession = urakawaSession;

            m_Logger.Log(@"Urakawa module is initializing...", Category.Debug, Priority.Medium);
        }

        private int m_ToolBarId_1;
        private int m_ToolBarId_2;
        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_UrakawaSession.OpenCommand, m_UrakawaSession.SaveCommand });
                m_ToolBarId_2 = m_ToolBarsView.AddToolBarGroup(new[] { m_UrakawaSession.UndoCommand, m_UrakawaSession.RedoCommand });

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"Urakawa session commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        public void Dispose()
        {
            if (m_ToolBarCommandsDone)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_2);

                m_ToolBarCommandsDone = false;

                m_Logger.Log(@"Urakawa session commands removed from toolbar", Category.Debug, Priority.Medium);
            }
        }

        public string Name
        {
            get { return @"Urakawa SDK session manager."; }
        }

        public string Description
        {
            get { return @"A context for opening and saving the data model of a Urakawa SDK project."; }
        }

        public Uri Home
        {
            get { return UserInterfaceStrings.TobiHomeUri; }
        }
    }
}
