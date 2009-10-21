using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IToolBarsView
    {
        int AddToolBarGroup(RichDelegateCommand[] commands);
        void RemoveToolBarGroup(int uid);
    }
}
