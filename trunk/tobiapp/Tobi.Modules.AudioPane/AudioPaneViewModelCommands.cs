using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;


namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Commands

        public RichDelegateCommand<object> CommandOpenFile { get; private set; }
        public RichDelegateCommand<object> CommandSwitchPhrasePrevious { get; private set; }
        public RichDelegateCommand<object> CommandSwitchPhraseNext { get; private set; }
        public RichDelegateCommand<object> CommandGotoBegining { get; private set; }
        public RichDelegateCommand<object> CommandGotoEnd { get; private set; }
        public RichDelegateCommand<object> CommandStepBack { get; private set; }
        public RichDelegateCommand<object> CommandStepForward { get; private set; }
        public RichDelegateCommand<object> CommandRewind { get; private set; }
        public RichDelegateCommand<object> CommandFastForward { get; private set; }
        public RichDelegateCommand<object> CommandSelectAll { get; private set; }
        public RichDelegateCommand<object> CommandClearSelection { get; private set; }
        public RichDelegateCommand<object> CommandZoomSelection { get; private set; }
        public RichDelegateCommand<object> CommandZoomFitFull { get; private set; }
        public RichDelegateCommand<object> CommandRefresh { get; private set; }
        public RichDelegateCommand<object> CommandAutoPlay { get; private set; }
        public RichDelegateCommand<object> CommandPlay { get; private set; }
        public RichDelegateCommand<object> CommandPause { get; private set; }
        public RichDelegateCommand<object> CommandStartRecord { get; private set; }
        public RichDelegateCommand<object> CommandStopRecord { get; private set; }

        private void initializeCommands()
        {
            Logger.Log("AudioPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            CommandOpenFile = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_OpenFile,
                new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift),
                (VisualBrush)Application.Current.FindResource("document-open"),
                obj => OpenFile(obj as String), obj => true);

            shellPresenter.RegisterRichCommand(CommandOpenFile);
            //
            CommandSwitchPhrasePrevious = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_SwitchPrevious,
                new KeyGesture(Key.Down, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("go-first"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithTreeNode);

            shellPresenter.RegisterRichCommand(CommandSwitchPhrasePrevious);
            //
            CommandSwitchPhraseNext = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_SwitchNext,
                new KeyGesture(Key.Up, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("go-last"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithTreeNode);

            shellPresenter.RegisterRichCommand(CommandSwitchPhraseNext);
            //
            CommandGotoBegining = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_GotoBegin,
                new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt),
                (VisualBrush)Application.Current.FindResource("go-previous"),
                obj => AudioPlayer_GotoBegining(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_GotoEnd,
                new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt),
                (VisualBrush)Application.Current.FindResource("go-next"),
                obj => AudioPlayer_GotoEnd(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand<object>(null,
                 UserInterfaceStrings.Audio_StepBack,
                new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift),
                (VisualBrush)Application.Current.FindResource("media-skip-backward"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithSubTreeNodes);

            shellPresenter.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_StepForward,
                new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift),
                (VisualBrush)Application.Current.FindResource("media-skip-forward"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithSubTreeNodes);

            shellPresenter.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_FastForward,
                new KeyGesture(Key.Right, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("media-seek-forward"),
                obj => AudioPlayer_FastForward(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_Rewind,
                new KeyGesture(Key.Left, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("media-seek-backward"),
                obj => AudioPlayer_Rewind(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandRewind);
            //
            CommandSelectAll = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.SelectAll,
                new KeyGesture(Key.A, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("view-fullscreen"),
                obj => SelectAll(), obj => true);

            shellPresenter.RegisterRichCommand(CommandSelectAll);
            //
            CommandClearSelection = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_ClearSelection,
                new KeyGesture(Key.D, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("edit-clear"),
                obj => ClearSelection(), obj => IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandClearSelection);
            //
            CommandZoomSelection = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_ZoomSelection,
                new KeyGesture(Key.W, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("system-search"),
                obj => ZoomSelection(), obj => IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandZoomSelection);
            //
            CommandZoomFitFull = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_FitFull,
                new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift),
                (VisualBrush)Application.Current.FindResource("utilities-system-monitor"),
                obj => ZoomFitFull(), obj => true);

            shellPresenter.RegisterRichCommand(CommandZoomFitFull);
            //
            CommandRefresh = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_Reload,
                null,
                (VisualBrush)Application.Current.FindResource("view-refresh"),
                obj => Refresh(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandRefresh);
            //
            CommandAutoPlay = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_AutoPlay,
                new KeyGesture(Key.Y, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("go-jump"),
                obj => { }, obj => true);

            shellPresenter.RegisterRichCommand(CommandAutoPlay);
            //
            //
            CommandPlay = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_Play,
                new KeyGesture(Key.Enter, ModifierKeys.Control),
                (VisualBrush)Application.Current.FindResource("media-playback-start"),
                obj => AudioPlayer_PlayPause(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandPlay);
            //
            CommandPause = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_Pause,
                null,
                (VisualBrush)Application.Current.FindResource("media-playback-pause"),
                obj => AudioPlayer_Pause(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandPause);
            //
            CommandStartRecord = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_StartRecord,
                new KeyGesture(Key.Enter, ModifierKeys.Control | ModifierKeys.Shift),
                (VisualBrush)Application.Current.FindResource("media-record"),
                obj => AudioRecorder_StartStop(), obj => true);

            shellPresenter.RegisterRichCommand(CommandStartRecord);
            //
            CommandStopRecord = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_StopRecord,
                null,
                (VisualBrush)Application.Current.FindResource("media-playback-stop"),
                obj => AudioRecorder_Stop(), obj => true);

            shellPresenter.RegisterRichCommand(CommandStopRecord);

            if (View != null)
            {
                View.InitGraphicalCommandBindings();
            }

        }

        #endregion Commands
    }
}
