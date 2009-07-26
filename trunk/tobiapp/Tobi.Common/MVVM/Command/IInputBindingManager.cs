using System.Windows.Input;

namespace Tobi.Common.MVVM.Command
{
    public interface IInputBindingManager
    {
        bool AddInputBinding(InputBinding inputBinding);
        void RemoveInputBinding(InputBinding inputBinding);
    }
}
