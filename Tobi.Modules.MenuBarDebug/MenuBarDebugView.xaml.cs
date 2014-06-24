using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Plugin.MenuBar;

namespace Tobi.Plugin.MenuBarDebug
{
    [Export(typeof(IMenuBarView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MenuBarDebugView : IMenuBarView
    {
        private readonly MenuBarView m_MenuBarView;

        [ImportingConstructor]
        public MenuBarDebugView(
            ILoggerFacade logger,
            IRegionManager regionManager)
        {
#if DEBUG
            Debugger.Break();
#endif

            PreferredPositionRegion.MARK_PREFERRED_POS = true;

            InitializeComponent();

            m_MenuBarView = new MenuBarView(logger, regionManager);
            Content = m_MenuBarView;
        }

        public int AddMenuBarGroup(string topLevelMenuItemId, PreferredPosition positionInTopLevel, bool addSeparatorTopLevel, string subMenuItemId, PreferredPosition positionInSubLevel, bool addSeparatorSubLevel, object[] commands)
        {
            return m_MenuBarView.AddMenuBarGroup(topLevelMenuItemId, positionInTopLevel, addSeparatorTopLevel, subMenuItemId,
                                          positionInSubLevel, addSeparatorSubLevel, commands);
        }

        public void RemoveMenuBarGroup(string region, int uid)
        {
            m_MenuBarView.RemoveMenuBarGroup(region, uid);
        }
    }
}
