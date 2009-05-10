using System.Windows.Input;
using Tobi.Infrastructure.Commanding;
using urakawa;

namespace Tobi
{
    public interface IInputBindingManager
    {
        bool AddInputBinding(InputBinding inputBinding);
        void RemoveInputBinding(InputBinding inputBinding);
    }

    ///<summary>
    /// The contract for the Presenter
    ///</summary>
    public interface IShellPresenter : IInputBindingManager
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
        //bool AddInputBinding(InputBinding inputBinding);

        /// <summary>
        /// registers a command and adds the corresponding input binding to the main window
        /// </summary>
        /// <param name="command"></param>
        void RegisterRichCommand(RichDelegateCommand<object> command);
        
        string DocumentFilePath { get; }
        Project DocumentProject { get; }

        // TODO: The methods below are only called by the view,
        // we should perharps inject the Presenter into the View instead.
        bool OnShellWindowClosing();
        void SetZoomValue(double value);
    }
}
