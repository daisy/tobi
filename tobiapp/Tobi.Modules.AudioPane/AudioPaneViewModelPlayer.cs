using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;
using urakawa.media.data.audio;
using System.Diagnostics;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Player


        public RichDelegateCommand CommandAutoPlay { get; private set; }

        public RichDelegateCommand CommandPlay { get; private set; }

        private bool m_PlayAutoAdvance = false;
        public RichDelegateCommand CommandPlayAutoAdvance { get; private set; }

        public RichDelegateCommand CommandPlayPreviewLeft { get; private set; }
        public RichDelegateCommand CommandPlayPreviewRight { get; private set; }
        public RichDelegateCommand CommandPause { get; private set; }

        public RichDelegateCommand CommandPlaybackRateDown { get; private set; }
        public RichDelegateCommand CommandPlaybackRateUp { get; private set; }
        public RichDelegateCommand CommandPlaybackRateReset { get; private set; }

        public int AudioPlayer_RefreshInterval
        {
            get { return m_Player.RefreshInterval; }
        }

        private const float PLAYBACK_RATE_MIN = 1f;
        private const float PLAYBACK_RATE_MAX = 2.5f;
        private const float PLAYBACK_RATE_STEP = 0.25f;

        private void initializeCommands_Player()
        {
            CommandPlaybackRateReset = new RichDelegateCommand(
                  Tobi_Plugin_AudioPane_Lang.CmdAudioPlayRateReset_ShortDesc,
                  Tobi_Plugin_AudioPane_Lang.CmdAudioPlayRateReset_LongDesc,
                  null, // KeyGesture obtained from settings (see last parameters below)
                  m_ShellView.LoadGnomeGionIcon("Gion_go-previous"),
                  () =>
                  {
                      Logger.Log("AudioPaneViewModel.CommandPlaybackRateReset", Category.Debug, Priority.Medium);

                      PlaybackRate = PLAYBACK_RATE_MIN;
                  },
                  () => true
                //&& !IsWaveFormLoading
                      ,
                   Settings_KeyGestures.Default,
                   PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlaybackRateReset));

            m_ShellView.RegisterRichCommand(CommandPlaybackRateReset);
            //
            CommandPlaybackRateDown = new RichDelegateCommand(
               Tobi_Plugin_AudioPane_Lang.CmdAudioPlayRateDown_ShortDesc,
               Tobi_Plugin_AudioPane_Lang.CmdAudioPlayRateDown_LongDesc,
               null, // KeyGesture obtained from settings (see last parameters below)
               m_ShellView.LoadGnomeGionIcon("Gion_go-down"),
               () =>
               {
                   Logger.Log("AudioPaneViewModel.CommandPlaybackRateDown", Category.Debug, Priority.Medium);

                   if ((PlaybackRate - PLAYBACK_RATE_STEP) >= PLAYBACK_RATE_MIN)
                       PlaybackRate -= PLAYBACK_RATE_STEP;
                   else
                   {
                       PlaybackRate = PLAYBACK_RATE_MIN;
                       Debug.Fail("This should never happen !");
                   }
               },
               () => (PlaybackRate - PLAYBACK_RATE_STEP) >= PLAYBACK_RATE_MIN
                //&& !IsWaveFormLoading 
               ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlaybackRateDown));

            m_ShellView.RegisterRichCommand(CommandPlaybackRateDown);
            //
            CommandPlaybackRateUp = new RichDelegateCommand(
               Tobi_Plugin_AudioPane_Lang.CmdAudioPlayRateUp_ShortDesc,
               Tobi_Plugin_AudioPane_Lang.CmdAudioPlayRateUp_LongDesc,
               null, // KeyGesture obtained from settings (see last parameters below)
               m_ShellView.LoadGnomeGionIcon("Gion_go-up"),
               () =>
               {
                   Logger.Log("AudioPaneViewModel.CommandPlaybackRateUp", Category.Debug, Priority.Medium);

                   if ((PlaybackRate + PLAYBACK_RATE_STEP) <= PLAYBACK_RATE_MAX)
                       PlaybackRate += PLAYBACK_RATE_STEP;
                   else
                   {
                       PlaybackRate = PLAYBACK_RATE_MAX;
                       Debug.Fail("This should never happen !");
                   }
               },
               () => (PlaybackRate + PLAYBACK_RATE_STEP) <= PLAYBACK_RATE_MAX
                //&& !IsWaveFormLoading 
               ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlaybackRateUp));

            m_ShellView.RegisterRichCommand(CommandPlaybackRateUp);
            //
            CommandAutoPlay = new RichDelegateCommand(
               Tobi_Plugin_AudioPane_Lang.CmdAudioAutoPlay_ShortDesc,
               Tobi_Plugin_AudioPane_Lang.CmdAudioAutoPlay_LongDesc,
               null, // KeyGesture obtained from settings (see last parameters below)
               m_ShellView.LoadTangoIcon("applications-multimedia"),
               () =>
               {
                   Logger.Log("AudioPaneViewModel.CommandAutoPlay", Category.Debug, Priority.Medium);

                   //if (IsAutoPlay)
                   //{
                   //    AudioCues.PlayTock();
                   //}
                   //else
                   //{
                   //    AudioCues.PlayTockTock();
                   //}

                   IsAutoPlay = !IsAutoPlay;
               },
               () => true
                //&& !IsWaveFormLoading
                   ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ToggleAutoPlayMode));

            m_ShellView.RegisterRichCommand(CommandAutoPlay);
            //
            //
            CommandPause = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioPause_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioPause_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-pause"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandPause", Category.Debug, Priority.Medium);

                    m_PlayAutoAdvance = false;

                    SetRecordAfterPlayOverwriteSelection(-1);

                    long playBytePosition = PlayBytePosition;

                    m_Player.Stop();

                    SetPlayHeadTimeBypassAutoPlay(playBytePosition);

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.PlaybackStopped);
                    }

                    if (IsMonitoringAlways)
                    {
                        CommandStartMonitor.Execute();
                    }
                },
                () => State.Audio.HasContent && IsPlaying
                //&& !IsWaveFormLoading 
                ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlayPause));

            m_ShellView.RegisterRichCommand(CommandPause);
            //
            CommandPlay = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlay_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlay_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-start"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandPlay", Category.Debug, Priority.Medium);
                    CommandPause.Execute();

                    if (IsMonitoring)
                    {
                        CommandStopMonitor.Execute();
                    }

                    //#if DEBUG
                    //                    Logger.Log("AudioPaneViewModel.CommandPlay (called PAUSE)", Category.Debug, Priority.Medium);
                    //#endif


                    if (PlayBytePosition < 0)
                    {
                        m_LastSetPlayBytePosition = 0;
                    }

                    if (!IsSelectionSet)
                    {
                        //if (LastPlayHeadTime >= State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength))
                        if (PlayBytePosition >= State.Audio.DataLength)
                        {
                            //LastPlayHeadTime = 0; infinite loop !
                            AudioPlayer_PlayFromTo(0, -1);
                        }
                        else
                        {
                            AudioPlayer_PlayFromTo(PlayBytePosition, -1);
                        }
                    }
                    else
                    {
                        if (false
                            && PlayBytePosition >= State.Selection.SelectionBeginBytePosition
                                && PlayBytePosition < State.Selection.SelectionEndBytePosition)
                        {
                            //if (verifyBeginEndPlayerValues(byteLastPlayHeadTime, byteSelectionRight))
                            //{
                            //}
                            AudioPlayer_PlayFromTo(PlayBytePosition, State.Selection.SelectionEndBytePosition);
                        }
                        else
                        {
                            //if (verifyBeginEndPlayerValues(byteSelectionLeft, byteSelectionRight))
                            //{
                            //}
                            AudioPlayer_PlayFromTo(State.Selection.SelectionBeginBytePosition, State.Selection.SelectionEndBytePosition);
                        }
                    }
                },
                () => State.Audio.HasContent
                    && !IsPlaying
                    && (!IsMonitoring || IsMonitoringAlways)
                    && !IsRecording
                //&& !IsWaveFormLoading 
                ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlayPause));

            m_ShellView.RegisterRichCommand(CommandPlay);
            //
            CommandPlayAutoAdvance = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlayAutoAdvance_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlayAutoAdvance_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeGionIcon("applications-multimedia"),//emblem-system
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandPlayAutoAdvance", Category.Debug, Priority.Medium);

                    m_PlayAutoAdvance = true;
                    CommandPlay.Execute();
                },
                () => CommandPlay.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlayAutoAdvance));

            m_ShellView.RegisterRichCommand(CommandPlayAutoAdvance);
            //
            CommandPlayPreviewLeft = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlayPreviewLeft_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlayPreviewLeft_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left")),
                () => PlayPreviewLeftRight(true),
                () => CommandPlay.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlayLeftPreview));

            m_ShellView.RegisterRichCommand(CommandPlayPreviewLeft);
            //
            CommandPlayPreviewRight = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlayPreviewRight_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioPlayPreviewRight_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right")),
                () => PlayPreviewLeftRight(false),
                () => CommandPlay.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_PlayRightPreview));

            m_ShellView.RegisterRichCommand(CommandPlayPreviewRight);
            //
        }

        private void PlayPreviewLeftRight(bool left)
        {
            Logger.Log(String.Format("AudioPaneViewModel.CommandPlayPreview ({0})",
                              (left ? "before" : "after")), Category.Debug, Priority.Medium);

            CommandPause.Execute();

            long from = 0;
            long to = 0;
            if (left)
            {
                from = Math.Max(0, PlayBytePosition - State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes((long)Settings.Default.AudioWaveForm_TimeStepPlayPreview * 2 * AudioLibPCMFormat.TIME_UNIT));
                to = PlayBytePosition;
            }
            else
            {
                from = PlayBytePosition;
                to = Math.Min(State.Audio.DataLength, PlayBytePosition + State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes((long)Settings.Default.AudioWaveForm_TimeStepPlayPreview * 2 * AudioLibPCMFormat.TIME_UNIT));
            }

            if (from == to)
            {
                return;
            }

            //if (verifyBeginEndPlayerValues(byteLeft, byteRight))
            //{
            //}
            AudioPlayer_PlayFromTo(from, to);

            State.Audio.EndOffsetOfPlayStream = left ? to : from;

            //DebugFix.Assert(State.Audio.EndOffsetOfPlayStream == (left ? to : from));
        }

        //private long m_StreamRiffHeaderEndPos;

        private AudioPlayer m_Player;

        // A pointer to a function that returns a stream of PCM data,
        // not including the PCM format RIFF header.
        // The function also calculates the initial StateData
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;
        private AudioPlayer.StreamProviderDelegate m_AudioStreamProvider_TreeNode;
        private AudioPlayer.StreamProviderDelegate m_AudioStreamProvider_File;

        [NotifyDependsOn("IsMonitoringAlways")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanSwapOutputDevice
        {
            get
            {
                return (!IsMonitoring || IsMonitoringAlways) && !IsRecording;
            }
        }


        private List<OutputDevice> m_OutputDevices;
        public List<OutputDevice> OutputDevices
        {
            get
            {
                m_OutputDevices = m_Player.OutputDevices;
                return m_OutputDevices;
            }
        }
        public OutputDevice OutputDevice
        {
            get
            {
                if (m_OutputDevices != null)
                    foreach (var outputDevice in m_OutputDevices)
                    {
                        if (outputDevice.Name == m_Player.OutputDevice.Name)
                            return outputDevice;
                    }
                return m_Player.OutputDevice;
            }
            set
            {
                if (value != null
                    && (m_Player.OutputDevice == null || m_Player.OutputDevice.Name != value.Name))
                {
                    Settings.Default.Audio_OutputDevice = value.Name;
                }
            }
        }

        //private void checkAndDoAutoPlay()
        //{
        //    if (IsAutoPlay
        //        //&& (
        //        ////m_Player.CurrentState == AudioPlayer.State.Paused ||
        //        //IsStopped)
        //        )
        //    {
        //        Logger.Log("AudioPaneViewModel.checkAndDoAutoPlay", Category.Debug, Priority.Medium);

        //        CommandPlay.Execute();
        //    }
        //}

        
      
        
        [NotifyDependsOn("IsAutoPlay")]
        public string IsAutoPlayString
        {
            get { return Tobi_Plugin_AudioPane_Lang.AudioAutoPlay + (IsAutoPlay ? " [ON]" : " [OFF]"); }
        }

        [NotifyDependsOn("PlaybackRate")]
        public string PlaybackRateString
        {
            get
            {
                if (IsMonitoring || IsRecording) return "";
                return String.Format(Tobi_Plugin_AudioPane_Lang.PlaybackX, PlaybackRate);
            }
        }

        public float PlaybackRate
        {
            get
            {
                return m_Player.FastPlayFactor;
            }
            set
            {
                if (m_Player.FastPlayFactor == value) return;
                m_Player.FastPlayFactor = value;

                RaisePropertyChanged(() => PlaybackRate);
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

                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.AudioAutoPlay + (m_IsAutoPlay ? " (ON)" : " (OFF)"));
                }

                RaisePropertyChanged(() => IsAutoPlay);
            }
        }

        public bool IsPlaying
        {
            get
            {
                return (m_Player.CurrentState == AudioPlayer.State.Playing);
            }
        }
        public bool IsStopped
        {
            get
            {
                return (m_Player.CurrentState == AudioPlayer.State.Stopped);
            }
        }

        //[NotifyDependsOnEx("CurrentTreeNode", typeof(StateData))]
        //[NotifyDependsOn("IsAudioLoaded")]
        //[NotifyDependsOnEx("PlayStream", typeof(StreamStateData))]
        //public bool IsAudioLoadedWithSubTreeNodes
        //{
        //    get
        //    {
        //        //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
        //        return (IsAudioLoaded
        //            //&& treeNodeSelection.Item1 != null
        //            && State.Audio.PlayStreamMarkers != null
        //            //&& State.Audio.PlayStreamMarkers.Count > 1
        //            );
        //    }
        //}

        //[NotifyDependsOnEx("PlayStream", typeof(StreamStateData))]
        //public bool IsAudioLoaded
        //{
        //    get
        //    {
        //        return State.Audio.HasContent; // && (View == null || View.BytesPerPixel > 0);
        //    }
        //}

        public void RefreshWaveFormChunkMarkers()
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            if (treeNodeSelection.Item1 == null || !State.Audio.HasContent)
            {
                return;
            }

            //Logger.Log("AudioPaneViewModel.RefreshWaveFormChunkMarkers", Category.Debug, Priority.Medium);

            long bytesRight;
            long bytesLeft;
            int index;
            TreeNode subTreeNode;
            bool match = State.Audio.FindInPlayStreamMarkers(PlayBytePosition, out subTreeNode, out index, out bytesLeft, out bytesRight);

            if (match)
            {
                if (View != null) // && subTreeNode != CurrentSubTreeNode
                {
                    View.RefreshUI_WaveFormChunkMarkers(bytesLeft, bytesRight);
                }
            }
            else
            {
                Debug.Fail("audio chunk not found ??");
                return;
            }
        }

        public void AudioPlayer_UpdateWaveFormPlayHead()
        {
            if (!State.Audio.HasContent)
            {
                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead(true);
                }
                return;
            }

            long bytePosition = PlayBytePosition;
            //if (IsPlaying
            //    //|| m_Player.CurrentState == AudioPlayer.State.Paused
            //    )
            //{
            //    time = m_Player.CurrentTime;
            //}
            //else if (IsStopped && time < 0)
            //{
            //    //time = 0;
            //}

            if (bytePosition >= 0)
            {
                SetPlayHeadTimeBypassAutoPlay(bytePosition);
                //PlayHeadTime = time;
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

                    if (EventAggregator != null)
                    {
                        if (m_IsWaveFormLoading)
                        {
                            EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(
                                Tobi_Plugin_AudioPane_Lang.LoadingWaveform);
                        }
                        else
                        {
                            EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(
                                Tobi_Plugin_AudioPane_Lang.WaveformLoaded);
                        }
                    }

                    RaisePropertyChanged(() => IsWaveFormLoading);

                    // Manually forcing the commands to refresh their "canExecute" state
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }


        //        private DispatcherTimer m_WaveFormLoadTimer;

        //        private static readonly Object LOCK = new Object();

        //        private void StartWaveFormLoadTimer(long delayMilliseconds)
        //        {
        //            if (IsWaveFormLoading)
        //            {
        //                return;
        //            }

        //            lock (LOCK)
        //            {
        //                if (false && View != null)
        //                {
        //                    View.ShowHideWaveFormLoadingMessage(true);
        //                }
        //                if (delayMilliseconds == 0)
        //                {
        ////#if DEBUG
        ////                    Logger.Log("CALLING AudioPlayer_LoadWaveForm (StartWaveFormLoadTimer)", Category.Debug, Priority.Medium);
        ////#endif
        //                    AudioPlayer_LoadWaveForm(false);
        //                    return;
        //                }
        //                if (m_WaveFormLoadTimer == null)
        //                {
        //                    m_WaveFormLoadTimer = new DispatcherTimer(DispatcherPriority.Normal);
        //                    m_WaveFormLoadTimer.Tick += OnWaveFormLoadTimerTick;
        //                    // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
        //                    if (delayMilliseconds == 0)
        //                    // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
        //                    {
        //                        m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(0);
        //                        //TODO: does this work ?? (immediate dispatch)
        //                    }
        //                    else
        //                    {
        //                        m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(delayMilliseconds);
        //                    }
        //                }
        //                else if (m_WaveFormLoadTimer.IsEnabled)
        //                {
        //                    //Logger.Log("m_WaveFormLoadTimer.Stop()", Category.Debug, Priority.Medium);

        //                    m_WaveFormLoadTimer.Stop();
        //                }

        //                //Logger.Log("m_WaveFormLoadTimer.Start()", Category.Debug, Priority.Medium);

        //                m_WaveFormLoadTimer.Start();
        //            }
        //        }

        //        private void OnWaveFormLoadTimerTick(object sender, EventArgs e)
        //        {
        //            if (!TheDispatcher.CheckAccess())
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<object, EventArgs>)OnWaveFormLoadTimerTick, sender, e);
        //                return;
        //            }

        //            m_WaveFormLoadTimer.Stop();

        //            if (IsWaveFormLoading)
        //            {
        //                return;
        //            }
        ////#if DEBUG
        ////            Logger.Log("CALLING AudioPlayer_LoadWaveForm (OnWaveFormLoadTimerTick)", Category.Debug, Priority.Medium);
        ////#endif
        //            AudioPlayer_LoadWaveForm(false);
        //        }

        public void AudioPlayer_LoadWaveForm(bool onlyUpdateTiles)
        {
            //Logger.Log("AudioPaneViewModel.AudioPlayer_LoadWaveForm", Category.Debug, Priority.Medium);
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            if (String.IsNullOrEmpty(State.FilePath) && treeNodeSelection.Item1 == null)
            {
                if (View != null)
                {
                    View.ShowHideWaveFormLoadingMessage(false);
                }
                return;
            }

            bool wasPlaying = IsPlaying;

            //#if DEBUG
            //            Logger.Log("AudioPlayer_LoadWaveForm (calling PAUSE)", Category.Debug, Priority.Medium);
            //#endif

            if (!onlyUpdateTiles)
            {
                CommandPause.Execute();
            }

            //#if DEBUG
            //            Logger.Log("AudioPlayer_LoadWaveForm (called PAUSE)", Category.Debug, Priority.Medium);
            //#endif

            //if (wasPlaying)
            //{
            //    //m_Player.Pause();
            //    //m_Player.Stop();
            //}

            if (m_CurrentAudioStreamProvider() == null)
            {
                if (View != null)
                {
                    View.ShowHideWaveFormLoadingMessage(false);
                }
                return;
            }
            // else: the stream is now open

            //if (!onlyUpdateTiles)
            //{
            //    State.Selection.ClearSelection();
            //}

            if (View != null)
            {
                //View.RefreshCanvasWidth();

                if (false && !onlyUpdateTiles)
                {
                    View.ShowHideWaveFormLoadingMessage(true);
                }

                View.RefreshUI_LoadWaveForm(wasPlaying, onlyUpdateTiles);
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
                AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying);
            }
        }

        struct StateToRestore
        {
            public long SelectionBeginBytePosition;
            public long SelectionEndBytePosition;
            public long PlayHeadBytePosition;
        }
        private StateToRestore? m_StateToRestore = null;

        public void AudioPlayer_PlayAfterWaveFormLoaded(bool wasPlaying)
        {
            if (m_StateToRestore != null)
            {
                long begin = m_StateToRestore.GetValueOrDefault().SelectionBeginBytePosition;
                long end = m_StateToRestore.GetValueOrDefault().SelectionEndBytePosition;

                if (begin >= 0 && end >= 0)
                {
                    State.Selection.SetSelectionBytes(begin, end);
                }
                else
                {
                    State.Selection.ResetAll();
                }

                long newBytePosition = m_StateToRestore.GetValueOrDefault().PlayHeadBytePosition;

                m_StateToRestore = null;

                if (newBytePosition < 0)
                {
                    m_LastSetPlayBytePosition = -1;
                    //AudioPlayer_UpdateWaveFormPlayHead();
                    if (View != null)
                    {
                        View.RefreshUI_WaveFormPlayHead(true);
                    }

                    //RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);
                }
                else
                {
                    PlayBytePosition = newBytePosition;
                    RefreshWaveFormChunkMarkers();
                }

                return;
            }

            // ensure the stream is closed before we resume the player
            //m_PlayStream.Close();
            //m_PlayStream = null;

            if (wasPlaying)
            {

                //#if DEBUG
                //                Logger.Log("AudioPlayer_PlayAfterWaveFormLoaded (wasPlaying)", Category.Debug, Priority.Medium);
                //#endif

                //m_Player.Resume();
                CommandPlay.Execute();
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

            if (IsAutoPlay)
            {
                //#if DEBUG
                //                Logger.Log("AudioPlayer_PlayAfterWaveFormLoaded (IsAutoPlay)", Category.Debug, Priority.Medium);
                //#endif
                CommandPlay.Execute();
                //OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_OutputDevice)));
                //m_Player.PlayBytes(m_CurrentAudioStreamProvider,
                //              State.Audio.DataLength,
                //              State.Audio.PcmFormat.Copy().Data,
                //              -1, -1);
            }
            else
            {
                AudioPlayer_UpdateWaveFormPlayHead();
            }
        }

        /// <summary>
        /// If player exists and is playing, then pause. Otherwise if paused or stopped, then plays.
        /// </summary>
        //public void AudioPlayer_TogglePlayPause()
        //{
        //    //Logger.Log("AudioPaneViewModel.AudioPlayer_TogglePlayPause", Category.Debug, Priority.Medium);

        //    if (!State.Audio.HasContent)
        //    {
        //        return;
        //    }

        //    if (m_Player.CurrentState == AudioPlayer.State.Playing)
        //    {
        //        m_Player.Pause();

        //        bool wasAutoPlay = IsAutoPlay;
        //        if (wasAutoPlay) IsAutoPlay = false;
        //        LastPlayHeadTime = m_Player.CurrentTime;
        //        if (wasAutoPlay) IsAutoPlay = true;
        //    }
        //    else if (m_Player.CurrentState == AudioPlayer.State.Paused || m_Player.CurrentState == AudioPlayer.State.Stopped)
        //    {
        //        m_Player.Resume();
        //    }
        //}

        /// <summary>
        /// If player exists, resumes or start playing at the given byte offset in the audio stream,
        /// and ends playback at the specified offset.
        /// </summary>
        /// <param name="bytesStart"></param>
        /// <param name="bytesEnd"></param>
        public void AudioPlayer_PlayFromTo(long bytesStart, long bytesEnd)
        {
            //Logger.Log("AudioPaneViewModel.AudioPlayer_PlayFromTo", Category.Debug, Priority.Medium);

            if (!State.Audio.HasContent)
            {
                return;
            }

            State.Audio.EndOffsetOfPlayStream = State.Audio.DataLength;

            //if (m_Player.CurrentState == AudioPlayer.State.Paused)
            //{
            //    m_Player.Stop();
            //}

            if (bytesEnd < 0)
            {
                if (IsPlaying)
                {
                    CommandPause.Execute();
                    //m_Player.Stop();
                }

                if (IsStopped)
                {
                    if (m_CurrentAudioStreamProvider() == null)
                    {
                        return;
                    }
                    // else: the stream is now open

                    State.Audio.EndOffsetOfPlayStream = State.Audio.DataLength;

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_OutputDevice)));
                    m_Player.PlayBytes(m_CurrentAudioStreamProvider,
                                  State.Audio.DataLength,
                                  State.Audio.PcmFormat.Copy().Data,
                                  bytesStart,
                                  -1
                        );
                }
                //else if (m_Player.CurrentState == AudioPlayer.State.Playing)
                //{
                //    m_Player.CurrentTime = State.Audio.ConvertBytesToMilliseconds(bytesStart);
                //}
            }
            else
            {
                State.Audio.EndOffsetOfPlayStream = bytesEnd;

                CommandPause.Execute();
                //if (IsPlaying)
                //{
                //    //m_Player.Stop();
                //}

                if (m_CurrentAudioStreamProvider() == null)
                {
                    return;
                }
                // else: the stream is now open

                OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_OutputDevice)));
                m_Player.PlayBytes(m_CurrentAudioStreamProvider,
                              State.Audio.DataLength,
                              State.Audio.PcmFormat.Copy().Data,
                              bytesStart,
                              bytesEnd
                    );
            }

            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Playing);
            }

            //AudioPlayer_UpdateWaveFormPlayHead(); rounding problems between player.currentTime and playheadtime => let's let the vumeter callback do the refresh.
        }

        //private bool verifyBeginEndPlayerValues(double begin, double end)
        //{
        //    double from = State.Audio.ConvertBytesToMilliseconds(begin);
        //    double to = State.Audio.ConvertBytesToMilliseconds(end);

        //    PCMFormatInfo pcmInfo = State.Audio.PcmFormat;
        //    if (pcmInfo == null)
        //    {
        //        pcmInfo = m_PcmFormatOfAudioToInsert;
        //    }

        //    long startPosition = 0;
        //    if (from > 0)
        //    {
        //        startPosition = CalculationFunctions.ConvertTimeToByte(from, (int)pcmInfo.SampleRate, pcmInfo.BlockAlign);
        //        startPosition = CalculationFunctions.AdaptToFrame(startPosition, pcmInfo.BlockAlign);
        //    }
        //    long endPosition = 0;
        //    if (to > 0)
        //    {
        //        endPosition = CalculationFunctions.ConvertTimeToByte(to, (int)pcmInfo.SampleRate, pcmInfo.BlockAlign);
        //        endPosition = CalculationFunctions.AdaptToFrame(endPosition, pcmInfo.BlockAlign);
        //    }
        //    double time = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);
        //    Time timeD = new Time(time);
        //    //Time timeD = pcmInfo.GetDuration(DataLength);
        //    if (startPosition >= 0 &&
        //        (endPosition == 0 || startPosition < endPosition) &&
        //        endPosition <= pcmInfo.GetDataLength(timeD))
        //    {
        //        return true;
        //    }

        //    Debug.Fail("Oops, this should have never happened !");
        //    return false;
        //}

        //private long m_TimeStepForwardRewindInLocalUnits = 500 * AudioLibPCMFormat.TIME_UNIT; // 500ms
        //private long m_TimePreviewPlayInLocalUnits = 1500 * AudioLibPCMFormat.TIME_UNIT; // 1.5s

        private void ensurePlaybackStreamIsDead()
        {
            bool itClosedTheStream = m_Player.EnsurePlaybackStreamIsDead();
            if (!itClosedTheStream && State.Audio.PlayStream != null)
            {
                State.Audio.PlayStream.Close();
            }
            if (State.Audio.SecondaryAudioStream != null)
            {
                State.Audio.SecondaryAudioStream.Close();
            }
            State.Audio.ResetAll();
        }

        public void AudioPlayer_LoadAndPlayFromFile(string path)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_LoadAndPlayFromFile", Category.Debug, Priority.Medium);

            CommandPause.Execute();
            //if (m_Player.CurrentState != AudioPlayer.State.NotReady
            //    && !IsStopped)
            //{
            //    //m_Player.Stop();
            //}

            if (View != null)
            {
                View.CancelWaveFormLoad(true);
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            State.ResetAll();

            m_LastSetPlayBytePosition = 0;
            //IsWaveFormLoading = false;

            State.FilePath = path;

            m_CurrentAudioStreamProvider = m_AudioStreamProvider_File;

            if (m_CurrentAudioStreamProvider() == null)
            {
                State.ResetAll();

                m_LastSetPlayBytePosition = -1;
                //IsWaveFormLoading = false;
                return;
            }

            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(true);
                //View.ResetPeakLines();
            }

            PeakOverloadCountCh1 = 0;
            PeakOverloadCountCh2 = 0;

            //#if DEBUG
            //            Logger.Log("CALLING AudioPlayer_LoadWaveForm (loadAndPlay) FILE", Category.Debug, Priority.Medium);
            //#endif
            AudioPlayer_LoadWaveForm(false);
        }

        //[NotifyDependsOn("IsRecording")]
        //[NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOnEx("PcmFormatRecordingMonitoring", typeof(StreamStateData))]
        [NotifyDependsOnEx("PcmFormat", typeof(StreamStateData))]
        public bool PcmFormatStringVisible
        {
            get
            {
                PCMFormatInfo pcmInfo = State.Audio.GetCurrentPcmFormat();
                return pcmInfo != null;
            }
        }

        //[NotifyDependsOn("IsRecording")]
        //[NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("PcmFormatStringVisible")]
        public String PcmFormatString
        {
            get
            {
                PCMFormatInfo pcmInfo = State.Audio.GetCurrentPcmFormat();
                return (pcmInfo != null ? pcmInfo.ToString() : "");
            }
        }

        public Stream AudioPlayer_GetWaveformAudioStream()
        {
            m_CurrentAudioStreamProvider(); // make sure it's initialized
            return State.Audio.SecondaryAudioStream ?? State.Audio.PlayStream;
        }

        /*
        public void AudioPlayer_ResetPlayStreamPosition()
        {
            m_PlayStream.Position = m_StreamRiffHeaderEndPos;
            m_PlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
        }*/

        #region Event / Callbacks

        private void OnAudioPlaybackFinished(object sender, AudioPlayer.AudioPlaybackFinishEventArgs e)
        {
            if (!TheDispatcher.CheckAccess())
            {
                TheDispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, AudioPlayer.AudioPlaybackFinishEventArgs>)OnAudioPlaybackFinished_,
                                  sender, e);
                return;
            }
