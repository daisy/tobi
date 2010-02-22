using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IGlobalSearchCommands
    {
        RichCompositeCommand CmdFindNext { get; }
        RichCompositeCommand CmdFindPrevious { get; }
    }
}
