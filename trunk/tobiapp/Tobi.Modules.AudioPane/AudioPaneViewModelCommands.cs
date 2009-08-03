using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Commands

        public RichDelegateCommand<object> CommandShowOptionsDialog { get; private set; }

        public RichDelegateCommand<object> CommandFocus { get; private set; }
        public RichDelegateCommand<object> CommandOpenFile { get; private set; }
        public RichDelegateCommand<object> CommandInsertFile { get; private set; }
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
        public RichDelegateCommand<object> CommandPlayPreviewLeft { get; private set; }
        public RichDelegateCommand<object> CommandPlayPreviewRight { get; private set; }
        public RichDelegateCommand<object> CommandPause { get; private set; }
        public RichDelegateCommand<object> CommandStartRecord { get; private set; }
        public RichDelegateCommand<object> CommandStopRecord { get; private set; }
        public RichDelegateCommand<object> CommandStartMonitor { get; private set; }
        public RichDelegateCommand<object> CommandStopMonitor { get; private set; }
        public RichDelegateCommand<object> CommandBeginSelection { get; private set; }
        public RichDelegateCommand<object> CommandEndSelection { get; private set; }
        public RichDelegateCommand<object> CommandSelectNextChunk { get; private set; }
        public RichDelegateCommand<object> CommandSelectPreviousChunk { get; private set; }
        public RichDelegateCommand<object> CommandDeleteAudioSelection { get; private set; }


        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanGotoBegining
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanGotoEnd
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanPlay
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && !IsPlaying && !IsMonitoring && !IsRecording;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanPause
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && IsPlaying;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanInsertFile
        {
            get
            {
                var session = Container.Resolve<IUrakawaSession>();

                return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                    && session.DocumentProject != null && State.CurrentTreeNode != null;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanRecord
        {
            get
            {
                var session = Container.Resolve<IUrakawaSession>();

                return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                    && (
                    (session.DocumentProject != null && State.CurrentTreeNode != null)
                    ||
                    (session.DocumentProject == null)
                    );
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanStopRecord
        {
            get
            {
                return !IsWaveFormLoading && IsRecording;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanMonitor
        {
            get
            {
                return !IsWaveFormLoading && !IsPlaying && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanStopMonitor
        {
            get
            {
                return !IsWaveFormLoading && IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanRewind
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanFastForward
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoadedWithSubTreeNodes")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanStepBack
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoadedWithSubTreeNodes && !IsRecording && !IsMonitoring;
            }
        }

        [NotifyDependsOn("IsAudioLoadedWithSubTreeNodes")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("PlayStreamMarkers")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanStepForward
        {
            get
            {
                return CanStepBack;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanOpenFile
        {
            get
            {
                return !IsWaveFormLoading && !IsMonitoring && !IsRecording;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanBeginSelection
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanEndSelection
        {
            get
            {
                return !IsWaveFormLoading && IsAudioLoaded && m_SelectionBeginTmp >= 0;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsWaveFormLoading")]
        public bool CanDelete
        {
            get
            {
                var session = Container.Resolve<IUrakawaSession>();

                return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                    &&
                    session.DocumentProject != null && State.CurrentTreeNode != null;
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

            CommandDeleteAudioSelection = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Delete,
                UserInterfaceStrings.Audio_Delete_,
                UserInterfaceStrings.Audio_Delete_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_dialog-cancel"),
                obj => DeleteAudioSelection()
                , obj => CanDelete);

            shellPresenter.RegisterRichCommand(CommandDeleteAudioSelection);
            //
            CommandFocus = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Focus,
                null,
                UserInterfaceStrings.Audio_Focus_KEYS,
                null,
                obj =>
                {
                    if (View != null)
                    {
                        View.BringIntoFocus();
                    }
                }, obj => true);

            shellPresenter.RegisterRichCommand(CommandFocus);
            //
            CommandShowOptionsDialog = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_ShowOptions,
                null,
                UserInterfaceStrings.Audio_ShowOptions_KEYS,
                null,
                obj => AudioPlayer_ShowOptions(), obj => true);

            shellPresenter.RegisterRichCommand(CommandShowOptionsDialog);
            //
            CommandOpenFile = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_OpenFile,
                UserInterfaceStrings.Audio_OpenFile_,
                UserInterfaceStrings.Audio_OpenFile_KEYS,
                shellPresenter.LoadTangoIcon("document-open"),
                obj =>
                {
                    m_PcmFormatOfAudioToInsert = null;
                    openFile(obj as String, false);
                }, obj => CanOpenFile);

            shellPresenter.RegisterRichCommand(CommandOpenFile);
            //
            CommandInsertFile = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_InsertFile,
                UserInterfaceStrings.Audio_InsertFile_,
                UserInterfaceStrings.Audio_InsertFile_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_go-jump"),
                obj =>
                {
                    m_PcmFormatOfAudioToInsert = null;
                    openFile(obj as String, true);
                }, obj => CanInsertFile);

            shellPresenter.RegisterRichCommand(CommandInsertFile);
            //
            CommandGotoBegining = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_GotoBegin,
                UserInterfaceStrings.Audio_GotoBegin_,
                UserInterfaceStrings.Audio_GotoBegin_KEYS,
                shellPresenter.LoadTangoIcon("go-first"),
                obj => AudioPlayer_GotoBegining(), obj => CanGotoBegining);

            shellPresenter.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_GotoEnd,
                UserInterfaceStrings.Audio_GotoEnd_,
                UserInterfaceStrings.Audio_GotoEnd_KEYS,
                shellPresenter.LoadTangoIcon("go-last"),
                obj => AudioPlayer_GotoEnd(), obj => CanGotoEnd);

            shellPresenter.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StepBack,
                 UserInterfaceStrings.Audio_StepBack_,
                 UserInterfaceStrings.Audio_StepBack_KEYS,
                shellPresenter.LoadTangoIcon("media-skip-backward"),
                obj => AudioPlayer_StepBack(), obj => CanStepBack);

            shellPresenter.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StepForward,
                UserInterfaceStrings.Audio_StepForward_,
                UserInterfaceStrings.Audio_StepForward_KEYS,
                shellPresenter.LoadTangoIcon("media-skip-forward"),
                obj => AudioPlayer_StepForward(), obj => CanStepForward);

            shellPresenter.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_FastForward,
                UserInterfaceStrings.Audio_FastForward_,
                UserInterfaceStrings.Audio_FastForward_KEYS,
                shellPresenter.LoadTangoIcon("media-seek-forward"),
                obj => AudioPlayer_FastForward(), obj => CanFastForward);

            shellPresenter.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Rewind,
                UserInterfaceStrings.Audio_Rewind_,
                UserInterfaceStrings.Audio_Rewind_KEYS,
                shellPresenter.LoadTangoIcon("media-seek-backward"),
                obj => AudioPlayer_Rewind(), obj => CanRewind);

            shellPresenter.RegisterRichCommand(CommandRewind);
            //
            CommandSelectAll = new RichDelegateCommand<object>(UserInterfaceStrings.SelectAll,
                UserInterfaceStrings.SelectAll_,
                UserInterfaceStrings.SelectAll_KEYS,
                shellPresenter.LoadTangoIcon("view-fullscreen"),
                obj => SelectAll(), obj => !IsWaveFormLoading);

            shellPresenter.RegisterRichCommand(CommandSelectAll);
            //
            CommandClearSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_ClearSelection,
                UserInterfaceStrings.Audio_ClearSelection_,
                UserInterfaceStrings.Audio_ClearSelection_KEYS,
                shellPresenter.LoadTangoIcon("edit-clear"),
                obj => ClearSelection(), obj => !IsWaveFormLoading && IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandClearSelection);
            //
            CommandZoomSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_ZoomSelection,
                UserInterfaceStrings.Audio_ZoomSelection_,
                UserInterfaceStrings.Audio_ZoomSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Search")),
                //shellPresenter.LoadTangoIcon("system-search"),
                obj => ZoomSelection(), obj => !IsWaveFormLoading && IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandZoomSelection);
            //
            CommandZoomFitFull = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_FitFull,
                UserInterfaceStrings.Audio_FitFull_,
                UserInterfaceStrings.Audio_FitFull_KEYS,
                shellPresenter.LoadTangoIcon("utilities-system-monitor"),
                obj => ZoomFitFull(), obj => !IsWaveFormLoading);

            shellPresenter.RegisterRichCommand(CommandZoomFitFull);
            //
            CommandRefresh = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Reload,
                UserInterfaceStrings.Audio_Reload_,
                null,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Refresh")),
                //shellPresenter.LoadTangoIcon("view-refresh"),
                obj => ReloadWaveForm(), obj => !IsWaveFormLoading && IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandRefresh);
            //
            CommandAutoPlay = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_AutoPlay,
                UserInterfaceStrings.Audio_AutoPlay_,
                UserInterfaceStrings.Audio_AutoPlay_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_go-last"),
                obj => IsAutoPlay = !IsAutoPlay, obj => !IsWaveFormLoading);

            shellPresenter.RegisterRichCommand(CommandAutoPlay);
            //
            //
            CommandPlayPreviewLeft = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_PlayPreviewLeft,
                UserInterfaceStrings.Audio_PlayPreviewLeft_,
                UserInterfaceStrings.Audio_PlayPreviewLeft_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left")),
                obj => AudioPlayer_PlayPreview(true), obj => CanPlay);

            shellPresenter.RegisterRichCommand(CommandPlayPreviewLeft);
            //
            CommandPlayPreviewRight = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_PlayPreviewRight,
                UserInterfaceStrings.Audio_PlayPreviewRight_,
                UserInterfaceStrings.Audio_PlayPreviewRight_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right")),
                obj => AudioPlayer_PlayPreview(false), obj => CanPlay);

            shellPresenter.RegisterRichCommand(CommandPlayPreviewRight);
            //
            //
            CommandPlay = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Play,
                UserInterfaceStrings.Audio_Play_,
                UserInterfaceStrings.Audio_Play_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-start"),
                obj => AudioPlayer_PlayPause(), obj => CanPlay);

            shellPresenter.RegisterRichCommand(CommandPlay);
            //
            CommandPause = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_Pause,
                UserInterfaceStrings.Audio_Pause_,
                UserInterfaceStrings.Audio_Pause_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-pause"),
                obj => AudioPlayer_Pause(), obj => CanPause);

            shellPresenter.RegisterRichCommand(CommandPause);
            //
            CommandStartRecord = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StartRecord,
                UserInterfaceStrings.Audio_StartRecord_,
                UserInterfaceStrings.Audio_StartRecord_KEYS,
                shellPresenter.LoadTangoIcon("media-record"),
                obj => AudioRecorder_StartStop(), obj => CanRecord);

            shellPresenter.RegisterRichCommand(CommandStartRecord);
            //
            CommandStopRecord = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StopRecord,
                UserInterfaceStrings.Audio_StopRecord_,
                UserInterfaceStrings.Audio_StopRecord_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-stop"),
                obj => AudioRecorder_Stop(), obj => CanStopRecord);

            shellPresenter.RegisterRichCommand(CommandStopRecord);
            //
            CommandStartMonitor = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StartMonitor,
                UserInterfaceStrings.Audio_StartMonitor_,
                UserInterfaceStrings.Audio_StartMonitor_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_audio-x-generic"),
                obj => AudioRecorder_StartStopMonitor(), obj => CanMonitor);

            shellPresenter.RegisterRichCommand(CommandStartMonitor);
            //
            CommandStopMonitor = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_StopMonitor,
                UserInterfaceStrings.Audio_StopMonitor_,
                UserInterfaceStrings.Audio_StopMonitor_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-stop"),
                obj => AudioRecorder_StopMonitor(), obj => CanStopMonitor);

            shellPresenter.RegisterRichCommand(CommandStopMonitor);

            //
            CommandSelectPreviousChunk = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_SelectPreviousChunk,
                UserInterfaceStrings.Audio_SelectPreviousChunk_,
                UserInterfaceStrings.Audio_SelectPreviousChunk_KEYS,
                shellPresenter.LoadTangoIcon("go-previous"),
                obj => AudioPlayer_SelectPreviousChunk(), obj => CanStepBack);

            shellPresenter.RegisterRichCommand(CommandSelectPreviousChunk);
            //
            CommandSelectNextChunk = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_SelectNextChunk,
                UserInterfaceStrings.Audio_SelectNextChunk_,
                UserInterfaceStrings.Audio_SelectNextChunk_KEYS,
                shellPresenter.LoadTangoIcon("go-next"),
                obj => AudioPlayer_SelectNextChunk(), obj => CanStepForward);

            shellPresenter.RegisterRichCommand(CommandSelectNextChunk);
            //
            //
            CommandEndSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_EndSelection,
                UserInterfaceStrings.Audio_EndSelection_,
                UserInterfaceStrings.Audio_EndSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right1")),
                obj => EndSelection(), obj => CanEndSelection);

            shellPresenter.RegisterRichCommand(CommandEndSelection);
            //
            CommandBeginSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_BeginSelection,
                UserInterfaceStrings.Audio_BeginSelection_,
                UserInterfaceStrings.Audio_BeginSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left1")),
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