#if DEBUG
            Debugger.Break();
#endif
        }

        private void OnAudioPlaybackFinished_RefreshStatus()
        {
            RaisePropertyChanged(() => IsPlaying);

            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.PlaybackEnded);
            }

            CommandManager.InvalidateRequerySuggested();

            UpdatePeakMeter();

            View.RefreshUI_WaveFormPlayHead(true);
        }

        private void OnAudioPlaybackFinished_(object sender, AudioPlayer.AudioPlaybackFinishEventArgs e)
        {
            bool gotoNext = State.Audio.EndOffsetOfPlayStream == State.Audio.DataLength
                            && (IsAutoPlay || m_PlayAutoAdvance)
                            && !IsSelectionSet
                            && m_UrakawaSession.DocumentProject != null;

            //Logger.Log("AudioPaneViewModel.OnAudioPlaybackFinished", Category.Debug, Priority.Medium);

            if (State.Audio.HasContent)
            {
                SetPlayHeadTimeBypassAutoPlay(State.Audio.EndOffsetOfPlayStream);

                //updateWaveFormPlayHead(time);
            }

            if (gotoNext)
            {
                OnAudioPlaybackFinished_RefreshStatus();

                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                TreeNode nextNode = treeNodeSelection.Item1.GetNextSiblingWithManagedAudio();

            next:
                if (nextNode != null)
                {
                    if (Settings.Default.Audio_EnableSkippability && m_UrakawaSession.isTreeNodeSkippable(nextNode))
                    {
                        nextNode = nextNode.GetNextSiblingWithManagedAudio();
                        goto next;
                    }

                    if (IsWaveFormLoading)
                    {
                        if (View != null)
                        {
                            View.CancelWaveFormLoad(true);
                        }
                    }

                    //Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnAudioPlaybackFinished", Category.Debug, Priority.Medium);

                    //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                    m_UrakawaSession.PerformTreeNodeSelection(nextNode);
                }
                else
                {
                    m_PlayAutoAdvance = false;

                    if (IsMonitoringAlways)
                    {
                        CommandStartMonitor.Execute();
                    }
                }
            }
            else
            {
                m_PlayAutoAdvance = false;

                if (m_RecordAfterPlayOverwriteSelection > 0) // && State.Selection.SelectionBeginBytePosition == m_RecordAfterPlayOverwriteSelection)
                {
                    SetRecordAfterPlayOverwriteSelection(-1);
                    m_RecordAfterPlayOverwriteSelection = 1; // hack
                    CommandStartRecord.Execute();
                    m_RecordAfterPlayOverwriteSelection = -1; // hack
                }
                else
                {
                    OnAudioPlaybackFinished_RefreshStatus();

                    if (IsMonitoringAlways)
                    {
                        CommandStartMonitor.Execute();
                    }
                }
            }
        }

        private void OnStateChanged_Player(object sender, AudioPlayer.StateChangedEventArgs e)
        {
            if (!TheDispatcher.CheckAccess())
            {
                TheDispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, AudioPlayer.StateChangedEventArgs>)OnStateChanged_Player_, sender, e);
                return;
            }
            OnStateChanged_Player_(sender, e);
        }
        private void OnStateChanged_Player_(object sender, AudioPlayer.StateChangedEventArgs e)
        {

            //Logger.Log("AudioPaneViewModel.OnStateChanged_Player", Category.Debug, Priority.Medium);


            CommandManager.InvalidateRequerySuggested();

            resetPeakMeter();

            RaisePropertyChanged(() => IsPlaying);

            if (e.OldState == AudioPlayer.State.Playing
                && (
                //m_Player.CurrentState == AudioPlayer.State.Paused ||
                    IsStopped))
            {
                UpdatePeakMeter();

                if (!AudioPlaybackStreamKeepAlive && IsStopped)
                {
                    // stream has been closed already on AudioPlayer side, we're just making sure to reset our cached pointer value.
                    ensurePlaybackStreamIsDead();
                }

                if (View != null)
                {
                    View.StopWaveFormTimer();
                    //View.StopPeakMeterTimer();
                }

                //if (IsMonitoringAlways)
                //{
                //    CommandStartMonitor.Execute();
                //}
            }

            if (IsPlaying)
            {
                if (e.OldState == AudioPlayer.State.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StartWaveFormTimer();
                    //View.StartPeakMeterTimer();
                }
            }
        }

        //private void OnPlayerResetVuMeter(object sender, UpdateVuMeterEventArgs e)
        //{
        //    resetPeakMeter();
        //}

        #endregion Event / Callbacks

        #endregion Audio Player
    }
}
