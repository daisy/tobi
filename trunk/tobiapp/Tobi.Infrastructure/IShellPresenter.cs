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
        RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; }
        RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; }
        RichDelegateCommand<object> ManageShortcutsCommand { get; }
        RichDelegateCommand<object> SaveCommand { get; }
        RichDelegateCommand<object> SaveAsCommand { get; }

        RichDelegateCommand<object> NewCommand { get; }
        RichDelegateCommand<object> OpenCommand { get; }

        RichDelegateCommand<object> UndoCommand { get; }
        RichDelegateCommand<object> RedoCommand { get; }

        RichDelegateCommand<object> CopyCommand { get; }
        RichDelegateCommand<object> CutCommand { get; }
        RichDelegateCommand<object> PasteCommand { get; }

        RichDelegateCommand<object> HelpCommand { get; }
        RichDelegateCommand<object> PreferencesCommand { get; }
        RichDelegateCommand<object> WebHomeCommand { get; }

        RichDelegateCommand<object> NavNextCommand { get; }
        RichDelegateCommand<object> NavPreviousCommand { get; }

        ///<summary>
        /// Adds a <see cref="KeyBinding"/> to the Shell window
        ///</summary>
        bool AddInputBinding(InputBinding inputBinding);

        void RegisterRichCommand(RichDelegateCommand<object> command);

        ///<summary>
        /// The shell window is closing, returns true if application is shutting down
        ///</summary>
        bool OnShellWindowClosing();

        void ToggleView(bool? show, IToggableView view);

        //TODO: the methods below are not part of the final design, they're here just to compile and run the experiments.
        void ProjectLoaded(Project proj);
        void PageEncountered(TextElement textElement);
        void SetZoomValue(double value);
    }
}
