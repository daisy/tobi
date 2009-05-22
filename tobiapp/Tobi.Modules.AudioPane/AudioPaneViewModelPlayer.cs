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
        private long m_StreamRiffHeaderEndPos;

        private AudioPlayer m_Player;
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;

        private double m_SelectionBeginTmp = -1;

        public void BeginSelection()
        {
            m_SelectionBeginTmp = LastPlayHeadTime;
        }

        public void EndSelection()
        {
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

            SelectionBegin = begin;
            SelectionEnd = end;

            if (View != null)
            {
                View.SetSelection(SelectionBegin, SelectionEnd);
            }
        }

        [NotifyDependsOn("SelectionBegin")]
        [NotifyDependsOn("SelectionEnd")]
        public bool IsSelectionSet
        {
            get
            {
                return SelectionBegin >= 0 && SelectionEnd >= 0;
            }
        }

        private double m_SelectionBegin;
        public double SelectionBegin
        {
            get
            {
                return m_SelectionBegin;
            }
            set
            {
                if (m_SelectionBegin == value) return;
                m_SelectionBegin = value;
                OnPropertyChanged(() => SelectionBegin);
            }
        }

        private double m_SelectionEnd;
        public double SelectionEnd
        {
            get
            {
                return m_SelectionEnd;
            }
            set
            {
                if (m_SelectionEnd == value) return;
                m_SelectionEnd = value;
                OnPropertyChanged(() => SelectionEnd);
            }
        }

        private List<TreeNodeAndStreamDataLength> m_PlayStreamMarkers;
        public List<TreeNodeAndStreamDataLength> PlayStreamMarkers
        {
            get
            {
                return m_PlayStreamMarkers;
            }
            set
            {
                if (m_PlayStreamMarkers == value) return;
                if (value == null)
                {
                    m_PlayStreamMarkers.Clear();
                }
                m_PlayStreamMarkers = value;
                OnPropertyChanged(() => PlayStreamMarkers);
            }
        }

        private long m_DataLength;
        public long DataLength
        {
            get
            {
                return m_DataLength;
            }
            set
            {
                if (m_DataLength == value) return;
                m_DataLength = value;
                OnPropertyChanged(() => DataLength);
            }
        }

        private PCMFormatInfo m_PcmFormat;
        public PCMFormatInfo PcmFormat
        {
            get
            {
                return m_PcmFormat;
            }
            set
            {
                if (m_PcmFormat == value) return;
                m_PcmFormat = value;
                OnPropertyChanged(() => PcmFormat);
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
                    if (time >= 0)
                    {
                        AudioPlayer_PlayFrom(AudioPlayer_ConvertMillisecondsToBytes(time));
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
                OnPropertyChanged(() => LastPlayHeadTime);

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

        [NotifyDependsOn("PcmFormat")]
        public bool IsPlaying
        {
            get
            {
                return (PcmFormat != null && m_Player.State == AudioPlayerState.Playing);
            }
        }


        [NotifyDependsOn("IsAudioLoadedWithTreeNode")]
        [NotifyDependsOn("PlayStreamMarkers")]
        [NotifyDependsOn("CurrentTreeNode")]
        [NotifyDependsOn("CurrentSubTreeNode")]
        public bool IsAudioLoadedWithSubTreeNodes
        {
            get
            {
                return (IsAudioLoadedWithTreeNode && CurrentTreeNode != CurrentSubTreeNode && PlayStreamMarkers != null);
            }
        }


        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("CurrentTreeNode")]
        public bool IsAudioLoadedWithTreeNode
        {
            get
            {
                return (IsAudioLoaded && CurrentTreeNode != null);
            }
        }

        [NotifyDependsOn("FilePath")]
        [NotifyDependsOn("PcmFormat")]
        [NotifyDependsOn("CurrentTreeNode")]
        public bool IsAudioLoaded
        {
            get
            {
                return PcmFormat != null && (!String.IsNullOrEmpty(FilePath) || CurrentTreeNode != null)
                    && (View == null || View.BytesPerPixel != 0);
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

            m_EndOffsetOfPlayStream = DataLength;

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            PcmFormat = null;

            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(false);
            }

            PeakOverloadCountCh1 = 0;
            PeakOverloadCountCh2 = 0;

            if (PcmFormat == null)
            {
                m_PlayStream.Position = 0;
                m_PlayStream.Seek(0, SeekOrigin.Begin);

                if (!String.IsNullOrEmpty(FilePath))
                {
                    PcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_PlayStream);
                    m_StreamRiffHeaderEndPos = m_PlayStream.Position;
                }
                else
                {
                    PcmFormat = CurrentTreeNode.Presentation.MediaDataManager.DefaultPCMFormat.Copy();
                }
            }

            LastPlayHeadTime = 0;

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

            if (PlayStreamMarkers == null)
            {
                return;
            }

            TreeNode subTreeNode = null;

            //long byteOffset = PcmFormat.GetByteForTime(new Time(LastPlayHeadTime));
            long byteOffset = (long)Math.Round(AudioPlayer_ConvertMillisecondsToBytes(LastPlayHeadTime));

            long sumData = 0;
            long sumDataPrev = 0;
            foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
            {
                sumData += marker.m_LocalStreamDataLength;
                if (byteOffset <= sumData)
                {
                    subTreeNode = marker.m_TreeNode;
                    break;
                }
                sumDataPrev = sumData;
            }

            if (View != null)
            {
                View.RefreshUI_WaveFormChunkMarkers(sumDataPrev, sumData);
            }

            if (subTreeNode == null || (subTreeNode == CurrentSubTreeNode && subTreeNode != CurrentTreeNode))
            {
                return;
            }

            if (CurrentSubTreeNode != subTreeNode)
            {
                CurrentSubTreeNode = subTreeNode;
            }

            if (CurrentSubTreeNode != CurrentTreeNode)
            {
                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.updateWaveFormPlayHead", Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(CurrentSubTreeNode);
            }
        }

        public double AudioPlayer_ConvertBytesToMilliseconds(double bytes)
        {
            if (PcmFormat == null)
            {
                return 0;
            }
            return 1000.0 * bytes / ((double)PcmFormat.SampleRate * PcmFormat.NumberOfChannels * PcmFormat.BitDepth / 8.0);
        }

        public double AudioPlayer_ConvertMillisecondsToBytes(double ms)
        {
            if (PcmFormat == null)
            {
                return 0;
            }
            return (ms * PcmFormat.SampleRate * PcmFormat.NumberOfChannels * PcmFormat.BitDepth / 8.0) / 1000.0;
        }

        public void AudioPlayer_UpdateWaveFormPlayHead()
        {
            if (PcmFormat == null)
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

            if (!(PcmFormat != null && (!String.IsNullOrEmpty(FilePath) || CurrentTreeNode != null)))
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

            if (PcmFormat.BitDepth != 16)
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
                return;
            }
            // else: the stream is now open

            if (DataLength == 0)
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
                TimeDelta dur = PcmFormat.GetDuration(DataLength);
                m_Player.Play(m_CurrentAudioStreamProvider,
                              dur.TimeDeltaAsMillisecondDouble,
                              new AudioLibPCMFormat (PcmFormat.NumberOfChannels, PcmFormat.SampleRate, PcmFormat.BitDepth));
            }
        }

        /// <summary>
        /// If player exists and is playing, then pause. Otherwise if paused or stopped, then plays.
        /// </summary>
        public void AudioPlayer_TogglePlayPause()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_TogglePlayPause", Category.Debug, Priority.Medium);

            if (PcmFormat == null)
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

            if (PcmFormat == null)
            {
                return;
            }

            m_EndOffsetOfPlayStream = DataLength;

            if (m_Player.State == AudioPlayerState.Paused)
            {
                m_Player.Stop();
            }

            if (bytesEnd < 0)
            {
                if (m_Player.State == AudioPlayerState.Stopped)
                {
                    m_CurrentAudioStreamProvider(); // ensure m_PlayStream is open

                    m_EndOffsetOfPlayStream = DataLength;

                    m_Player.Play(m_CurrentAudioStreamProvider,
                                  PcmFormat.GetDuration(DataLength).TimeDeltaAsMillisecondDouble,
                                  new AudioLibPCMFormat( PcmFormat.NumberOfChannels,PcmFormat.SampleRate,PcmFormat.BitDepth),
                                  AudioPlayer_ConvertBytesToMilliseconds(bytesStart)
                        );
                }
                else if (m_Player.State == AudioPlayerState.Playing)
                {
                    m_Player.CurrentTimePosition = AudioPlayer_ConvertBytesToMilliseconds(bytesStart);
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
                              PcmFormat.GetDuration(DataLength).TimeDeltaAsMillisecondDouble,
                              new AudioLibPCMFormat(PcmFormat.NumberOfChannels, PcmFormat.SampleRate, PcmFormat.BitDepth),
                              AudioPlayer_ConvertBytesToMilliseconds(bytesStart),
                              AudioPlayer_ConvertBytesToMilliseconds(bytesEnd)
                    );
            }

            AudioPlayer_UpdateWaveFormPlayHead();
        }

        public void AudioPlayer_Play()
        {
            if (PcmFormat == null)
            {
                return;
            }

            AudioPlayer_Stop();

            Logger.Log("AudioPaneViewModel.AudioPlayer_Play", Category.Debug, Priority.Medium);

            double byteLastPlayHeadTime = AudioPlayer_ConvertMillisecondsToBytes(LastPlayHeadTime);

            if (!IsSelectionSet)
            {
                if (LastPlayHeadTime >=
                        AudioPlayer_ConvertBytesToMilliseconds(
                                            DataLength))
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
                double byteSelectionLeft = Math.Round(AudioPlayer_ConvertMillisecondsToBytes(SelectionBegin));
                double byteSelectionRight = Math.Round(AudioPlayer_ConvertMillisecondsToBytes(SelectionEnd));

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
            double from = AudioPlayer_ConvertBytesToMilliseconds(begin);
            double to = AudioPlayer_ConvertBytesToMilliseconds(end);

            var pcmInfo = PcmFormat;

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
                endPosition <= pcmInfo.GetDataLength(pcmInfo.GetDuration(DataLength)))
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
            if (PcmFormat == null)
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

            if (PcmFormat == null)
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
            Logger.Log("AudioPaneViewModel.AudioPlayer_Rewind", Category.Debug, Priority.Medium);

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
            Logger.Log("AudioPaneViewModel.AudioPlayer_FastForward", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();
            double newTime = LastPlayHeadTime + m_TimeStepForwardRewind;
            double max = AudioPlayer_ConvertBytesToMilliseconds(DataLength);
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

        public void AudioPlayer_StepBack()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_StepBack", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();

            long prev = -1;
            long begin = 0;

            var bytes = (long)Math.Round(AudioPlayer_ConvertMillisecondsToBytes(LastPlayHeadTime));

            bool moved = false;

            foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
            {
                long end = (begin + marker.m_LocalStreamDataLength);
                if (bytes >= begin && bytes <= end)
                {
                    if (prev == -1)
                    {
                        LastPlayHeadTime = AudioPlayer_ConvertBytesToMilliseconds(begin);
                        SystemSounds.Exclamation.Play();
                        moved = true;
                        break;
                    }

                    LastPlayHeadTime = AudioPlayer_ConvertBytesToMilliseconds(prev);
                    moved = true;
                    break;
                }
                prev = begin;
                begin += (marker.m_LocalStreamDataLength + 1);
            }

            if (moved && IsAutoPlay)
            {
                if (View != null)
                {
                    View.ClearSelection();
                    AudioPlayer_Play();
                }
            }
        }

        public void AudioPlayer_StepForward()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_StepForward", Category.Debug, Priority.Medium);

            if (PlayStreamMarkers == null || PlayStreamMarkers.Count ==0)
            {
                return;
            }

            AudioPlayer_Stop();

            long begin = 0;

            var bytes = (long) Math.Round(AudioPlayer_ConvertMillisecondsToBytes(LastPlayHeadTime));

            bool moved = false;
            bool found = false;

            foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
            {
                if (found)
                {
                    LastPlayHeadTime = AudioPlayer_ConvertBytesToMilliseconds(begin);
                    moved = true;
                    break;
                }

                long end = (begin + marker.m_LocalStreamDataLength);
                if (bytes >= begin && bytes <= end)
                {
                    found = true;
                }
                
                begin += (marker.m_LocalStreamDataLength + 1);
            }

            if (!found)
            {
                System.Diagnostics.Debugger.Break();
            }

            if (!moved)
            {
                SystemSounds.Exclamation.Play();
            }

            if (moved && IsAutoPlay)
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
            Logger.Log("AudioPaneViewModel.AudioPlayer_GotoEnd", Category.Debug, Priority.Medium);

            if (PlayStreamMarkers == null || PlayStreamMarkers.Count == 0)
            {
                return;
            }

            AudioPlayer_Stop();

            double end = AudioPlayer_ConvertBytesToMilliseconds(DataLength);

            if (LastPlayHeadTime == end)
            {
                SystemSounds.Exclamation.Play();
            }

            LastPlayHeadTime = end;
        }

        public void AudioPlayer_GotoBegining()
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_GotoBegining", Category.Debug, Priority.Medium);
            
            AudioPlayer_Stop();

            if (LastPlayHeadTime == 0)
            {
                SystemSounds.Exclamation.Play();
            }

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

        private void resetAllInternalValues()
        {
            Logger.Log("AudioPaneViewModel.resetAllInternalValues", Category.Debug, Priority.Medium);
            
            m_SkipTreeNodeSelectedEvent = false;
            
            CurrentTreeNode = null;
            CurrentSubTreeNode = null;
            
            PlayStreamMarkers = null;
            PcmFormat = null;
            DataLength = 0;
            
            ClearSelection();

            m_EndOffsetOfPlayStream = 0;
            FilePath = null;
            m_StreamRiffHeaderEndPos = 0;
            m_PlayStream = null;
            m_LastPlayHeadTime = 0;
        }

        public void AudioPlayer_LoadAndPlayFromFile(string path)
        {
            Logger.Log("AudioPaneViewModel.AudioPlayer_LoadAndPlayFromFile", Category.Debug, Priority.Medium);

            resetAllInternalValues();

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
                    PlayStreamMarkers = null;
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

                    DataLength = m_PlayStream.Length;
                }
                return m_PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                resetAllInternalValues();

                FilePath = null;
                return;
            }

            loadAndPlay();
        }

        [NotifyDependsOn("PcmFormat")]
        public String PcmFormatString
        {
            get
            {
                return (PcmFormat != null ? PcmFormat.ToString() : "");
            }
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

        #region Event / Callbacks

        private void OnEndOfAudioAsset(object sender, EndOfAudioAssetEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnEndOfAudioAsset", Category.Debug, Priority.Medium);

            OnPropertyChanged(() => IsPlaying);

            if (PcmFormat != null)
            {
                //double time = m_PcmFormat.GetDuration(m_DataLength).TimeDeltaAsMillisecondDouble;
                //long bytes = (long) m_Player.CurrentTimePosition;

                double time = PcmFormat.GetDuration(m_EndOffsetOfPlayStream).TimeDeltaAsMillisecondDouble;
                LastPlayHeadTime = time;
                //updateWaveFormPlayHead(time);
            }

            UpdatePeakMeter();

            if (!String.IsNullOrEmpty(FilePath) || CurrentTreeNode == null)
            {
                return;
            }

            if (m_EndOffsetOfPlayStream == DataLength && IsAutoPlay)
            {
                TreeNode nextNode = CurrentTreeNode.GetNextSiblingWithManagedAudio();
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
