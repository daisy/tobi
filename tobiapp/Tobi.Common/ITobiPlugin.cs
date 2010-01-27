using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;

namespace Tobi.Common
{
    /// <summary>
    /// Acts as an intermediate between a plugin and the host application,
    /// by wiring things together, such as establishing region mapping in the user-interface.
    /// </summary>
    public interface ITobiPlugin : IDisposable
    {
        ///<summary>
        /// A short name for the plugin
        ///</summary>
        string Name { get; }

        ///<summary>
        /// A single-line description of the plugin's role
        ///</summary>
        string Description { get; }

        ///<summary>
        /// This points to the "home page" of the plugin (i.e. where updates can be found).
        ///</summary>
        Uri Home { get; }

        ///<summary>
        /// The current plugin version
        ///</summary>
        string Version { get; }
    }

    ///<summary>
    /// Default implementation for Version and Home.
    ///</summary>
    [InheritedExport(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public class AbstractTobiPlugin : ITobiPlugin, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public virtual void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            tryToolbar();

            tryMenubar();
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        protected IToolBarsView m_ToolBarsView;

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        protected IMenuBarView m_MenuBarView;

#pragma warning restore 649

        private bool m_ToolBarDone;
        private void tryToolbar()
        {
            if (!m_ToolBarDone && m_ToolBarsView != null)
            {
                m_ToolBarDone = true;
                OnToolBarReady();
            }
        }

        protected virtual void OnToolBarReady()
        {
            ;
        }

        private bool m_MenuBarDone;
        private void tryMenubar()
        {
            if (!m_MenuBarDone && m_MenuBarView != null)
            {
                m_MenuBarDone = true;
                OnMenuBarReady();
            }
        }

        protected virtual void OnMenuBarReady()
        {
            ;
        }

        public virtual string Version
        {
            get { return UserInterfaceStrings.APP_VERSION; }
        }

        public virtual Uri Home
        {
            get { return UserInterfaceStrings.TobiHomeUri; }
        }

        public virtual string Name { private set;  get; }
        public virtual string Description { private set; get; }

        public virtual void Dispose()
        {
            ;            
        }
    }
}
