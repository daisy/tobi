using System.Windows.Input;
using System.Windows.Media;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.UI;
using urakawa;

namespace Tobi
{
    ///<summary>
    /// The contract for the Presenter
    ///</summary>
    public interface IShellPresenter : IInputBindingManager
    {
        ///<summary>
        /// The View associated with this Presenter
        ///</summary>
        IShellView View { get; }

        void PlayAudioCueTock();
        void PlayAudioCueTockTock();

        RichDelegateCommand<object> ExitCommand { get; }
        RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; }
        RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; }
        RichDelegateCommand<object> ManageShortcutsCommand { get; }

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

        VisualBrush LoadTangoIcon(string resourceKey);

        // TODO: The methods below are only called by the view,
        // we should perharps inject the Presenter into the View instead.
        bool OnShellWindowClosing();
        void OnMagnificationLevelChanged(double value);
    }
}
