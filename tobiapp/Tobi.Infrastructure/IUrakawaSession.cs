using Tobi.Infrastructure.Commanding;
using urakawa;

namespace Tobi.Infrastructure
{
    public interface IUrakawaSession
    {
        Project DocumentProject { get; }
        string DocumentFilePath { get; }

        RichDelegateCommand<object> SaveCommand { get; }
        RichDelegateCommand<object> SaveAsCommand { get; }

        RichDelegateCommand<object> NewCommand { get; }
        RichDelegateCommand<object> OpenCommand { get; }
        RichDelegateCommand<object> CloseCommand { get; }
    }
}
