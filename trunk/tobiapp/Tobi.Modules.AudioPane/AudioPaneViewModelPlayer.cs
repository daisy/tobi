using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AudioLib;
using AudioLib.Events.Player;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.timing;
using System.Diagnostics;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Player


        private void initializeCommands_Player()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            CommandAutoPlay = new RichDelegateCommand<object>(
               UserInterfaceStrings.Audio_AutoPlay,
               UserInterfaceStrings.Audio_AutoPlay_,
               UserInterfaceStrings.Audio_AutoPlay_KEYS,
               shellPresenter.LoadGnomeNeuIcon("Neu_go-last"),
               obj =>
               {
                   Logger.Log("AudioPaneViewModel.CommandAutoPlay", Category.Debug, Priority.Medium);

                   IsAutoPlay = !IsAutoPlay;
               },
               obj => !IsWaveFormLoading);

            shellPresenter.RegisterRichCommand(CommandAutoPlay);
            //
            //
            CommandPause = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Pause,
                UserInterfaceStrings.Audio_Pause_,
                UserInterfaceStrings.Audio_Pause_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-pause"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandPause", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && IsPlaying);

            shellPresenter.RegisterRichCommand(CommandPause);
            //
            CommandPlay = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Play,
                UserInterfaceStrings.Audio_Play_,
                UserInterfaceStrings.Audio_Play_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-start"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandPlay", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    if (!IsSelectionSet)
                    {
                        if (LastPlayHeadTime >=
                                State.Audio.ConvertBytesToMilliseconds(
                                                    State.Audio.DataLength))
                        {
                            //LastPlayHeadTime = 0; infinite loop !
                            AudioPlayer_PlayFrom(0);
                        }
                        else
                        {
                            double byteLastPlayHeadTime = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);
                            AudioPlayer_PlayFrom(byteLastPlayHeadTime);
                        }
                    }
                    else
                    {
                        double byteSelectionLeft = Math.Round(State.Audio.ConvertMillisecondsToBytes(State.Selection.SelectionBegin));
                        double byteSelectionRight = Math.Round(State.Audio.ConvertMillisecondsToBytes(State.Selection.SelectionEnd));

                        double byteLastPlayHeadTime = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);
                        byteLastPlayHeadTime = Math.Round(byteLastPlayHeadTime);

                        if (byteLastPlayHeadTime >= byteSelectionLeft
                                && byteLastPlayHeadTime < byteSelectionRight)
                        {
                            if (verifyBeginEndPlayerValues(byteLastPlayHeadTime, byteSelectionRight))
                            {
                                AudioPlayer_PlayFromTo(byteLastPlayHeadTime, byteSelectionRight);
                            }
                        }
                        else
                        {
                            if (verifyBeginEndPlayerValues(byteSelectionLeft, byteSelectionRight))
                            {
                                AudioPlayer_PlayFromTo(byteSelectionLeft, byteSelectionRight);
                            }
                        }
                    }
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && !IsPlaying && !IsMonitoring && !IsRecording);

            shellPresenter.RegisterRichCommand(CommandPlay);
            //
            CommandPlayPreviewLeft = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_PlayPreviewLeft,
                UserInterfaceStrings.Audio_PlayPreviewLeft_,
                UserInterfaceStrings.Audio_PlayPreviewLeft_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left")),
                obj =>
                {
                    bool left = (obj == null ? true : false);

                    Logger.Log(
                        String.Format("AudioPaneViewModel.CommandPlayPreview ({0})",
                                      (left ? "before" : "after")), Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double from = 0;
                    double to = 0;
                    if (left)
                    {
                        from = Math.Max(0, LastPlayHeadTime - m_TimePreviewPlay);
                        to = LastPlayHeadTime;
                    }
                    else
                    {
                        from = LastPlayHeadTime;
                        to = Math.Min(State.Audio.DataLength, LastPlayHeadTime + m_TimePreviewPlay);
                    }

                    double byteLeft = Math.Round(State.Audio.ConvertMillisecondsToBytes(from));
                    double byteRight = Math.Round(State.Audio.ConvertMillisecondsToBytes(to));

                    if (verifyBeginEndPlayerValues(byteLeft, byteRight))
                    {
                        AudioPlayer_PlayFromTo(byteLeft, byteRight);

                        State.Audio.EndOffsetOfPlayStream = (long)(left ? byteRight : byteLeft);
                    }
                },
                obj => CommandPlay.CanExecute(null));

            shellPresenter.RegisterRichCommand(CommandPlayPreviewLeft);
            //
            CommandPlayPreviewRight = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_PlayPreviewRight,
                UserInterfaceStrings.Audio_PlayPreviewRight_,
                UserInterfaceStrings.Audio_PlayPreviewRight_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right")),
                obj => CommandPlayPreviewLeft.Execute(new Boolean()),
                obj => CommandPlay.CanExecute(null));

            shellPresenter.RegisterRichCommand(CommandPlayPreviewRight);
            //
            CommandGotoBegining = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_GotoBegin,
                UserInterfaceStrings.Audio_GotoBegin_,
                UserInterfaceStrings.Audio_GotoBegin_KEYS,
                shellPresenter.LoadTangoIcon("go-first"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGotoBegining", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    if (LastPlayHeadTime == 0)
                    {
                        SystemSounds.Beep.Play();
                    }
                    else
                    {
                        if (IsAutoPlay)
                        {
                            if (View != null)
                            {
                                View.ClearSelection();
                            }
                        }

                        LastPlayHeadTime = 0;
                    }
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_GotoEnd,
                UserInterfaceStrings.Audio_GotoEnd_,
                UserInterfaceStrings.Audio_GotoEnd_KEYS,
                shellPresenter.LoadTangoIcon("go-last"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGotoEnd", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double end = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);

                    if (LastPlayHeadTime == end)
                    {
                        SystemSounds.Beep.Play();
                    }
                    else
                    {
                        if (IsAutoPlay)
                        {
                            if (View != null)
                            {
                                View.ClearSelection();
                            }
                        }

                        LastPlayHeadTime = end;
                    }
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_StepBack,
                 UserInterfaceStrings.Audio_StepBack_,
                 UserInterfaceStrings.Audio_StepBack_KEYS,
                shellPresenter.LoadTangoIcon("media-skip-backward"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepBack", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    long prev = -1;
                    long begin = 0;

                    var bytes = (long)Math.Round(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));

                    int index = -1;

                    foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                    {
                        index++;

                        long end = (begin + marker.m_LocalStreamDataLength);
                        if (bytes >= begin && bytes < end
                            || index == (State.Audio.PlayStreamMarkers.Count - 1) && bytes >= end)
                        {
                            if (prev == -1)
                            {
                                if (IsAutoPlay)
                                {
                                    if (View != null)
                                    {
                                        View.ClearSelection();
                                    }
                                }

                                LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(begin);
                                SystemSounds.Beep.Play();
                                break;
                            }

                            if (IsAutoPlay)
                            {
                                if (View != null)
                                {
                                    View.ClearSelection();
                                }
                            }

                            LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(prev);
                            break;
                        }
                        prev = begin;
                        begin += marker.m_LocalStreamDataLength;
                    }
                },
                obj => !IsWaveFormLoading && IsAudioLoadedWithSubTreeNodes && !IsRecording && !IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_StepForward,
                UserInterfaceStrings.Audio_StepForward_,
                UserInterfaceStrings.Audio_StepForward_KEYS,
                shellPresenter.LoadTangoIcon("media-skip-forward"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepForward", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    long begin = 0;

                    var bytes = (long)Math.Round(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));

                    bool found = false;

                    int index = -1;
                    foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                    {
                        index++;

                        if (found)
                        {
                            if (IsAutoPlay)
                            {
                                if (View != null)
                                {
                                    View.ClearSelection();
                                }
                            }

                            LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(begin);
                            return;
                        }

                        long end = (begin + marker.m_LocalStreamDataLength);
                        if (bytes >= begin && bytes < end
                            || index == (State.Audio.PlayStreamMarkers.Count - 1) && bytes >= end)
                        {
                            found = true;
                        }

                        begin += marker.m_LocalStreamDataLength;
                    }

                    if (!found)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }

                    SystemSounds.Beep.Play();
                },
                obj => CommandStepBack.CanExecute(null));

            shellPresenter.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_FastForward,
                UserInterfaceStrings.Audio_FastForward_,
                UserInterfaceStrings.Audio_FastForward_KEYS,
                shellPresenter.LoadTangoIcon("media-seek-forward"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandFastForward", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double newTime = LastPlayHeadTime + m_TimeStepForwardRewind;
                    double max = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);
                    if (newTime > max)
                    {
                        newTime = max;
                        SystemSounds.Beep.Play();
                    }

                    if (IsAutoPlay)
                    {
                        if (View != null)
                        {
                            View.ClearSelection();
                        }
                    }

                    LastPlayHeadTime = newTime;
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Rewind,
                UserInterfaceStrings.Audio_Rewind_,
                UserInterfaceStrings.Audio_Rewind_KEYS,
                shellPresenter.LoadTangoIcon("media-seek-backward"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandRewind", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double newTime = LastPlayHeadTime - m_TimeStepForwardRewind;
                    if (newTime < 0)
                    {
                        newTime = 0;
                        SystemSounds.Beep.Play();
                    }

                    if (IsAutoPlay)
                    {
                        if (View != null)
                        {
                            View.ClearSelection();
                        }
                    }

                    LastPlayHeadTime = newTime;
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandRewind);
            //
        }


        //private long m_StreamRiffHeaderEndPos;

        private AudioPlayer m_Player;

        // A pointer to a function that returns a stream of PCM data,
        // not including the PCM format RIFF header.
        // The function also calculates the initial StateData
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanSwapOutputDevice
        {
            get
            {
                return !IsMonitoring && !IsRecording;
            }
        }

        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanSwapInputDevice
        {
            get
            {
                return !IsMonitoring && !IsRecording;
            }
        }

        public List<OutputDevice> OutputDevices
        {
            get
            {
                return m_Player.OutputDevices;
            }
        }
        public OutputDevice OutputDevice
        {
            get
            {
                return m_Player.OutputDevice;
            }
            set
            {
                if (value != null && m_Player.OutputDevice != value)
                {
                    double time = -1;
                    if (m_Player.State == AudioPlayerState.Playing)
                    {
                        time = m_Player.CurrentTimePosition;
                        AudioPlayer_Stop();
                    }

                    m_Player.SetDevice(GetWindowsFormsHookControl(), value);

                    if (time >= 0 && State.Audio.HasContent)
                    {
                        AudioPlayer_PlayFrom(State.Audio.ConvertMillisecondsToBytes(time));
                    }
                }
            }
        }

        private double m_LastPlayHeadTime;
        public double LastPlayHeadTime
        {
            get
            {
                return m_LastPlayHeadTime;
            }
            set
            {
                if (m_LastPlayHeadTime == value)
                {
                    return;
                }

                m_LastPlayHeadTime = value;

                if (m_LastPlayHeadTime < 0)
                {
                    Debug.Fail(String.Format("m_LastPlayHeadTime < 0 ?? {0}", m_LastPlayHeadTime));
                    m_LastPlayHeadTime = 0;
                }

                if (State.Audio.HasContent)
                {
                    double time = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);
                    //double time = PcmFormat.GetDuration(DataLength).TimeDeltaAsMillisecondDouble;
                    if (m_LastPlayHeadTime > time)
                    {
                        Debug.Fail(String.Format("m_LastPlayHeadTime > DataLength ?? {0}", m_LastPlayHeadTime));
                        m_LastPlayHeadTime = time;
                    }
                }

                OnPropertyChanged(() => LastPlayHeadTime);

                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead();
                }

                if (!State.Audio.HasContent)
                {
                    return;
                }

                if (State.CurrentTreeNode == null)
                {
                    checkAndDoAutoPlay();
                    return;
                }

                TreeNode subTreeNode = null;

                //long byteOffset = PcmFormat.GetByteForTime(new Time(LastPlayHeadTime));
                long byteOffset = (long)Math.Round(State.Audio.ConvertMillisecondsToBytes(m_LastPlayHeadTime));

                long sumData = 0;
                long sumDataPrev = 0;
                int index = -1;
                foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                {
                    index++;
                    sumData += marker.m_LocalStreamDataLength;
                    if (byteOffset < sumData
                    || index == (State.Audio.PlayStreamMarkers.Count - 1) && byteOffset >= sumData)
                    {
                        subTreeNode = marker.m_TreeNode;

                        if (View != null && subTreeNode != State.CurrentSubTreeNode)
                        {
                            View.RefreshUI_WaveFormChunkMarkers(sumDataPrev, sumData);
                        }
                        break;
                    }
                    sumDataPrev = sumData;
                }

                if (subTreeNode == null || subTreeNode == State.CurrentSubTreeNode)
                {
                    checkAndDoAutoPlay();
                    return;
                }

                State.CurrentSubTreeNode = subTreeNode;

                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.updateWaveFormPlayHead",
                               Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(State.CurrentSubTreeNode);

                //if (State.CurrentSubTreeNode != State.CurrentTreeNode)
                //{
                //    Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.updateWaveFormPlayHead",
                //               Category.Debug, Priority.Medium);

                //    EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(State.CurrentSubTreeNode);
                //}
                //else
                //{
                //    checkAndDoAutoPlay();
                //}
            }
        }

        private void checkAndDoAutoPlay()
        {
            if (IsAutoPlay
                && (m_Player.State == AudioPlayerState.Paused
                || m_Player.State == AudioPlayerState.Stopped))
            {
                Logger.Log("AudioPaneViewModel.checkAndDoAutoPlay", Category.Debug, Priority.Medium);

                CommandPlay.Execute(null);
            }
        }

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_IsAutoPlay = false;
        // ReSharper restore RedundantDefaultFieldInitializer
        public bool IsAutoPlay
        {
            get
            {
                return m_IsAutoPlay;
            }
            set
            {
                if (m_IsAutoPlay == value) return;
                m_IsAutoPlay = value;

                OnPropertyChanged(() => IsAutoPlay);
            }
        }

        public bool IsPlaying
        {
            get
            {
                return (m_Player.State == AudioPlayerState.Playing);
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("CurrentTreeNode")]
        [NotifyDependsOn("PlayStream")]
        public bool IsAudioLoadedWithSubTreeNodes
        {
            get
            {
                return (IsAudioLoaded
                    && State.CurrentTreeNode != null
                    && State.Audio.PlayStreamMarkers.Count > 1);
            }
        }

        [NotifyDependsOn("PlayStream")]
        public bool IsAudioLoaded
        {
            get
            {
                return State.Audio.HasContent && (View == null || View.BytesPerPixel > 0);
            }
        }

        private void loadAndPlay()
        {
            Logger.Log("AudioPaneViewModel.loadAndPlay", Category.Debug, Priority.Medium);

            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(false);
            }

            PeakOverloadCountCh1 = 0;
            PeakOverloadCountCh2 = 0;

            //StartWaveFormLoadTimer(0, IsAutoPlay);

            m_ForcePlayAfterWaveFormLoaded = IsAutoPlay;
            AudioPlayer_LoadWaveForm(m_ForcePlayAfterWaveFormLoaded);
        }

        public void RefreshWaveFormChunkMarkers()
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            Logger.Log("AudioPaneViewModel.RefreshWaveFormChunkMarkers", Category.Debug, Priority.Medium);

            long byteOffset = (long)Math.Round(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));

            long sumData = 0;
            long sumDataPrev = 0;
            int index = -1;
            TreeNode subTreeNode = null;
            foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
            {
                index++;
                sumData += marker.m_LocalStreamDataLength;
                if (byteOffset < sumData
                    || index == (State.Audio.PlayStreamMarkers.Count - 1) && byteOffset >= sumData)
                {
                    subTreeNode = marker.m_TreeNode;
                    break;
                }
                sumDataPrev = sumData;
            }

            if (View != null) // && subTreeNode != CurrentSubTreeNode
            {
                View.RefreshUI_WaveFormChunkMarkers(sumDataPrev, sumData);
            }
        }

        public void AudioPlayer_UpdateWaveFormPlayHead()
        {
            if (!State.Audio.HasContent)
            {
                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead();
                }
                return;
            }

            double time = LastPlayHeadTime;
            if (m_Player.State == AudioPlayerState.Playing
                || m_Player.State == AudioPlayerState.Paused)
            {
                time = m_Player.CurrentTimePosition;
            }
            else if (m_Player.State == AudioPlayerState.Stopped && time < 0)
            {
                time = 0;
            }

            if (time == LastPlayHeadTime)
            {
                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead();
                }
            }
            else
            {
                LastPlayHeadTime = time;
            }
        }

        private bool m_IsWaveFormLoading;
        public bool IsWaveFormLoading
        {
            get
            {
                return m_IsWaveFormLoading;
            }
            set
            {
                if (value != m_IsWaveFormLoading)
                {
                    m_IsWaveFormLoading = value;
                    OnPropertyChanged(() => IsWaveFormLoading);

                    // Manually forcing the commands to refresh their "canExecute" state
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }


        private DispatcherTimer m_WaveFormLoadTimer;

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_ForcePlayAfterWaveFormLoaded = false;
        // ReSharper restore RedundantDefaultFieldInitializer

        private static readonly Object LOCK = new Object();

        private void StartWaveFormLoadTimer(double delay, bool play)
        {
            if (IsWaveFormLoading)
            {
                return;
            }

            lock (LOCK)
            {
                m_ForcePlayAfterWaveFormLoaded = play;

                if (View != null)
                {
                    View.ShowHideWaveFormLoadingMessage(true);
                }

                if (m_WaveFormLoadTimer == null)
                {
                    m_WaveFormLoadTimer = new DispatcherTimer(DispatcherPriority.Normal);
                    m_WaveFormLoadTimer.Tick += OnWaveFormLoadTimerTick;
                    // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                    if (delay == 0)
                    // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                    {
                        m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(0);
                        //TODO: does this work ?? (immediate dispatch)
                    }
                    else
                    {
                        m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(delay);
                    }
                }
                else if (m_WaveFormLoadTimer.IsEnabled)
                {
                    m_WaveFormLoadTimer.Stop();
                }

                m_WaveFormLoadTimer.Start();
            }
        }

        private void OnWaveFormLoadTimerTick(object sender, EventArgs e)
        {
            m_WaveFormLoadTimer.Stop();

            if (IsWaveFormLoading)
            {
                return;
            }

            AudioPlayer_LoadWaveForm(m_ForcePlayAfterWaveFormLoaded);
        }

        public void AudioPlayer_LoadWaveForm(bool play)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_LoadWaveForm", Category.Debug, Priority.Medium);

            if (String.IsNullOrEmpty(State.FilePath) && State.CurrentTreeNode == null)
            {
                if (View != null)
                {
                    View.ShowHideWaveFormLoadingMessage(false);
                }
                return;
            }

            bool wasPlaying = (m_Player.State == AudioPlayerState.Playing);

            if (m_Player.State != AudioPlayerState.NotReady)
            {
                if (wasPlaying)
                {
                    m_Player.Pause();
                }
            }

            if (m_CurrentAudioStreamProvider() == null)
            {
                if (View != null)
                {
                    View.ShowHideWaveFormLoadingMessage(false);
                }
                return;
            }
            // else: the stream is now open

            if (View != null)
            {
                View.ShowHideWaveFormLoadingMessage(true);
            }

            if (View != null)
            {
                View.RefreshUI_LoadWaveForm(wasPlaying, play);
            }
            else
            {
                AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying, play);
            }
        }

        public void AudioPlayer_PlayAfterWaveFormLoaded(bool wasPlaying, bool play)
        {
            if (m_StateToRestore != null)
            {
                LastPlayHeadTime = m_StateToRestore.GetValueOrDefault().LastPlayHeadTime;

                double begin = m_StateToRestore.GetValueOrDefault().SelectionBegin;
                double end = m_StateToRestore.GetValueOrDefault().SelectionEnd;

                if (begin >= 0 && end >= 0)
                {
                    State.Selection.SetSelection(begin, end);
                }

                m_StateToRestore = null;
            }

            // ensure the stream is closed before we resume the player
            //m_PlayStream.Close();
            //m_PlayStream = null;

            if (wasPlaying)
            {
                m_Player.Resume();
                return;

                /*
                if (!play)
                {
                    m_Player.Resume();
                    return;
                }
                m_Player.Stop();
                 * */
            }

            if (play)
            {
                //TimeDelta dur = PcmFormat.GetDuration(DataLength);

                double duration = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);
                TimeDelta dur = new TimeDelta(duration);

                m_Player.Play(m_CurrentAudioStreamProvider,
                              dur.TimeDeltaAsMillisecondDouble,
                              new AudioLibPCMFormat(State.Audio.PcmFormat.NumberOfChannels, State.Audio.PcmFormat.SampleRate, State.Audio.PcmFormat.BitDepth));
            }
            else
            {
                AudioPlayer_UpdateWaveFormPlayHead();
            }
        }

        /// <summary>
        /// If player exists and is playing, then pause. Otherwise if paused or stopped, then plays.
        /// </summary>
        public void AudioPlayer_TogglePlayPause()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_TogglePlayPause", Category.Debug, Priority.Medium);

            if (!State.Audio.HasContent)
            {
                return;
            }

            if (m_Player.State == AudioPlayerState.Playing)
            {
                m_Player.Pause();
            }
            else if (m_Player.State == AudioPlayerState.Paused || m_Player.State == AudioPlayerState.Stopped)
            {
                m_Player.Resume();
            }
        }

        /// <summary>
        /// If player exists, resumes or start playing at the given byte offset in the audio stream
        /// </summary>
        /// <param name="bytes"></param>
        public void AudioPlayer_PlayFrom(double bytes)
        {
            AudioPlayer_PlayFromTo(bytes, -1);
        }

        /// <summary>
        /// If player exists, resumes or start playing at the given byte offset in the audio stream,
        /// and ends playback at the specified offset.
        /// </summary>
        /// <param name="bytesStart"></param>
        /// <param name="bytesEnd"></param>
        public void AudioPlayer_PlayFromTo(double bytesStart, double bytesEnd)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_PlayFromTo", Category.Debug, Priority.Medium);

            if (!State.Audio.HasContent)
            {
                return;
            }

            State.Audio.EndOffsetOfPlayStream = State.Audio.DataLength;

            if (m_Player.State == AudioPlayerState.Paused)
            {
                m_Player.Stop();
            }

            if (bytesEnd < 0)
            {
                if (m_Player.State == AudioPlayerState.Stopped)
                {
                    if (m_CurrentAudioStreamProvider() == null)
                    {
                        return;
                    }
                    // else: the stream is now open

                    State.Audio.EndOffsetOfPlayStream = State.Audio.DataLength;

                    double duration = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);

                    m_Player.Play(m_CurrentAudioStreamProvider,
                                  duration,
                                  new AudioLibPCMFormat(State.Audio.PcmFormat.NumberOfChannels, State.Audio.PcmFormat.SampleRate, State.Audio.PcmFormat.BitDepth),
                                  State.Audio.ConvertBytesToMilliseconds(bytesStart)
                        );
                }
                else if (m_Player.State == AudioPlayerState.Playing)
                {
                    m_Player.CurrentTimePosition = State.Audio.ConvertBytesToMilliseconds(bytesStart);
                }
            }
            else
            {
                State.Audio.EndOffsetOfPlayStream = (long)bytesEnd;

                if (m_Player.State == AudioPlayerState.Playing)
                {
                    m_Player.Stop();
                }

                if (m_CurrentAudioStreamProvider() == null)
                {
                    return;
                }
                // else: the stream is now open

                double duration = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);

                m_Player.Play(m_CurrentAudioStreamProvider,
                              duration,
                              new AudioLibPCMFormat(State.Audio.PcmFormat.NumberOfChannels, State.Audio.PcmFormat.SampleRate, State.Audio.PcmFormat.BitDepth),
                              State.Audio.ConvertBytesToMilliseconds(bytesStart),
                              State.Audio.ConvertBytesToMilliseconds(bytesEnd)
                    );
            }

            //AudioPlayer_UpdateWaveFormPlayHead(); rounding problems between player.currentTime and playheadtime => let's let the vumeter callback do the refresh.
        }

        private bool verifyBeginEndPlayerValues(double begin, double end)
        {
            double from = State.Audio.ConvertBytesToMilliseconds(begin);
            double to = State.Audio.ConvertBytesToMilliseconds(end);

            PCMFormatInfo pcmInfo = State.Audio.PcmFormat;
            if (pcmInfo == null)
            {
                pcmInfo = m_PcmFormatOfAudioToInsert;
            }

            long startPosition = 0;
            if (from > 0)
            {
                startPosition = CalculationFunctions.ConvertTimeToByte(from, (int)pcmInfo.SampleRate, pcmInfo.BlockAlign);
                startPosition = CalculationFunctions.AdaptToFrame(startPosition, pcmInfo.BlockAlign);
            }
            long endPosition = 0;
            if (to > 0)
            {
                endPosition = CalculationFunctions.ConvertTimeToByte(to, (int)pcmInfo.SampleRate, pcmInfo.BlockAlign);
                endPosition = CalculationFunctions.AdaptToFrame(endPosition, pcmInfo.BlockAlign);
            }
            double time = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);
            TimeDelta timeD = new TimeDelta(time);
            //TimeDelta timeD = pcmInfo.GetDuration(DataLength);
            if (startPosition >= 0 &&
                (endPosition == 0 || startPosition < endPosition) &&
                endPosition <= pcmInfo.GetDataLength(timeD))
            {
                return true;
            }

            Debug.Fail("Oops, this should have never happened !");
            return false;
        }

        /// <summary>
        /// If player exists and is ready but is not stopped, then stops.
        /// </summary>
        public void AudioPlayer_Stop()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_Stop", Category.Debug, Priority.Medium);

            if (!State.Audio.HasContent)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }
        }

        private double m_TimeStepForwardRewind = 500; // 500ms
        private double m_TimePreviewPlay = 1500; // 1.5s

        private void ensurePlaybackStreamIsDead()
        {
            m_Player.EnsurePlaybackStreamIsDead();
            State.Audio.ResetAll();
        }

        public void AudioPlayer_LoadAndPlayFromFile(string path)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_LoadAndPlayFromFile", Category.Debug, Priority.Medium);

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }
            }

            State.ResetAll();

            m_LastPlayHeadTime = -1;
            IsWaveFormLoading = false;

            State.FilePath = path;

            m_CurrentAudioStreamProvider = () =>
            {
                if (State.Audio.PlayStream == null)
                {
                    if (String.IsNullOrEmpty(State.FilePath))
                    {
                        State.ResetAll();
                        return null;
                    }
                    if (!File.Exists(State.FilePath))
                    {
                        State.ResetAll();
                        return null;
                    }
                    try
                    {
                        State.Audio.PlayStream = File.Open(State.FilePath, FileMode.Open, FileAccess.Read,
                                                           FileShare.Read);
                    }
                    catch (Exception ex)
                    {
                        State.ResetAll();

                        m_LastPlayHeadTime = -1;
                        IsWaveFormLoading = false;
                        return null;
                    }
                }
                return State.Audio.PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                State.ResetAll();

                m_LastPlayHeadTime = -1;
                IsWaveFormLoading = false;
                return;
            }

            //m_LastPlayHeadTime = 0; Set after waveform loaded

            loadAndPlay();
        }

        [NotifyDependsOn("PcmFormat")]
        public String PcmFormatString
        {
            get
            {
                PCMFormatInfo pcmInfo = State.Audio.PcmFormat;
                if (pcmInfo == null)
                {
                    pcmInfo = m_PcmFormatOfAudioToInsert;
                }
                return (pcmInfo != null ? pcmInfo.ToString() : "");
            }
        }

        public Stream AudioPlayer_GetPlayStream()
        {
            return m_CurrentAudioStreamProvider(); // m_PlayStream;
        }

        /*
        public void AudioPlayer_ResetPlayStreamPosition()
        {
            m_PlayStream.Position = m_StreamRiffHeaderEndPos;
            m_PlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
        }*/

        #region Event / Callbacks

        private void OnEndOfAudioAsset(object sender, EndOfAudioAssetEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnEndOfAudioAsset", Category.Debug, Priority.Medium);

            OnPropertyChanged(() => IsPlaying);

            if (State.Audio.HasContent)
            {
                //double time = m_PcmFormat.GetDuration(m_DataLength).TimeDeltaAsMillisecondDouble;
                //long bytes = (long) m_Player.CurrentTimePosition;

                double time = State.Audio.ConvertBytesToMilliseconds(State.Audio.EndOffsetOfPlayStream);
                //double time = PcmFormat.GetDuration(m_EndOffsetOfPlayStream).TimeDeltaAsMillisecondDouble;

                bool oldVal = IsAutoPlay;
                IsAutoPlay = false;
                LastPlayHeadTime = time;
                IsAutoPlay = oldVal;

                //updateWaveFormPlayHead(time);
            }

            CommandManager.InvalidateRequerySuggested();

            UpdatePeakMeter();

            if (State.CurrentTreeNode != null && State.Audio.EndOffsetOfPlayStream == State.Audio.DataLength && IsAutoPlay)
            {
                TreeNode nextNode = State.CurrentTreeNode.GetNextSiblingWithManagedAudio();
                if (nextNode != null)
                {
                    Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnEndOfAudioAsset", Category.Debug, Priority.Medium);

                    EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                }
            }
        }

        private void OnPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnPlayerStateChanged", Category.Debug, Priority.Medium);

            OnPropertyChanged(() => IsPlaying);

            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                UpdatePeakMeter();

                if (!AudioPlaybackStreamKeepAlive && m_Player.State == AudioPlayerState.Stopped)
                {
                    // stream has been closed already on AudioPlayer side, we're just making sure to reset our cached pointer value.
                    ensurePlaybackStreamIsDead();
                }

                if (View != null)
                {
                    View.StopWaveFormTimer();
                    View.StopPeakMeterTimer();
                }
            }

            if (m_Player.State == AudioPlayerState.Playing)
            {
                if (e.OldState == AudioPlayerState.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StartWaveFormTimer();
                    View.StartPeakMeterTimer();
                }
            }
        }

        #endregion Event / Callbacks

        #endregion Audio Player
    }
}
