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
        RichDelegateCommand ExitCommand { get; }
        RichDelegateCommand MagnifyUiIncreaseCommand { get; }
        RichDelegateCommand MagnifyUiDecreaseCommand { get; }
        RichDelegateCommand ManageShortcutsCommand { get; }

        RichDelegateCommand CopyCommand { get; }
        RichDelegateCommand CutCommand { get; }
        RichDelegateCommand PasteCommand { get; }

        RichDelegateCommand HelpCommand { get; }
        RichDelegateCommand PreferencesCommand { get; }
        //RichDelegateCommand WebHomeCommand { get; }

        //RichDelegateCommand NavNextCommand { get; }
        //RichDelegateCommand NavPreviousCommand { get; }

        IShellView View { get; }

        void PlayAudioCueHi();
        void PlayAudioCueTock();
        void PlayAudioCueTockTock();

        void RegisterRichCommand(RichDelegateCommand command);

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
