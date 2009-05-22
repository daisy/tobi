using System;
using System.IO;
using AudioLib;
using AudioLib.Events.VuMeter;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using urakawa;
using urakawa.core;

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
            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
        }

        private void setRecordingDirectory(string path)
        {
            m_Recorder.AssetsDirectory = path;
            if (!Directory.Exists(m_Recorder.AssetsDirectory))
            {
                Directory.CreateDirectory(m_Recorder.AssetsDirectory);
            }
        }

        private void OnProjectLoaded(Project project)
        {
            //var shell = Container.Resolve<IShellPresenter>();
            //shell.DocumentProject
            setRecordingDirectory(project.GetPresentation(0).DataProviderManager.DataFileDirectoryFullPath);
        }

        private void initializeAudioStuff()
        {
            Logger.Log("AudioPaneViewModel.initializeAudioStuff", Category.Debug, Priority.Medium);

            m_Player = new AudioPlayer();
            m_Player.SetDevice(GetWindowsFormsHookControl(), @"fakename");
            m_Player.StateChanged += OnPlayerStateChanged;
            m_Player.EndOfAudioAsset += OnEndOfAudioAsset;
            LastPlayHeadTime = 0;

            m_Recorder = new AudioRecorder();
            m_Recorder.SetDevice(@"fakename");
            m_Recorder.StateChanged += OnRecorderStateChanged;
            //m_Recorder.UpdateVuMeterFromRecorder += OnUpdateVuMeterFromRecorder;

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

        private bool m_SkipTreeNodeSelectedEvent = false;

        private void OnTreeNodeSelected(TreeNode node)
        {
            Logger.Log("AudioPaneViewModel.OnTreeNodeSelected", Category.Debug, Priority.Medium);

            if (node == null)
            {
                return;
            }

            if (m_SkipTreeNodeSelectedEvent)
            {
                m_SkipTreeNodeSelectedEvent = false;
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            resetAllInternalValues();

            LastPlayHeadTime = 0;

            if (View != null)
            {
                View.RefreshUI_WaveFormBackground();

                View.RefreshUI_AllReset();
            }

            CurrentTreeNode = node;
            CurrentSubTreeNode = node;

            m_CurrentAudioStreamProvider = () =>
            {
                if (CurrentTreeNode == null) return null;

                if (m_PlayStream == null)
                {
                    PlayStreamMarkers = null;

                    StreamWithMarkers? sm = CurrentTreeNode.GetManagedAudioDataFlattened();

                    if (sm == null)
                    {
                        TreeNode ancerstor = CurrentTreeNode.GetFirstAncestorWithManagedAudio();
                        if (ancerstor == null)
                        {
                            return null;
                        }

                        StreamWithMarkers? sma = ancerstor.GetManagedAudioData();
                        if (sma != null)
                        {
                            CurrentSubTreeNode = CurrentTreeNode;
                            CurrentTreeNode = ancerstor;

                            m_SkipTreeNodeSelectedEvent = true;
                            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(CurrentTreeNode);
                            EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(CurrentSubTreeNode);

                            m_PlayStream = sma.GetValueOrDefault().m_Stream;
                            PlayStreamMarkers = sma.GetValueOrDefault().m_SubStreamMarkers;
                        }
                    }
                    else
                    {
                        m_PlayStream = sm.GetValueOrDefault().m_Stream;
                        PlayStreamMarkers = sm.GetValueOrDefault().m_SubStreamMarkers;
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

                return;
            }

            m_EndOffsetOfPlayStream = DataLength;

            FilePath = null;

            loadAndPlay();
        }

        #endregion Event / Callbacks

        #region Private Class Attributes

        #endregion Private Class Attributes

        #region Public Properties

        private TreeNode m_CurrentTreeNode;
        public TreeNode CurrentTreeNode
        {
            get
            {
                return m_CurrentTreeNode;
            }
            set
            {
                if (m_CurrentTreeNode == value) return;
                m_CurrentTreeNode = value;
                OnPropertyChanged(() => CurrentTreeNode);
            }
        }

        private TreeNode m_CurrentSubTreeNode;
        public TreeNode CurrentSubTreeNode
        {
            get
            {
                return m_CurrentSubTreeNode;
            }
            set
            {
                if (m_CurrentSubTreeNode == value) return;
                m_CurrentSubTreeNode = value;
                OnPropertyChanged(() => CurrentSubTreeNode);
            }
        }

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
                OnPropertyChanged(() => FilePath);
            }
        }

        public void Refresh()
        {
            if (View != null)
            {
                View.Refresh();
            }
        }

        public void ZoomFitFull()
        {
            if (View != null)
            {
                View.ZoomFitFull();
            }
        }

        public void ZoomSelection()
        {
            if (View != null)
            {
                View.ZoomSelection();
            }
        }

        public void ClearSelection()
        {
            SelectionBegin = -1.0;
            SelectionEnd = -1.0;
            if (View != null)
            {
                View.ClearSelection();
            }
        }

        public void SelectAll()
        {
            SelectionBegin = 0;
            SelectionEnd = AudioPlayer_ConvertByteToMilliseconds(DataLength);
            if (View != null)
            {
                View.ExpandSelection();
            }
        }

        public void OpenFile(String str)
        {
            Logger.Log("AudioPaneViewModel.OpenFile", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();

            string filePath = str;

            if (filePath == null && View != null)
            {
                filePath = View.OpenFileDialog();
            }

            if (filePath == null)
            {
                return;
            }

            AudioPlayer_LoadAndPlayFromFile(filePath);
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

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("DataLength")]
        public string TimeStringTotalWaveform
        {
            get
            {
                if (! IsAudioLoaded)
                {
                    return "";
                }
                var timeSpan = TimeSpan.FromMilliseconds(AudioPlayer_ConvertByteToMilliseconds(DataLength));
                return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
            }
        }

        public double RecorderCurrentDuration
        {
            get
            {
                return m_Recorder.TimeOfAsset;
            }
        }

        // TODO: LastPlayHeadTime and RecorderCurrentDuration(triggered from OnUpdateVuMeter)
        // refresh many times per seconds, which floods the data binding infrastructure with INotifiedPropertyChanged events.
        // We might need to optimize this part in the near future, to avoid performance issues.
        [NotifyDependsOn("PcmFormat")]
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("LastPlayHeadTime")]
        [NotifyDependsOn("RecorderCurrentDuration")]
        public String TimeStringCurrent
        {
            get
            {
                if (PcmFormat == null)
                {
                    return "";
                }

                if (IsRecording || IsMonitoring)
                {
                    var timeSpan = TimeSpan.FromMilliseconds(RecorderCurrentDuration);
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                }
                
                if (IsPlaying)
                {
                    var timeSpan = TimeSpan.FromMilliseconds(m_Player.CurrentTimePosition);
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                }
                
                if (m_Player.State == AudioPlayerState.Paused || m_Player.State == AudioPlayerState.Stopped)
                {
                    var timeSpan = TimeSpan.FromMilliseconds(LastPlayHeadTime);
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                }

                return "";
            }
        }

        #endregion Public Properties

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
                OnPropertyChanged(() => PeakOverloadCountCh1);
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
                OnPropertyChanged(() => PeakOverloadCountCh2);
            }
        }

        public void UpdatePeakMeter()
        {
            if (PcmFormat == null || m_Player.State != AudioPlayerState.Playing
                && (m_Recorder.State != AudioRecorderState.Recording && m_Recorder.State != AudioRecorderState.Monitoring))
            {
                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                }
                return;
            }
            PeakMeterBarDataCh1.ValueDb = m_PeakMeterValues[0];
            if (PcmFormat.NumberOfChannels > 1)
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
            if (IsRecording || IsMonitoring)
            {
                if (View != null)
                {
                    View.RefreshUI_TimeMessageInvalidate();
                }

                // TODO: generates too many events per seconds in the data binding pipeline (INotifyPropertyChanged)
                OnPropertyChanged(() => RecorderCurrentDuration);
            }

            if (e.PeakValues != null && e.PeakValues.Length > 0)
            {
                m_PeakMeterValues[0] = e.PeakValues[0];
                if (PcmFormat != null && PcmFormat.NumberOfChannels > 1)
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
    }
}
