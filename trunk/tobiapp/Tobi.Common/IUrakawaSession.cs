using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa;

namespace Tobi.Common
{
    public interface IUrakawaSession : IPropertyChangedNotifyBase
    {
        Project DocumentProject { get; }
        string DocumentFilePath { get; }
        bool IsDirty { get; }

        RichDelegateCommand<object> SaveCommand { get; }
        RichDelegateCommand<object> SaveAsCommand { get; }

        RichDelegateCommand<object> NewCommand { get; }
        RichDelegateCommand<object> OpenCommand { get; }
        RichDelegateCommand<object> CloseCommand { get; }

        RichDelegateCommand<object> UndoCommand { get; }
        RichDelegateCommand<object> RedoCommand { get; }
    }
}
