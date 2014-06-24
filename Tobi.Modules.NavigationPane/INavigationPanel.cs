using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tobi.Modules.NavigationPane
{
    public interface INavigationPanel
    {
        string PanelTitle { get; }
        object View { get;  }
    }
}
