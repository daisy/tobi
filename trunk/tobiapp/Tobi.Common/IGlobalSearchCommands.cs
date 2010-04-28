using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IGlobalSearchCommands
    {
        RichDispatcherCommand CmdFindFocus { get; }
        RichDispatcherCommand CmdFindNext { get; }
        RichDispatcherCommand CmdFindPrevious { get; }
    }
}
