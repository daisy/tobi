using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Tobi.Infrastructure;
using urakawa;

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
        RichDelegateCommand<object> ExitCommand { get; }

        ///<summary>
        /// Adds a <see cref="KeyBinding"/> to the Shell window
        ///</summary>
        bool AddInputBinding(InputBinding inputBinding);

        ///<summary>
        /// The shell window is closing, returns true if application is shutting down
        ///</summary>
        bool OnShellWindowClosing();

        void ToggleView(bool? show, IToggableView view);

        //TODO: the methods below are not part of the final design, they're here just to compile and run the experiments.
        void ProjectLoaded(Project proj);
        void PageEncountered(TextElement textElement);
    }
}
