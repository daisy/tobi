using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi
{
    [Export(typeof(IGlobalSearchCommands)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class GlobalSearchCommands : IGlobalSearchCommands, IPartImportsSatisfiedNotification  
    {
        private readonly ILoggerFacade m_Logger;
        private readonly IShellView m_ShellView;

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IToolBarsView m_ToolBarsView;

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IMenuBarView m_MenuBarView;

#pragma warning restore 649

        
        [ImportingConstructor]
        public GlobalSearchCommands(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view)
        {
            m_Logger = logger;
            m_ShellView = view;
            InitializeCommands();

#if DEBUG
            m_Logger.Log("Global Search Commands Created", Category.Debug, Priority.None);
#endif
        }

        public RichCompositeCommand CmdFindFocus { get; private set; }
        public RichCompositeCommand CmdFindNext { get; private set; }
        public RichCompositeCommand CmdFindPrevious { get; private set; }

        private void InitializeCommands()
        {
            CmdFindFocus = new RichCompositeCommand(Tobi_Lang.Find, //TODO: localize Find
                "Edit the search criteria.",
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("edit-find"),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Find));

            m_ShellView.RegisterRichCommand(CmdFindFocus);

            CmdFindNext = new RichCompositeCommand(Tobi_Lang.FindNext, //TODO: localize FindNext
                "Find The Next Item That Matches The Search Criteria.",
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("format-indent-more"),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_FindNext));

            m_ShellView.RegisterRichCommand(CmdFindNext);

            CmdFindPrevious = new RichCompositeCommand(Tobi_Lang.FindPrevious, //TODO: localize FindPrevious
                "Find The Previous Item That Matches The Search Criteria.",
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("format-indent-less"),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_FindPrevious));

            m_ShellView.RegisterRichCommand(CmdFindPrevious);
        }

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

        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                //int uid1 = m_ToolBarsView.AddToolBarGroup(new[] { CmdFindPrevious, CmdFindNext }, PreferredPosition.Last);

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"Search commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                int uid1 = m_MenuBarView.AddMenuBarGroup(
                    RegionNames.MenuBar_Tools, PreferredPosition.First, true,
                    RegionNames.MenuBar_Find, PreferredPosition.First, true,
                    new[] { CmdFindFocus, CmdFindPrevious, CmdFindNext });

                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Search commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }
    }
}
