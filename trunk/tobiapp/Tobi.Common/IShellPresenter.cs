using System;
using System.Windows.Media;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    ///<summary>
    /// The contract for the Presenter
    ///</summary>
    public interface IShellPresenter : IInputBindingManager
    {
        RichDelegateCommand<object> ExitCommand { get; }
        RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; }
        RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; }
        RichDelegateCommand<object> ManageShortcutsCommand { get; }

        RichDelegateCommand<object> CopyCommand { get; }
        RichDelegateCommand<object> CutCommand { get; }
        RichDelegateCommand<object> PasteCommand { get; }

        RichDelegateCommand<object> HelpCommand { get; }
        RichDelegateCommand<object> PreferencesCommand { get; }
        //RichDelegateCommand<object> WebHomeCommand { get; }

        //RichDelegateCommand<object> NavNextCommand { get; }
        //RichDelegateCommand<object> NavPreviousCommand { get; }

        IShellView View { get; }

        void PlayAudioCueTock();
        void PlayAudioCueTockTock();

        void RegisterRichCommand(RichDelegateCommand<object> command);

        VisualBrush LoadTangoIcon(string resourceKey);
        VisualBrush LoadGnomeNeuIcon(string resourceKey);
        VisualBrush LoadGnomeGionIcon(string resourceKey);
        VisualBrush LoadGnomeFoxtrotIcon(string resourceKey);

        void DimBackgroundWhile(Action action);

        // TODO: The methods below are only called by the view,
        // we should perharps inject the Presenter into the View instead.
        bool OnShellWindowClosing();
        void OnMagnificationLevelChanged(double value);

    }
}
