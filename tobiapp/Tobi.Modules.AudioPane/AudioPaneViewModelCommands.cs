using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.Onyx.Reflection;


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
        public RichDelegateCommand<object> CommandStartMonitor { get; private set; }
        public RichDelegateCommand<object> CommandStopMonitor { get; private set; }
        public RichDelegateCommand<object> CommandBeginSelection { get; private set; }
        public RichDelegateCommand<object> CommandEndSelection { get; private set; }


        [NotifyDependsOn("IsAudioLoadedWithTreeNode")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanSwitchPhrasePrevious
        {
            get
            {
                return IsAudioLoadedWithTreeNode && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoadedWithTreeNode")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanSwitchPhraseNext
        {
            get
            {
                return IsAudioLoadedWithTreeNode && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanGotoBegining
        {
            get
            {
                return IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanGotoEnd
        {
            get
            {
                return IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsPlaying")]
        public bool CanPlay
        {
            get
            {
                return IsAudioLoaded && !IsPlaying && !IsMonitoring && !IsRecording;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsPlaying")]
        public bool CanPause
        {
            get
            {
                return IsAudioLoaded && IsPlaying;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsPlaying")]
        public bool CanRecord
        {
            get
            {
                return !IsPlaying && !IsMonitoring && !IsRecording;
            }
        }

        [NotifyDependsOn("IsRecording")]
        public bool CanStopRecord
        {
            get
            {
                return IsRecording;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanMonitor
        {
            get
            {
                return !IsPlaying && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsMonitoring")]
        public bool CanStopMonitor
        {
            get
            {
                return IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanRewind
        {
            get
            {
                return IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanFastForward
        {
            get
            {
                return IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoadedWithSubTreeNodes")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanStepBack
        {
            get
            {
                return IsAudioLoadedWithSubTreeNodes && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoadedWithSubTreeNodes")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanStepForward
        {
            get
            {
                return IsAudioLoadedWithSubTreeNodes && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanOpenFile
        {
            get
            {
                return !IsMonitoring && !IsRecording;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        public bool CanBeginSelection
        {
            get
            {
                return IsAudioLoaded;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        public bool CanEndSelection
        {
            get
            {
                return IsAudioLoaded && m_SelectionBeginTmp >= 0;
            }
        }

        public IInputBindingManager InputBindingManager
        {
            get
            {
                var shellPresenter = Container.Resolve<IShellPresenter>();

                return shellPresenter;
            }
        }

        private void initializeCommands()
        {
            Logger.Log("AudioPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            CommandOpenFile = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_OpenFile,
                UserInterfaceStrings.Audio_OpenFile_,
                UserInterfaceStrings.Audio_OpenFile_KEYS,
                (VisualBrush)Application.Current.FindResource("document-open"),
                obj => OpenFile(obj as String), obj => CanOpenFile);

            shellPresenter.RegisterRichCommand(CommandOpenFile);
            //
            CommandSwitchPhrasePrevious = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_SwitchPrevious,
                UserInterfaceStrings.Audio_SwitchPrevious_,
                UserInterfaceStrings.Audio_SwitchPrevious_KEYS,
                (VisualBrush)Application.Current.FindResource("go-first"),
                obj => AudioPlayer_Stop(), obj => CanSwitchPhrasePrevious);

            shellPresenter.RegisterRichCommand(CommandSwitchPhrasePrevious);
            //
            CommandSwitchPhraseNext = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_SwitchNext,
                UserInterfaceStrings.Audio_SwitchNext_,
                UserInterfaceStrings.Audio_SwitchNext_KEYS,
                (VisualBrush)Application.Current.FindResource("go-last"),
                obj => AudioPlayer_Stop(), obj => CanSwitchPhraseNext);

            shellPresenter.RegisterRichCommand(CommandSwitchPhraseNext);
            //
            CommandGotoBegining = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_GotoBegin,
                UserInterfaceStrings.Audio_GotoBegin_,
                UserInterfaceStrings.Audio_GotoBegin_KEYS,
                (VisualBrush)Application.Current.FindResource("go-previous"),
                obj => AudioPlayer_GotoBegining(), obj => CanGotoBegining);

            shellPresenter.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_GotoEnd,
                UserInterfaceStrings.Audio_GotoEnd_,
                UserInterfaceStrings.Audio_GotoEnd_KEYS,
                (VisualBrush)Application.Current.FindResource("go-next"),
                obj => AudioPlayer_GotoEnd(), obj => CanGotoEnd);

            shellPresenter.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StepBack,
                 UserInterfaceStrings.Audio_StepBack_,
                 UserInterfaceStrings.Audio_StepBack_KEYS,
                (VisualBrush)Application.Current.FindResource("media-skip-backward"),
                obj => AudioPlayer_Stop(), obj => CanStepBack);

            shellPresenter.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StepForward,
                UserInterfaceStrings.Audio_StepForward_,
                UserInterfaceStrings.Audio_StepForward_KEYS,
                (VisualBrush)Application.Current.FindResource("media-skip-forward"),
                obj => AudioPlayer_Stop(), obj => CanStepForward);

            shellPresenter.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_FastForward,
                UserInterfaceStrings.Audio_FastForward_,
                UserInterfaceStrings.Audio_FastForward_KEYS,
                (VisualBrush)Application.Current.FindResource("media-seek-forward"),
                obj => AudioPlayer_FastForward(), obj => CanFastForward);

            shellPresenter.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Rewind,
                UserInterfaceStrings.Audio_Rewind_,
                UserInterfaceStrings.Audio_Rewind_KEYS,
                (VisualBrush)Application.Current.FindResource("media-seek-backward"),
                obj => AudioPlayer_Rewind(), obj => CanRewind);

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
                obj => AudioPlayer_PlayPause(), obj => CanPlay);

            shellPresenter.RegisterRichCommand(CommandPlay);
            //
            CommandPause = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Pause,
                UserInterfaceStrings.Audio_Pause_,
                UserInterfaceStrings.Audio_Pause_KEYS,
                (VisualBrush)Application.Current.FindResource("media-playback-pause"),
                obj => AudioPlayer_Pause(), obj => CanPause);

            shellPresenter.RegisterRichCommand(CommandPause);
            //
            CommandStartRecord = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StartRecord,
                UserInterfaceStrings.Audio_StartRecord_,
                UserInterfaceStrings.Audio_StartRecord_KEYS,
                (VisualBrush)Application.Current.FindResource("media-record"),
                obj => AudioRecorder_StartStop(), obj => CanRecord);

            shellPresenter.RegisterRichCommand(CommandStartRecord);
            //
            CommandStopRecord = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StopRecord,
                UserInterfaceStrings.Audio_StopRecord_,
                UserInterfaceStrings.Audio_StopRecord_KEYS,
                (VisualBrush)Application.Current.FindResource("media-playback-stop"),
                obj => AudioRecorder_Stop(), obj => CanStopRecord);

            shellPresenter.RegisterRichCommand(CommandStopRecord);
            //
            CommandStartMonitor = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StartMonitor,
                UserInterfaceStrings.Audio_StartMonitor_,
                UserInterfaceStrings.Audio_StartMonitor_KEYS,
                (VisualBrush)Application.Current.FindResource("audio-volume-high"),
                obj => AudioRecorder_StartStopMonitor(), obj => CanMonitor);

            shellPresenter.RegisterRichCommand(CommandStartMonitor);
            //
            CommandStopMonitor = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StopMonitor,
                UserInterfaceStrings.Audio_StopMonitor_,
                UserInterfaceStrings.Audio_StopMonitor_KEYS,
                (VisualBrush)Application.Current.FindResource("media-playback-stop"),
                obj => AudioRecorder_StopMonitor(), obj => CanStopMonitor);

            shellPresenter.RegisterRichCommand(CommandStopMonitor);
            //
            CommandEndSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_EndSelection,
                UserInterfaceStrings.Audio_EndSelection_,
                UserInterfaceStrings.Audio_EndSelection_KEYS,
                (VisualBrush)Application.Current.FindResource("format-indent-more"),
                obj => EndSelection(), obj => CanEndSelection);

            shellPresenter.RegisterRichCommand(CommandEndSelection);
            //
            CommandBeginSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_BeginSelection,
                UserInterfaceStrings.Audio_BeginSelection_,
                UserInterfaceStrings.Audio_BeginSelection_KEYS,
                (VisualBrush)Application.Current.FindResource("format-indent-less"),
                obj => BeginSelection(), obj => CanBeginSelection);

            shellPresenter.RegisterRichCommand(CommandBeginSelection);
            //
            if (View != null)
            {
                View.InitGraphicalCommandBindings();
            }

        }

        #endregion Commands
    }
}
