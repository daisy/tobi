using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using AudioLib;
using AudioLib.Events.Player;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.timing;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Player

        private Stream m_PlayStream;
        private List<TreeNodeAndStreamDataLength> m_PlayStreamMarkers;
        private long m_DataLength;
        private PCMFormatInfo m_PcmFormat;
        private long m_StreamRiffHeaderEndPos;

        private AudioPlayer m_Player;
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;

        public bool CanSwapOutputDevice
        {
            get
            {
                return !IsMonitoring && !IsRecording;
            }
        }

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
                    if (time != -1)
                    {
                        AudioPlayer_PlayFrom(AudioPlayer_ConvertMillisecondsToByte(time));
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
                m_LastPlayHeadTime = value;
                updateWaveFormPlayHead(m_LastPlayHeadTime);
                OnPropertyChanged(() => CurrentTimeString);

                /*
                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead();
                }*/
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
                return (m_PcmFormat != null && m_Player.State == AudioPlayerState.Playing);
            }
        }

        public bool IsAudioLoadedWithSubTreeNodes
        {
            get
            {
                return (IsAudioLoadedWithTreeNode && m_CurrentTreeNode != m_CurrentSubTreeNode && m_PlayStreamMarkers != null);
            }
        }

        public bool IsAudioLoadedWithTreeNode
        {
            get
            {
                return (IsAudioLoaded && m_CurrentTreeNode != null);
            }
        }

        public bool IsAudioLoaded
        {
            get
            {
                return m_PcmFormat != null && (!String.IsNullOrEmpty(FilePath) || m_CurrentTreeNode != null);
            }
        }

        private void loadAndPlay()
        {
            Logger.Log("AudioPaneViewModel.loadAndPlay", Category.Debug, Priority.Medium);

            if (m_CurrentAudioStreamProvider() == null)
            {
                return;
            }
            //else the stream is now open

            m_EndOffsetOfPlayStream = m_DataLength;

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            m_PcmFormat = null;
            OnPropertyChanged(() => IsAudioLoaded);
            OnPropertyChanged(() => CurrentPcmFormatString);

            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(false);
            }

            PeakOverloadCountCh1 = 0;
            PeakOverloadCountCh2 = 0;

            if (m_PcmFormat == null)
            {
                m_PlayStream.Position = 0;
                m_PlayStream.Seek(0, SeekOrigin.Begin);

                if (FilePath.Length > 0)
                {
                    m_PcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_PlayStream);
                    m_StreamRiffHeaderEndPos = m_PlayStream.Position;
                }
                else
                {
                    m_PcmFormat = m_CurrentTreeNode.Presentation.MediaDataManager.DefaultPCMFormat.Copy();
                }
            }

            OnPropertyChanged(() => WaveFormTotalTimeString);
            OnPropertyChanged(() => CurrentPcmFormatString);
            OnPropertyChanged(() => IsAudioLoaded);
            OnPropertyChanged(() => IsAudioLoadedWithTreeNode);
            OnPropertyChanged(() => IsAudioLoadedWithSubTreeNodes);
            OnPropertyChanged(() => CurrentTimeString);

            if (View != null)
            {
                View.StartWaveFormLoadTimer(10, IsAutoPlay);
            }
        }

        private void updateWaveFormPlayHead(double time)
        {
            if (View != null)
            {
                View.RefreshUI_WaveFormPlayHead();
            }

            //infinite loop, do not uncomment !
            //LastPlayHeadTime = time;

            if (m_PlayStreamMarkers == null)
            {
                return;
            }

            TreeNode subTreeNode = null;

            long byteOffset = AudioPlayer_GetPcmFormat().GetByteForTime(new Time(LastPlayHeadTime));

            long sumData = 0;
            long sumDataPrev = 0;
            foreach (TreeNodeAndStreamDataLength markers in m_PlayStreamMarkers)
            {
                sumData += markers.m_LocalStreamDataLength;
                if (byteOffset <= sumData)
                {
                    subTreeNode = markers.m_TreeNode;
                    break;
                }
                sumDataPrev = sumData;
            }

            if (View != null)
            {
                View.RefreshUI_WaveFormChunkMarkers(sumDataPrev, sumData);
            }

            if (subTreeNode == null || (subTreeNode == m_CurrentSubTreeNode && subTreeNode != m_CurrentTreeNode))
            {
                return;
            }

            if (m_CurrentSubTreeNode != subTreeNode)
            {
                m_CurrentSubTreeNode = subTreeNode;
                OnPropertyChanged(() => IsAudioLoadedWithSubTreeNodes);
            }

            if (m_CurrentSubTreeNode != m_CurrentTreeNode)
            {
                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.updateWaveFormPlayHead", Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(m_CurrentSubTreeNode);
            }
        }

        public double AudioPlayer_ConvertByteToMilliseconds(double bytes)
        {
            if (m_PcmFormat == null)
            {
                return 0;
            }
            return 1000.0 * bytes / ((double)m_PcmFormat.SampleRate * m_PcmFormat.NumberOfChannels * m_PcmFormat.BitDepth / 8.0);
        }

        public double AudioPlayer_ConvertMillisecondsToByte(double ms)
        {
            if (m_PcmFormat == null)
            {
                return 0;
            }
            return (ms * m_PcmFormat.SampleRate * m_PcmFormat.NumberOfChannels * m_PcmFormat.BitDepth / 8.0) / 1000.0;
        }

        public void AudioPlayer_UpdateWaveFormPlayHead()
        {
            if (m_PcmFormat == null)
            {
                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead();
                }
                return;
            }

            double time = LastPlayHeadTime;
            if (m_Player.State == AudioPlayerState.Playing)
            {
                time = m_Player.CurrentTimePosition;
            }
            LastPlayHeadTime = time;

            updateWaveFormPlayHead(time);
        }

        public void AudioPlayer_LoadWaveForm(bool play)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_LoadWaveForm", Category.Debug, Priority.Medium);

            if (!(m_PcmFormat != null && (!String.IsNullOrEmpty(FilePath) || m_CurrentTreeNode != null)))
            {
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

            if (m_PcmFormat.BitDepth != 16)
            {
                if (!wasPlaying)
                {
                    m_PlayStream.Close();
                    m_PlayStream = null;
                }
                return;
            }

            if (m_CurrentAudioStreamProvider() == null)
            {
                OnPropertyChanged(() => IsAudioLoaded);
                OnPropertyChanged(() => CurrentTimeString);
                OnPropertyChanged(() => IsAudioLoadedWithTreeNode);
                OnPropertyChanged(() => IsAudioLoadedWithSubTreeNodes);

                return;
            }
            // else: the stream is now open

            if (m_DataLength == 0)
            {
                return; //weird bug
            }

            if (View != null)
            {
                View.RefreshUI_LoadWaveForm();
            }

            if (wasPlaying)
            {
                if (!play)
                {
                    m_Player.Resume();
                    return;
                }
                m_Player.Stop();
            }

            if (play)
            {
                TimeDelta dur = m_PcmFormat.GetDuration(m_DataLength);
                m_Player.Play(m_CurrentAudioStreamProvider,
                              dur,
                              m_PcmFormat);
            }
        }

        /// <summary>
        /// If player exists and is playing, then pause. Otherwise if paused or stopped, then plays.
        /// </summary>
        public void AudioPlayer_TogglePlayPause()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_TogglePlayPause", Category.Debug, Priority.Medium);

            if (m_PcmFormat == null)
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

        private long m_EndOffsetOfPlayStream;

        /// <summary>
        /// If player exists, resumes or start playing at the given byte offset in the audio stream,
        /// and ends playback at the specified offset.
        /// </summary>
        /// <param name="bytesStart"></param>
        /// <param name="bytesEnd"></param>
        public void AudioPlayer_PlayFromTo(double bytesStart, double bytesEnd)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_PlayFromTo", Category.Debug, Priority.Medium);

            if (m_PcmFormat == null)
            {
                return;
            }

            m_EndOffsetOfPlayStream = m_DataLength;

            if (m_Player.State == AudioPlayerState.Paused)
            {
                m_Player.Stop();
            }

            if (bytesEnd == -1)
            {
                if (m_Player.State == AudioPlayerState.Stopped)
                {
                    m_CurrentAudioStreamProvider(); // ensure m_PlayStream is open

                    m_EndOffsetOfPlayStream = m_DataLength;

                    m_Player.Play(m_CurrentAudioStreamProvider,
                                  m_PcmFormat.GetDuration(m_DataLength),
                                  m_PcmFormat,
                                  AudioPlayer_ConvertByteToMilliseconds(bytesStart)
                        );
                }
                else if (m_Player.State == AudioPlayerState.Playing)
                {
                    m_Player.CurrentTimePosition = AudioPlayer_ConvertByteToMilliseconds(bytesStart);
                }
            }
            else
            {
                m_EndOffsetOfPlayStream = (long)bytesEnd;

                if (m_Player.State == AudioPlayerState.Playing)
                {
                    m_Player.Stop();
                }

                m_CurrentAudioStreamProvider(); // ensure m_PlayStream is open

                m_Player.Play(m_CurrentAudioStreamProvider,
                              m_PcmFormat.GetDuration(m_DataLength),
                              m_PcmFormat,
                              AudioPlayer_ConvertByteToMilliseconds(bytesStart),
                              AudioPlayer_ConvertByteToMilliseconds(bytesEnd)
                    );
            }

            AudioPlayer_UpdateWaveFormPlayHead();
        }

        public void AudioPlayer_Play()
        {
            if (AudioPlayer_GetPcmFormat() == null)
            {
                return;
            }

            AudioPlayer_Stop();

            Logger.Log("AudioPaneViewModel.AudioPlayer_Play", Category.Debug, Priority.Medium);

            double byteLastPlayHeadTime = AudioPlayer_ConvertMillisecondsToByte(LastPlayHeadTime);

            if (View == null || View.GetSelectionLeft() == -1)
            {
                if (LastPlayHeadTime >=
                        AudioPlayer_ConvertByteToMilliseconds(
                                            AudioPlayer_GetDataLength()))
                {
                    LastPlayHeadTime = 0;
                    AudioPlayer_PlayFrom(0);
                }
                else
                {
                    AudioPlayer_PlayFrom(byteLastPlayHeadTime);
                }
            }
            else
            {
                double byteSelectionLeft = Math.Round(View.GetSelectionLeft() * View.BytesPerPixel);
                double byteSelectionRight = Math.Round((View.GetSelectionLeft() + View.GetSelectionWidth()) * View.BytesPerPixel);

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
            double from = AudioPlayer_ConvertByteToMilliseconds(begin);
            double to = AudioPlayer_ConvertByteToMilliseconds(end);

            var pcmInfo = AudioPlayer_GetPcmFormat();

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
            if (startPosition >= 0 &&
                (endPosition == 0 || startPosition < endPosition) &&
                endPosition <= pcmInfo.GetDataLength(pcmInfo.GetDuration(AudioPlayer_GetDataLength())))
            {
                return true;
            }
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
            if (AudioPlayer_GetPcmFormat() == null)
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

            if (m_PcmFormat == null)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }
        }

        private double m_TimeStepForwardRewind = 1000; //1s

        public void AudioPlayer_Rewind()
        {
            Logger.Log("AudioPaneView.OnRewind", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();
            double newTime = LastPlayHeadTime - m_TimeStepForwardRewind;
            if (newTime < 0)
            {
                newTime = 0;
                SystemSounds.Exclamation.Play();
            }
            LastPlayHeadTime = newTime;

            if (IsAutoPlay)
            {
                if (View != null)
                {
                    View.ClearSelection();
                    AudioPlayer_Play();
                }
            }
        }
        public void AudioPlayer_FastForward()
        {
            Logger.Log("AudioPaneView.OnFastForward", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();
            double newTime = LastPlayHeadTime + m_TimeStepForwardRewind;
            double max = AudioPlayer_ConvertByteToMilliseconds(AudioPlayer_GetDataLength());
            if (newTime > max)
            {
                newTime = max;
                SystemSounds.Exclamation.Play();
            }
            LastPlayHeadTime = newTime;

            if (IsAutoPlay && newTime < max)
            {
                if (View != null)
                {
                    View.ClearSelection();
                    AudioPlayer_Play();
                }
            }
        }
        public void AudioPlayer_GotoEnd()
        {
            AudioPlayer_Stop();
            LastPlayHeadTime = AudioPlayer_ConvertByteToMilliseconds(AudioPlayer_GetDataLength());
        }

        public void AudioPlayer_GotoBegining()
        {
            AudioPlayer_Stop();

            LastPlayHeadTime = 0;

            if (IsAutoPlay)
            {
                if (View != null)
                {
                    View.ClearSelection();
                    AudioPlayer_Play();
                }
            }
        }
        public long AudioPlayer_GetDataLength()
        {
            if (m_PcmFormat == null)
            {
                return 0;
            }

            return m_DataLength;
        }

        private void resetAllInternalPlayerValues()
        {
            Logger.Log("AudioPaneViewModel.resetAllInternalPlayerValues", Category.Debug, Priority.Medium);

            m_CurrentTreeNode = null;
            m_CurrentSubTreeNode = null;
            m_PlayStreamMarkers = null;
            m_PcmFormat = null;
            m_DataLength = 0;
            m_EndOffsetOfPlayStream = 0;
            m_WavFilePath = "";
            m_StreamRiffHeaderEndPos = 0;
            m_PlayStream = null;
            m_LastPlayHeadTime = 0;

            OnPropertyChanged(() => CurrentPcmFormatString);
            OnPropertyChanged(() => IsAudioLoaded);
            OnPropertyChanged(() => IsAudioLoadedWithTreeNode);
            OnPropertyChanged(() => IsAudioLoadedWithSubTreeNodes);
            OnPropertyChanged(() => CurrentTimeString);
            OnPropertyChanged(() => WaveFormTotalTimeString);
        }

        public void AudioPlayer_LoadAndPlayFromFile(string path)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_LoadAndPlayFromFile", Category.Debug, Priority.Medium);

            resetAllInternalPlayerValues();

            FilePath = path;

            LastPlayHeadTime = 0;

            if (View != null)
            {
                View.RefreshUI_WaveFormBackground();

                View.RefreshUI_AllReset();
            }

            m_CurrentAudioStreamProvider = () =>
            {
                if (m_PlayStream == null)
                {
                    if (m_PlayStreamMarkers != null)
                    {
                        m_PlayStreamMarkers.Clear();
                        m_PlayStreamMarkers = null;
                    }
                    if (!String.IsNullOrEmpty(FilePath))
                    {
                        if (!File.Exists(FilePath))
                        {
                            return null;
                        }
                        m_PlayStream = File.Open(FilePath, FileMode.Open);
                    }
                    if (m_PlayStream == null)
                    {
                        return null;
                    }

                    m_DataLength = m_PlayStream.Length;

                    OnPropertyChanged(() => WaveFormTotalTimeString);
                    OnPropertyChanged(() => IsAudioLoaded);
                    OnPropertyChanged(() => IsAudioLoadedWithTreeNode);
                    OnPropertyChanged(() => IsAudioLoadedWithSubTreeNodes);
                    OnPropertyChanged(() => CurrentTimeString);
                }
                return m_PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                resetAllInternalPlayerValues();

                FilePath = null;
                return;
            }

            loadAndPlay();
        }

        public String CurrentPcmFormatString
        {
            get
            {
                return (m_PcmFormat != null ? m_PcmFormat.ToString() : "");
            }
        }

        public PCMFormatInfo AudioPlayer_GetPcmFormat()
        {
            return m_PcmFormat;
        }

        public Stream AudioPlayer_GetPlayStream()
        {
            return m_PlayStream;
        }

        public void AudioPlayer_ClosePlayStream()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_ClosePlayStream", Category.Debug, Priority.Medium);

            m_PlayStream.Close();
            m_PlayStream = null;
        }

        public void AudioPlayer_ResetPlayStreamPosition()
        {
            m_PlayStream.Position = m_StreamRiffHeaderEndPos;
            m_PlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
        }

        public List<TreeNodeAndStreamDataLength> AudioPlayer_GetPlayStreamMarkers()
        {
            return m_PlayStreamMarkers;
        }

        #region Event / Callbacks

        private void OnEndOfAudioAsset(object sender, EndOfAudioAssetEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnEndOfAudioAsset", Category.Debug, Priority.Medium);

            OnPropertyChanged(() => CanSwapOutputDevice);
            OnPropertyChanged(() => CanSwapInputDevice);
            OnPropertyChanged(() => IsPlaying);

            if (m_PcmFormat != null)
            {
                //double time = m_PcmFormat.GetDuration(m_DataLength).TimeDeltaAsMillisecondDouble;
                //long bytes = (long) m_Player.CurrentTimePosition;

                double time = m_PcmFormat.GetDuration(m_EndOffsetOfPlayStream).TimeDeltaAsMillisecondDouble;
                LastPlayHeadTime = time;
                //updateWaveFormPlayHead(time);
            }
            OnPropertyChanged(() => CurrentTimeString);

            UpdatePeakMeter();

            if (FilePath.Length > 0 || m_CurrentTreeNode == null)
            {
                return;
            }

            if (m_EndOffsetOfPlayStream == m_DataLength && IsAutoPlay)
            {
                TreeNode nextNode = m_CurrentTreeNode.GetNextSiblingWithManagedAudio();
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

            OnPropertyChanged(() => CanSwapOutputDevice);
            OnPropertyChanged(() => CanSwapInputDevice);

            OnPropertyChanged(() => IsPlaying);
            OnPropertyChanged(() => CurrentTimeString);

            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                UpdatePeakMeter();
                m_PlayStream = null;
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
