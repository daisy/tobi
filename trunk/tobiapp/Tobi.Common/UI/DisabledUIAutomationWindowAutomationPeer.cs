using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Tobi.Common.UI
{
    // See http://blogs.msdn.com/jgoldb/archive/2009/12/18/wpf-performance-on-tablet-touch-enabled-machines.aspx
    // Also See: http://blogs.msdn.com/jgoldb/archive/2010/02/16/wpf-4-and-visual-studio-2010-ui-automation-performance-on-tablets-touch-enabled-machines.aspx
    // And Windows Automation API: http://support.microsoft.com/kb/971513/
    public class DisabledUIAutomationWindowAutomationPeer : WindowAutomationPeer
    {
        public DisabledUIAutomationWindowAutomationPeer(Window window)
            : base(window)
        { }

        protected override List<AutomationPeer> GetChildrenCore()
        { return null; }
    }
}
