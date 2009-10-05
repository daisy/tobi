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

        bool Close();

        RichDelegateCommand ExportCommand { get; }

        RichDelegateCommand SaveCommand { get; }
        RichDelegateCommand SaveAsCommand { get; }

        RichDelegateCommand NewCommand { get; }
        RichDelegateCommand OpenCommand { get; }
        RichDelegateCommand CloseCommand { get; }

        RichDelegateCommand UndoCommand { get; }
        RichDelegateCommand RedoCommand { get; }
    }
}
