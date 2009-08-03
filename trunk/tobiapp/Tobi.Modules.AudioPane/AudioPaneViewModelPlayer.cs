using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Input;
using System.Windows.Threading;
using AudioLib;
using AudioLib.Events.Player;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.data.utilities;
using urakawa.media.timing;
using System.Diagnostics;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Player

        //private long m_StreamRiffHeaderEndPos;

        private AudioPlayer m_Player;

        // A pointer to a function that returns a stream of PCM data,
        // not including the PCM format RIFF header.
        // The function also calculates the initial StateData
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;

        private double m_SelectionBeginTmp = -1;

        public void BeginSelection()
        {
            m_SelectionBeginTmp = LastPlayHeadTime;

            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTock();
        }

        public void EndSelection()
        {
            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTockTock();

            if (m_SelectionBeginTmp < 0)
            {
                return;
            }
            double begin = m_SelectionBeginTmp;
            double end = LastPlayHeadTime;

            if (begin == end)
            {
                ClearSelection();
                return;
            }

            if (begin > end)
            {
                double tmp = begin;
                begin = end;
                end = tmp;
            }

            State.Selection.SetSelection(begin, end);

            if (IsAutoPlay)
            {
                if (!State.Audio.HasContent)
                {
                    return;
                }

                IsAutoPlay = false;
                LastPlayHeadTime = begin;
                IsAutoPlay = true;

                double bytesFrom = State.Audio.ConvertMillisecondsToBytes(begin);
                double bytesTo = State.Audio.ConvertMillisecondsToBytes(end);

                AudioPlayer_PlayFromTo(bytesFrom, bytesTo);
            }
        }

        public void ClearSelection()
        {
            State.Selection.ClearSelection();
        }

        public void SelectAll()
        {
            if (!State.Audio.HasContent)
            {
                if (View != null)
                {
                    View.SelectAll();
                }
                return;
            }

            State.Selection.SetSelection(0, State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength));
            
            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTockTock();
        }

        public void SelectChunk(double byteOffset)
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            //if (PlayStreamMarkers == null || PlayStreamMarkers.Count == 1)
            //{
            //    SelectAll();
            //    return;
            //}

            //long byteOffset = (long)Math.Round(AudioPlayer_ConvertMillisecondsToBytes(time));

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
                    //subTreeNode = marker.m_TreeNode;

                    State.Selection.SetSelection(
                        State.Audio.ConvertBytesToMilliseconds(sumDataPrev),
                        State.Audio.ConvertBytesToMilliseconds(sumData));

                    break;
                }
                sumDataPrev = sumData;
            }
        }

        [NotifyDependsOn("SelectionBegin")]
        [NotifyDependsOn("SelectionEnd")]
        public bool IsSelectionSet
        {
            get
            {
                if (State.Audio.HasContent)
                {
                    return State.Selection.IsSelectionSet;
                }
                if (View != null)
                {
                    return View.IsSelectionSet;
                }
                return false;
            }
        }

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
                Logger.Log("AudioPaneViewModel.checkAndDoAutoPlay",
                           Category.Debug, Priority.Medium);

                AudioPlayer_Play();
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


        [NotifyDependsOn("IsAudioLoadedWithTreeNode")]
        [NotifyDependsOn("PlayStream")]
        public bool IsAudioLoadedWithSubTreeNodes
        {
            get
            {
                return (IsAudioLoadedWithTreeNode
                    && State.Audio.PlayStreamMarkers.Count > 1);
            }
        }


        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("CurrentTreeNode")]
        public bool IsAudioLoadedWithTreeNode
        {
            get
            {
                return (IsAudioLoaded && State.CurrentTreeNode != null);
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

        public void ReloadWaveForm()
        {
            StartWaveFormLoadTimer(500, IsAutoPlay);
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

        public void AudioPlayer_PlayPreview(bool left)
        {
            if (!State.Audio.HasContent)
            {
                return;
            }

            AudioPlayer_Stop();

            Logger.Log(String.Format("AudioPaneViewModel.AudioPlayer_PlayPreview ({0})", (left ? "before" : "after")), Category.Debug, Priority.Medium);

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
        }

        public void AudioPlayer_Play()
        {
            if (!State.Audio.HasContent)
            {
                return;
            }

            AudioPlayer_Stop();

            Logger.Log("AudioPaneViewModel.AudioPlayer_Play", Category.Debug, Priority.Medium);

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

        private void AudioPlayer_PlayPause()
        {
            if (IsPlaying)
            {
                AudioPlayer_Pause();
            }
            else
            {
                AudioPlayer_Play();
            }
        }

        private void AudioPlayer_Pause()
        {
            if (!State.Audio.HasContent)
            {
                return;
            }

            AudioPlayer_Stop();
            //ViewModel.AudioPlayer_TogglePlayPause();
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

        public void AudioPlayer_Rewind()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_Rewind", Category.Debug, Priority.Medium);

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
        }
        public void AudioPlayer_FastForward()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_FastForward", Category.Debug, Priority.Medium);

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
        }

        public void AudioPlayer_SelectPreviousChunk()
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            AudioPlayer_StepBack();
            SelectChunk(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));
        }

        public void AudioPlayer_SelectNextChunk()
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            AudioPlayer_StepForward();
            SelectChunk(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));
        }

        public void AudioPlayer_StepBack()
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            Logger.Log("AudioPaneViewModel.AudioPlayer_StepBack", Category.Debug, Priority.Medium);

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
        }

        public void AudioPlayer_StepForward()
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            Logger.Log("AudioPaneViewModel.AudioPlayer_StepForward", Category.Debug, Priority.Medium);

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
        }

        public void AudioPlayer_GotoEnd()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_GotoEnd", Category.Debug, Priority.Medium);

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
        }

        public void AudioPlayer_GotoBegining()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_GotoBegining", Category.Debug, Priority.Medium);

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
        }

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
