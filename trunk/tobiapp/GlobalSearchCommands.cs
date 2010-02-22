using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.MVVM.Command;

namespace Tobi
{
    [Export(typeof(IGlobalSearchCommands)), PartCreationPolicy(CreationPolicy.Shared)]
    class GlobalSearchCommands : IGlobalSearchCommands, IPartImportsSatisfiedNotification  
    {
        private readonly ILoggerFacade m_Logger;
        private readonly IShellView m_ShellView;

        
        public RichCompositeCommand CmdFindNext { get; private set; }
        public RichCompositeCommand CmdFindPrevious { get; private set; }

        [ImportingConstructor]
        public GlobalSearchCommands(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view)
        {
            m_Logger = logger;
            m_ShellView = view;
            InitialzeCommands();
#if DEBUG
            m_Logger.Log("Global Search Commands Created", Category.Debug, Priority.None);
#endif
        }

        private void InitialzeCommands()
        {
            CmdFindNext = new RichCompositeCommand("Find Next", "Find The Next Item That Matches The Search Criteria.", new KeyGesture(Key.F3), null);
            CmdFindPrevious = new RichCompositeCommand("Find Previous", "Find The Previous Item That Matches The Search Criteria.", new KeyGesture(Key.F3), null);
            m_ShellView.RegisterRichCommand(CmdFindNext);
        }
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }
    }
}
