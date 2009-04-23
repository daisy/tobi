using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;
using AudioLib;
using AudioLib.Events.Player;
using AudioLib.Events.VuMeter;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.timing;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// ViewModel for the AudioPane
    /// </summary>
    public partial class AudioPaneViewModel : ViewModelBase
    {
        #region Construction

        protected IUnityContainer Container { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public AudioPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator)
        {
            Container = container;
            EventAggregator = eventAggregator;

            Initialize();
        }

        #endregion Construction

        #region Initialization

        protected IAudioPaneView View { get; private set; }
        public void SetView(IAudioPaneView view)
        {
            View = view;
        }

        protected void Initialize()
        {
            InitializeAudioStuff();
            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
        }

        private void InitializeAudioStuff()
        {
            m_Player = new AudioPlayer();
            m_Player.SetDevice(GetWindowsFormsHookControl(), @"auto");
            m_Player.StateChanged += OnPlayerStateChanged;
            m_Player.EndOfAudioAsset += OnEndOfAudioAsset;
            LastPlayHeadTime = 0;

            m_Recorder = new AudioRecorder();
            m_Recorder.StateChanged += OnRecorderStateChanged;

            m_VuMeter = new VuMeter(m_Player, m_Recorder);
            m_VuMeter.UpdatePeakMeter += OnUpdateVuMeter;
            m_VuMeter.ResetEvent += OnResetVuMeter;
            m_VuMeter.PeakOverload += OnPeakOverload;

            PeakMeterBarDataCh1 = new PeakMeterBarData();
            PeakMeterBarDataCh2 = new PeakMeterBarData();
            PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;
            m_PeakMeterValues = new double[2];
        }

        #endregion Initialization

        #region WindowsFormsHookControl (required by DirectSound)

        // ReSharper disable RedundantDefaultFieldInitializer
        private System.Windows.Forms.Control m_WindowsFormsHookControl = null;
        // ReSharper restore RedundantDefaultFieldInitializer
        public System.Windows.Forms.Control GetWindowsFormsHookControl()
        {
            if (m_WindowsFormsHookControl == null)
            {
                m_WindowsFormsHookControl = new System.Windows.Forms.Control(@"Dummy visual needed by DirectSound");
            }

            return m_WindowsFormsHookControl;
        }

        #endregion WindowsFormsHookControl (required by DirectSound)

        #region Event / Callbacks

        private void OnTreeNodeSelected(TreeNode node)
        {
            if (node == null)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            resetWaveForm();

            m_CurrentTreeNode = node;
            m_CurrentSubTreeNode = node;

            m_CurrentAudioStreamProvider = () =>
            {
                if (m_CurrentTreeNode == null) return null;

                if (m_PlayStream == null)
                {
                    if (m_PlayStreamMarkers != null)
                    {
                        m_PlayStreamMarkers.Clear();
                        m_PlayStreamMarkers = null;
                    }

                    StreamWithMarkers? sm = m_CurrentTreeNode.GetManagedAudioDataFlattened();

                    if (sm == null)
                    {
                        TreeNode ancerstor = m_CurrentTreeNode.GetFirstAncestorWithManagedAudio();
                        if (ancerstor == null)
                        {
                            return null;
                        }

                        StreamWithMarkers? sma = ancerstor.GetManagedAudioData();
                        if (sma != null)
                        {
                            m_CurrentTreeNode = ancerstor;
                            m_PlayStream = sma.GetValueOrDefault().m_Stream;
                            m_PlayStreamMarkers = sma.GetValueOrDefault().m_SubStreamMarkers;
                        }
                    }
                    else
                    {
                        m_PlayStream = sm.GetValueOrDefault().m_Stream;
                        m_PlayStreamMarkers = sm.GetValueOrDefault().m_SubStreamMarkers;
                    }
                    if (m_PlayStream == null)
                    {
                        return null;
                    }
                    m_DataLength = m_PlayStream.Length;
                }
                return m_PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                m_CurrentTreeNode = null;
                m_CurrentSubTreeNode = null;
                return;
            }

            FilePath = "";

            loadAndPlay();
        }

        #endregion Event / Callbacks

        #region Private Class Attributes

        private TreeNode m_CurrentTreeNode;
        private TreeNode m_CurrentSubTreeNode;

        #endregion Private Class Attributes


        #region Public Properties

        private string m_WavFilePath;
        public string FilePath
        {
            get
            {
                return m_WavFilePath;
            }
            set
            {
                if (m_WavFilePath == value) return;
                m_WavFilePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        #endregion Public Properties

        #region WaveForm

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_ForcePlayAfterWaveFormLoaded = false;
        // ReSharper restore RedundantDefaultFieldInitializer
        private Stream m_PlayStream;
        private List<TreeNodeAndStreamDataLength> m_PlayStreamMarkers;
        private long m_DataLength;
        private PCMFormatInfo m_PcmFormat;
        private long m_StreamRiffHeaderEndPos;

        private DispatcherTimer m_PlaybackTimer;
        private DispatcherTimer m_WaveFormLoadTimer;

        public double ConvertByteToMilliseconds(double bytes)
        {
            if (m_PcmFormat == null)
            {
                return 0;
            }
            return 1000.0 * bytes / ((double)m_PcmFormat.SampleRate * m_PcmFormat.NumberOfChannels * m_PcmFormat.BitDepth / 8.0);
        }

        public double ConvertMillisecondsToByte(double ms)
        {
            if (m_PcmFormat == null)
            {
                return 0;
            }
            return (ms * m_PcmFormat.SampleRate * m_PcmFormat.NumberOfChannels * m_PcmFormat.BitDepth / 8.0) / 1000.0;
        }

        private void loadAndPlay()
        {
            if (m_CurrentAudioStreamProvider() == null)
            {
                return;
            }
            //else the stream is now open

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            m_PcmFormat = null;

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

            startWaveFormLoadTimer(10, true);
        }

        private void startWaveFormLoadTimer(double delay, bool play)
        {
            if (m_PcmFormat == null)
            {
                return;
            }

            m_ForcePlayAfterWaveFormLoaded = play;

            if (View != null)
            {
                View.RefreshUI_LoadingMessage(true);
            }

            if (m_WaveFormLoadTimer == null)
            {
                m_WaveFormLoadTimer = new DispatcherTimer(DispatcherPriority.Background);
                m_WaveFormLoadTimer.Tick += OnWaveFormLoadTimerTick;
                // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                if (delay == 0)
                // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                {
                    m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(0);//TODO: does this work ?? (immediate dispatch)
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

        private void stopWaveFormTimer()
        {
            if (m_PlaybackTimer != null && m_PlaybackTimer.IsEnabled)
            {
                m_PlaybackTimer.Stop();
            }
            m_PlaybackTimer = null;
        }
        private void startWaveFormTimer()
        {
            if (m_PlaybackTimer == null)
            {
                m_PlaybackTimer = new DispatcherTimer(DispatcherPriority.Send);
                m_PlaybackTimer.Tick += OnPlaybackTimerTick;

                double interval = 60;

                if (View != null)
                {
                    interval = ConvertByteToMilliseconds(View.BytesPerPixel);
                }

                if (interval < 60.0)
                {
                    interval = 60;
                }
                m_PlaybackTimer.Interval = TimeSpan.FromMilliseconds(interval);
            }
            else if (m_PlaybackTimer.IsEnabled)
            {
                return;
            }

            m_PlaybackTimer.Start();
        }
        private void updateWaveFormPlayHead(double time)
        {
            LastPlayHeadTime = time;

            if (View != null)
            {
                View.RefreshUI_WaveFormPlayHead();
            }

            if (m_PlayStreamMarkers == null)
            {
                return;
            }

            TreeNode subTreeNode = null;

            long byteOffset = GetPcmFormat().GetByteForTime(new Time(LastPlayHeadTime));

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

            m_CurrentSubTreeNode = subTreeNode;

            if (View != null)
            {
                View.RefreshUI_WaveFormChunkMarkers(sumDataPrev, sumData);
            }

            if (subTreeNode == null || (subTreeNode == m_CurrentSubTreeNode && subTreeNode != m_CurrentTreeNode))
            {
                return;
            }
            if (m_CurrentSubTreeNode != m_CurrentTreeNode)
            {
                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(m_CurrentSubTreeNode);
            }
        }

        private void updateWaveFormPlayHead()
        {
            if (m_PcmFormat == null)
            {
                return;
            }

            double time = LastPlayHeadTime;
            if (m_Player.State == AudioPlayerState.Playing)
            {
                time = m_Player.CurrentTimePosition;
            }
            updateWaveFormPlayHead(time);
        }

        private void resetWaveFormBackground()
        {
            if (View != null)
            {
                View.RefreshUI_WaveFormBackground();
            }
        }

        private void resetWaveForm()
        {
            resetWaveFormBackground();

            LastPlayHeadTime = 0;

            if (View != null)
            {
                View.RefreshUI_AllReset();
            }
        }

        private void loadWaveForm(bool play)
        {
            if (m_PcmFormat == null)
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
                return;
            }
            // else: the stream is now open, thus why we have a try/finally wrapper below:


            if (View != null)
            {
                View.RefreshUI_LoadWaveForm();
            }


            if (wasPlaying && play)
            {
                m_Player.Stop();
            }
            else
            {
                m_Player.Resume();
            }

            if (play)
            {
                TimeDelta dur = m_PcmFormat.GetDuration(m_DataLength);
                m_Player.Play(m_CurrentAudioStreamProvider,
                              dur,
                              m_PcmFormat);
            }
        }

        #region Event / Callbacks

        private void OnPlaybackTimerTick(object sender, EventArgs e)
        {
            updateWaveFormPlayHead();
        }

        private void OnWaveFormLoadTimerTick(object sender, EventArgs e)
        {
            if (View != null)
            {
                View.RefreshUI_LoadingMessage(true);
            }
            m_WaveFormLoadTimer.Stop();
            loadWaveForm(m_ForcePlayAfterWaveFormLoaded);
        }

        #endregion Event / Callbacks

        #endregion WaveForm

        #region Audio Recorder

        private AudioRecorder m_Recorder;

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnRecorderStateChanged(object sender, AudioLib.Events.Recorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            //m_Recorder.State == AudioLib.AudioRecorderState.Monitoring
        }

        #endregion Audio Recorder

        #region VuMeter / PeakMeter

        private VuMeter m_VuMeter;

        public PeakMeterBarData PeakMeterBarDataCh1 { get; set; }
        public PeakMeterBarData PeakMeterBarDataCh2 { get; set; }

        private double[] m_PeakMeterValues;
        private DispatcherTimer m_PeakMeterTimer;

        public int PeakOverloadCountCh1
        {
            get
            {
                return PeakMeterBarDataCh1.PeakOverloadCount;
            }
            set
            {
                if (PeakMeterBarDataCh1.PeakOverloadCount == value) return;
                PeakMeterBarDataCh1.PeakOverloadCount = value;
                OnPropertyChanged("PeakOverloadCountCh1");
            }
        }

        public int PeakOverloadCountCh2
        {
            get
            {
                return PeakMeterBarDataCh2.PeakOverloadCount;
            }
            set
            {
                if (PeakMeterBarDataCh2.PeakOverloadCount == value) return;
                PeakMeterBarDataCh2.PeakOverloadCount = value;
                OnPropertyChanged("PeakOverloadCountCh2");
            }
        }


        private void stopPeakMeterTimer()
        {
            if (m_PeakMeterTimer != null && m_PeakMeterTimer.IsEnabled)
            {
                m_PeakMeterTimer.Stop();
            }
            m_PeakMeterTimer = null;
        }

        private void startPeakMeterTimer()
        {
            if (m_PeakMeterTimer == null)
            {
                m_PeakMeterTimer = new DispatcherTimer(DispatcherPriority.Input);
                m_PeakMeterTimer.Tick += OnPeakMeterTimerTick;
                m_PeakMeterTimer.Interval = TimeSpan.FromMilliseconds(60);
            }
            else if (m_PeakMeterTimer.IsEnabled)
            {
                return;
            }

            m_PeakMeterTimer.Start();
        }


        private void resetPeakMeterValues()
        {
            PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            //PeakMeterBarDataCh1.ForceFullFallback();

            PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;
            //PeakMeterBarDataCh2.ForceFullFallback();

            m_PeakMeterValues[0] = PeakMeterBarDataCh1.ValueDb;
            m_PeakMeterValues[1] = PeakMeterBarDataCh2.ValueDb;

            updatePeakMeter();
        }


        public void updatePeakMeter()
        {
            if (m_PcmFormat == null || m_Player.State != AudioPlayerState.Playing)
            {
                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                }
                return;
            }
            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(false);

                PeakMeterBarDataCh1.ValueDb = m_PeakMeterValues[0];
                if (m_PcmFormat.NumberOfChannels > 1)
                {
                    PeakMeterBarDataCh2.ValueDb = m_PeakMeterValues[1];
                }

                View.RefreshUI_PeakMeter();
            }
        }

        #region Event / Callbacks

        private void OnResetVuMeter(object sender, ResetEventArgs e)
        {
            resetPeakMeterValues();

            updateWaveFormPlayHead();
        }

        private void OnUpdateVuMeter(object sender, UpdatePeakMeter e)
        {
            if (e.PeakValues != null && e.PeakValues.Length > 0)
            {
                m_PeakMeterValues[0] = e.PeakValues[0];
                if (m_PcmFormat.NumberOfChannels > 1)
                {
                    m_PeakMeterValues[1] = e.PeakValues[1];
                }
            }
        }

        private void OnPeakOverload(object sender, PeakOverloadEventArgs e)
        {
            if (e != null)
            {
                if (e.Channel == 1)
                {
                    PeakOverloadCountCh1++;
                }
                else if (e.Channel == 2)
                {
                    PeakOverloadCountCh2++;
                }
            }
        }


        private void OnPeakMeterTimerTick(object sender, EventArgs e)
        {
            updatePeakMeter();
        }

        #endregion Event / Callbacks

        #endregion VuMeter / PeakMeter

        #region Audio Player

        private AudioPlayer m_Player;
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;

        public double LastPlayHeadTime { get; private set; }

        /// <summary>
        /// If player exists and is playing, then pause. Otherwise if paused or stopped, then plays.
        /// </summary>
        public void AudioPlayer_TogglePlayPause()
        {
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
        /// If player exists, resumes or start playing at the given byte offset
        /// </summary>
        /// <param name="bytes"></param>
        public void AudioPlayer_PlayFromOffset(double bytes)
        {
            if (m_PcmFormat == null)
            {
                return;
            }

            if (m_Player.State == AudioPlayerState.Paused)
            {
                m_Player.Stop();
            }

            if (m_Player.State == AudioPlayerState.Stopped)
            {
                m_CurrentAudioStreamProvider(); // ensure m_PlayStream is open

                m_Player.Play(m_CurrentAudioStreamProvider,
                            m_PcmFormat.GetDuration(m_DataLength),
                            m_PcmFormat,
                            ConvertByteToMilliseconds(bytes)
                            );
            }
            else if (m_Player.State == AudioPlayerState.Playing)
            {
                m_Player.CurrentTimePosition = ConvertByteToMilliseconds(bytes);
            }
            updateWaveFormPlayHead();
        }

        /// <summary>
        /// If player exists and is ready but is not stopped, then stops.
        /// </summary>
        public void AudioPlayer_Stop()
        {
            if (m_PcmFormat == null)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
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

        public void AudioPlayer_LoadAndPlayFromFile(string path)
        {
            FilePath = path;

            resetWaveForm();

            m_CurrentTreeNode = null;
            m_CurrentSubTreeNode = null;

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
                }
                return m_PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                FilePath = "Could not get file stream !";
                return;
            }

            loadAndPlay();
        }


        #region Event / Callbacks

        private void OnEndOfAudioAsset(object sender, EndOfAudioAssetEventArgs e)
        {
            if (m_PcmFormat != null)
            {
                double time = m_PcmFormat.GetDuration(m_DataLength).TimeDeltaAsMillisecondDouble;
                updateWaveFormPlayHead(time);
            }

            updatePeakMeter();

            if (FilePath.Length > 0 || m_CurrentTreeNode == null)
            {
                return;
            }
            TreeNode nextNode = m_CurrentTreeNode.GetNextSiblingWithManagedAudio();
            if (nextNode != null)
            {
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
            }
        }

        private void OnPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                updatePeakMeter();
                m_PlayStream = null;
                stopWaveFormTimer();
                stopPeakMeterTimer();
            }
            if (m_Player.State == AudioPlayerState.Playing)
            {
                if (e.OldState == AudioPlayerState.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                updatePeakMeter();
                startWaveFormTimer();
                startPeakMeterTimer();
            }
        }

        #endregion Event / Callbacks


        #endregion Audio Player

        public void ReloadAfterSizeChanged()
        {
            updateWaveFormPlayHead();

            if (m_PcmFormat == null) //!e.WidthChanged || 
            {
                return;
            }
            startWaveFormLoadTimer(500, false);
        }

        #region General stuff



        #endregion General stuff

        public PCMFormatInfo GetPcmFormat()
        {
            return m_PcmFormat;
        }

        public Stream GetPlayStream()
        {
            return m_PlayStream;
        }

        public void ClosePlayStream()
        {
            m_PlayStream.Close();
            m_PlayStream = null;
        }

        public void ResetPlayStreamPosition()
        {
            m_PlayStream.Position = m_StreamRiffHeaderEndPos;
            m_PlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
        }

        public List<TreeNodeAndStreamDataLength> GetPlayStreamMarkers()
        {
            return m_PlayStreamMarkers;
        }

        public long GetDataLength()
        {
            return m_DataLength;
        }
    }
}
