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
            CommandOpenFile = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_OpenFile,
                UserInterfaceStrings.Audio_OpenFile_,
                UserInterfaceStrings.Audio_OpenFile_KEYS,
                (VisualBrush)Application.Current.FindResource("document-open"),
                obj => OpenFile(obj as String), obj => true);

            shellPresenter.RegisterRichCommand(CommandOpenFile);
            //
            CommandSwitchPhrasePrevious = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_SwitchPrevious,
                UserInterfaceStrings.Audio_SwitchPrevious_,
                UserInterfaceStrings.Audio_SwitchPrevious_KEYS,
                (VisualBrush)Application.Current.FindResource("go-first"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithTreeNode);

            shellPresenter.RegisterRichCommand(CommandSwitchPhrasePrevious);
            //
            CommandSwitchPhraseNext = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_SwitchNext,
                UserInterfaceStrings.Audio_SwitchNext_,
                UserInterfaceStrings.Audio_SwitchNext_KEYS,
                (VisualBrush)Application.Current.FindResource("go-last"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithTreeNode);

            shellPresenter.RegisterRichCommand(CommandSwitchPhraseNext);
            //
            CommandGotoBegining = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_GotoBegin,
                UserInterfaceStrings.Audio_GotoBegin_,
                UserInterfaceStrings.Audio_GotoBegin_KEYS,
                (VisualBrush)Application.Current.FindResource("go-previous"),
                obj => AudioPlayer_GotoBegining(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_GotoEnd,
                UserInterfaceStrings.Audio_GotoEnd_,
                UserInterfaceStrings.Audio_GotoEnd_KEYS,
                (VisualBrush)Application.Current.FindResource("go-next"),
                obj => AudioPlayer_GotoEnd(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StepBack,
                 UserInterfaceStrings.Audio_StepBack_,
                 UserInterfaceStrings.Audio_StepBack_KEYS,
                (VisualBrush)Application.Current.FindResource("media-skip-backward"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithSubTreeNodes);

            shellPresenter.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StepForward,
                UserInterfaceStrings.Audio_StepForward_,
                UserInterfaceStrings.Audio_StepForward_KEYS,
                (VisualBrush)Application.Current.FindResource("media-skip-forward"),
                obj => AudioPlayer_Stop(), obj => IsAudioLoadedWithSubTreeNodes);

            shellPresenter.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_FastForward,
                UserInterfaceStrings.Audio_FastForward_,
                UserInterfaceStrings.Audio_FastForward_KEYS,
                (VisualBrush)Application.Current.FindResource("media-seek-forward"),
                obj => AudioPlayer_FastForward(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Rewind,
                UserInterfaceStrings.Audio_Rewind_,
                UserInterfaceStrings.Audio_Rewind_KEYS,
                (VisualBrush)Application.Current.FindResource("media-seek-backward"),
                obj => AudioPlayer_Rewind(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandRewind);
            //
            CommandSelectAll = new RichDelegateCommand<object>(UserInterfaceStrings.SelectAll,
                UserInterfaceStrings.SelectAll_,
                UserInterfaceStrings.SelectAll_KEYS,
                (VisualBrush)Application.Current.FindResource("view-fullscreen"),
                obj => SelectAll(), obj => true);

            shellPresenter.RegisterRichCommand(CommandSelectAll);
            //
            CommandClearSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_ClearSelection,
                UserInterfaceStrings.Audio_ClearSelection_,
                UserInterfaceStrings.Audio_ClearSelection_KEYS,
                (VisualBrush)Application.Current.FindResource("edit-clear"),
                obj => ClearSelection(), obj => IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandClearSelection);
            //
            CommandZoomSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_ZoomSelection,
                UserInterfaceStrings.Audio_ZoomSelection_,
                UserInterfaceStrings.Audio_ZoomSelection_KEYS,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Search")),
                //(VisualBrush)Application.Current.FindResource("system-search"),
                obj => ZoomSelection(), obj => IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandZoomSelection);
            //
            CommandZoomFitFull = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_FitFull,
                UserInterfaceStrings.Audio_FitFull_,
                UserInterfaceStrings.Audio_FitFull_KEYS,
                (VisualBrush)Application.Current.FindResource("utilities-system-monitor"),
                obj => ZoomFitFull(), obj => true);

            shellPresenter.RegisterRichCommand(CommandZoomFitFull);
            //
            CommandRefresh = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Reload,
                UserInterfaceStrings.Audio_Reload_,
                null,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Refresh")),
                //(VisualBrush)Application.Current.FindResource("view-refresh"),
                obj => Refresh(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandRefresh);
            //
            CommandAutoPlay = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_AutoPlay,
                UserInterfaceStrings.Audio_AutoPlay_,
                UserInterfaceStrings.Audio_AutoPlay_KEYS,
                (VisualBrush)Application.Current.FindResource("go-jump"),
                obj => { }, obj => true);

            shellPresenter.RegisterRichCommand(CommandAutoPlay);
            //
            //
            CommandPlay = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Play,
                UserInterfaceStrings.Audio_Play_,
                UserInterfaceStrings.Audio_Play_KEYS,
                (VisualBrush)Application.Current.FindResource("media-playback-start"),
                obj => AudioPlayer_PlayPause(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandPlay);
            //
            CommandPause = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Pause,
                UserInterfaceStrings.Audio_Pause_,
                null,
                (VisualBrush)Application.Current.FindResource("media-playback-pause"),
                obj => AudioPlayer_Pause(), obj => IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandPause);
            //
            CommandStartRecord = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StartRecord,
                UserInterfaceStrings.Audio_StartRecord_,
                UserInterfaceStrings.Audio_StartRecord_KEYS,
                (VisualBrush)Application.Current.FindResource("media-record"),
                obj => AudioRecorder_StartStop(), obj => true);

            shellPresenter.RegisterRichCommand(CommandStartRecord);
            //
            CommandStopRecord = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StopRecord,
                UserInterfaceStrings.Audio_StopRecord_,
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
