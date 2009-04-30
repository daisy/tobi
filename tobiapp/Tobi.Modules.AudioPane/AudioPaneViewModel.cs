using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AudioLib;
using AudioLib.Events.Player;
using AudioLib.Events.VuMeter;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
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
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public AudioPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;

            Initialize();
        }

        #endregion Construction

        #region Commands

        public RichDelegateCommand<object> CommandOpenFile { get; private set; }

        private void initializeCommands()
        {
            Logger.Log("AudioPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandOpenFile = new RichDelegateCommand<object>(null,
                UserInterfaceStrings.Audio_OpenFile,
                new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift),
                (VisualBrush)Application.Current.FindResource("document-open"),
                CommandOpenFile_Executed, obj => true);

            shellPresenter.AddInputBinding(CommandOpenFile.KeyBinding);
        }

        public void OpenFile()
        {
            Logger.Log("AudioPaneViewModel.OpenFile", Category.Debug, Priority.Medium);

            AudioPlayer_TogglePlayPause();

            string str = null;

            if (View != null)
            {
                str = View.OpenFileDialog();
            }
            else
            {
                str = null;
                return;
            }

            AudioPlayer_Stop();
            AudioPlayer_LoadAndPlayFromFile(str);
        }

        private void CommandOpenFile_Executed(object obj)
        {
            OpenFile();
        }

        #endregion Commands

        #region Initialization

        protected IAudioPaneView View { get; private set; }
        public void SetView(IAudioPaneView view)
        {
            View = view;
        }

        protected void Initialize()
        {
            initializeCommands();
            initializeAudioStuff();
            //EventAggregator.GetEvent<UserInterfaceScaledEvent>().Subscribe(OnUserInterfaceScaled, ThreadOption.UIThread);
            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
        }

        private void initializeAudioStuff()
        {
            Logger.Log("AudioPaneViewModel.initializeAudioStuff", Category.Debug, Priority.Medium);

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
                Logger.Log("AudioPaneViewModel.GetWindowsFormsHookControl", Category.Debug, Priority.Medium);

                m_WindowsFormsHookControl = new System.Windows.Forms.Control(@"Dummy visual needed by DirectSound");
            }

            return m_WindowsFormsHookControl;
        }

        #endregion WindowsFormsHookControl (required by DirectSound)

        #region Event / Callbacks

        private void OnTreeNodeSelected(TreeNode node)
        {
            Logger.Log("AudioPaneViewModel.OnTreeNodeSelected", Category.Debug, Priority.Medium);

            resetAllInternalPlayerValues();

            OnPropertyChanged("IsAudioLoaded");
            OnPropertyChanged("IsAudioLoadedWithTreeNode");
            OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

            if (node == null)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
                OnPropertyChanged("IsPlaying");
            }

            LastPlayHeadTime = 0;

            if (View != null)
            {
                View.RefreshUI_WaveFormBackground();

                View.RefreshUI_AllReset();
            }

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

                    OnPropertyChanged("IsAudioLoaded");
                    OnPropertyChanged("IsAudioLoadedWithTreeNode");
                    OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");
                }
                return m_PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                resetAllInternalPlayerValues();

                OnPropertyChanged("IsAudioLoaded");
                OnPropertyChanged("IsAudioLoadedWithTreeNode");
                OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

                return;
            }

            m_EndOffsetOfPlayStream = m_DataLength;

            FilePath = "";

            OnPropertyChanged("IsAudioLoaded");
            OnPropertyChanged("IsAudioLoadedWithTreeNode");
            OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

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

        public bool IsSelectionSet
        {
            get
            {
                if (View != null)
                {
                    return View.IsSelectionSet;
                }
                return false;
            }
            set
            {
                OnPropertyChanged("IsSelectionSet");
            }
        }

        public bool ResizeDrag
        {
            get
            {
                var shell = Container.Resolve<IShellView>();
                if (shell != null)
                {
                    return shell.SplitterDrag;
                }
                return false;
            }
        }

        #endregion Public Properties

        #region Audio Recorder

        private AudioRecorder m_Recorder;

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnRecorderStateChanged(object sender, AudioLib.Events.Recorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            //m_Recorder.State == AudioLib.AudioRecorderState.Monitoring
        }

        public void AudioRecorder_Start()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Start", Category.Debug, Priority.Medium);
        }

        public void AudioRecorder_Stop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Stop", Category.Debug, Priority.Medium);
        }

        #endregion Audio Recorder

        #region VuMeter / PeakMeter

        private VuMeter m_VuMeter;

        public PeakMeterBarData PeakMeterBarDataCh1 { get; set; }
        public PeakMeterBarData PeakMeterBarDataCh2 { get; set; }

        private double[] m_PeakMeterValues;

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

        public void UpdatePeakMeter()
        {
            if (m_PcmFormat == null || m_Player.State != AudioPlayerState.Playing)
            {
                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                }
                return;
            }
            PeakMeterBarDataCh1.ValueDb = m_PeakMeterValues[0];
            if (m_PcmFormat.NumberOfChannels > 1)
            {
                PeakMeterBarDataCh2.ValueDb = m_PeakMeterValues[1];
            }

            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(false);

                View.RefreshUI_PeakMeter();
            }
        }

        #region Event / Callbacks

        private void OnResetVuMeter(object sender, ResetEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnResetVuMeter", Category.Debug, Priority.Medium);

            PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            //PeakMeterBarDataCh1.ForceFullFallback();

            PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;
            //PeakMeterBarDataCh2.ForceFullFallback();

            m_PeakMeterValues[0] = PeakMeterBarDataCh1.ValueDb;
            m_PeakMeterValues[1] = PeakMeterBarDataCh2.ValueDb;

            UpdatePeakMeter();

            AudioPlayer_UpdateWaveFormPlayHead();
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

        #endregion Event / Callbacks

        #endregion VuMeter / PeakMeter

        #region Audio Player

        private Stream m_PlayStream;
        private List<TreeNodeAndStreamDataLength> m_PlayStreamMarkers;
        private long m_DataLength;
        private PCMFormatInfo m_PcmFormat;
        private long m_StreamRiffHeaderEndPos;

        private AudioPlayer m_Player;
        private AudioPlayer.StreamProviderDelegate m_CurrentAudioStreamProvider;

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

                OnPropertyChanged("IsAutoPlay");
            }
        }

        public bool IsRecording
        {
            get
            {
                return false;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return (m_PcmFormat != null && m_Player.State != AudioPlayerState.NotReady && m_Player.State == AudioPlayerState.Playing);
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
                return (m_PcmFormat != null);
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
                OnPropertyChanged("IsPlaying");
            }

            m_PcmFormat = null;
            OnPropertyChanged("IsAudioLoaded");

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

            OnPropertyChanged("IsAudioLoaded");
            OnPropertyChanged("IsAudioLoadedWithTreeNode");
            OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

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

            m_CurrentSubTreeNode = subTreeNode;
            OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

            if (m_CurrentSubTreeNode != m_CurrentTreeNode)
            {
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
                    OnPropertyChanged("IsPlaying");
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
                OnPropertyChanged("IsAudioLoaded");
                OnPropertyChanged("IsAudioLoadedWithTreeNode");
                OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

                return;
            }
            // else: the stream is now open

            if (View != null)
            {
                View.RefreshUI_LoadWaveForm();
            }

            if (wasPlaying)
            {
                if (!play)
                {
                    m_Player.Resume();
                    OnPropertyChanged("IsPlaying");
                    return;
                }
                m_Player.Stop();
                OnPropertyChanged("IsPlaying");
            }

            if (play)
            {
                TimeDelta dur = m_PcmFormat.GetDuration(m_DataLength);
                m_Player.Play(m_CurrentAudioStreamProvider,
                              dur,
                              m_PcmFormat);

                OnPropertyChanged("IsPlaying");
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
                OnPropertyChanged("IsPlaying");
            }
            else if (m_Player.State == AudioPlayerState.Paused || m_Player.State == AudioPlayerState.Stopped)
            {
                m_Player.Resume();
                OnPropertyChanged("IsPlaying");
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
                OnPropertyChanged("IsPlaying");
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
                    OnPropertyChanged("IsPlaying");
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
                    OnPropertyChanged("IsPlaying");
                }

                m_CurrentAudioStreamProvider(); // ensure m_PlayStream is open

                m_Player.Play(m_CurrentAudioStreamProvider,
                              m_PcmFormat.GetDuration(m_DataLength),
                              m_PcmFormat,
                              AudioPlayer_ConvertByteToMilliseconds(bytesStart),
                              AudioPlayer_ConvertByteToMilliseconds(bytesEnd)
                    );
                OnPropertyChanged("IsPlaying");
            }

            AudioPlayer_UpdateWaveFormPlayHead();
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
                OnPropertyChanged("IsPlaying");
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

                    OnPropertyChanged("IsAudioLoaded");
                    OnPropertyChanged("IsAudioLoadedWithTreeNode");
                    OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");
                }
                return m_PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                resetAllInternalPlayerValues();

                OnPropertyChanged("IsAudioLoaded");
                OnPropertyChanged("IsAudioLoadedWithTreeNode");
                OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

                FilePath = "Could not get file stream !";
                return;
            }

            OnPropertyChanged("IsAudioLoaded");
            OnPropertyChanged("IsAudioLoadedWithTreeNode");
            OnPropertyChanged("IsAudioLoadedWithSubTreeNodes");

            loadAndPlay();
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

            OnPropertyChanged("IsPlaying");

            if (m_PcmFormat != null)
            {
                //double time = m_PcmFormat.GetDuration(m_DataLength).TimeDeltaAsMillisecondDouble;
                //long bytes = (long) m_Player.CurrentTimePosition;

                double time = m_PcmFormat.GetDuration(m_EndOffsetOfPlayStream).TimeDeltaAsMillisecondDouble;
                LastPlayHeadTime = time;
                //updateWaveFormPlayHead(time);
            }

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
                    EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                }
            }
        }

        private void OnPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnPlayerStateChanged", Category.Debug, Priority.Medium);

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
