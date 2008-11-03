using System.Windows.Data;
using System.Windows.Input;
using Tobi.Infrastructure;

namespace Tobi
{
    ///<summary>
    /// The contract for the Presenter
    ///</summary>
    public interface IShellPresenter
    {
        ///<summary>
        /// The View associated with this Presenter
        ///</summary>
        IShellView View { get; }

        ///<summary>
        /// Requests application exit
        ///</summary>
        DelegateCommandWithInputGesture<object> ExitCommand { get; }

        ///<summary>
        /// Adds a <see cref="KeyBinding"/> to the Shell window
        ///</summary>
        void AddInputBinding(InputBinding inputBinding);

        ///<summary>
        /// The shell window is closing, returns true if application is shutting down
        ///</summary>
        bool OnShellWindowClosing();

        void ToggleView(bool? show, IToggableView view);
    }
}
