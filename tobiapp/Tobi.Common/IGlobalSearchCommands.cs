using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IGlobalSearchCommands
    {
        RichCompositeCommand CmdFindFocus { get; }
        RichCompositeCommand CmdFindNext { get; }
        RichCompositeCommand CmdFindPrevious { get; }
    }
}
