using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.core;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.media.timing;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// ViewModel for the AudioPane
    /// </summary>
    public partial class AudioPaneViewModel : ViewModelBase
    {
        #region Construction

        protected IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public AudioPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
            : base(container)
        {
            EventAggregator = eventAggregator;
            Logger = logger;

            Initialize();
        }

        ~AudioPaneViewModel()
        {
#if DEBUG
            Logger.Log("AudioPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
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
            State = new StateData(this, this);

            initializeCommands();

            // Removed because called too many times.
            //PropertyChanged += OnViewModelPropertyChanged;

            initializeAudioStuff();

            State.ResetAll();

            m_LastPlayHeadTime = -1;
            IsWaveFormLoading = false;

            //EventAggregator.GetEvent<UserInterfaceScaledEvent>().Subscribe(OnUserInterfaceScaled, ThreadOption.UIThread);

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);

            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
            EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, ThreadOption.UIThread);
        }

        private void initializeAudioStuff()
        {
            Logger.Log("AudioPaneViewModel.initializeAudioStuff", Category.Debug, Priority.Medium);

            m_Player = new AudioPlayer(AudioPlaybackStreamKeepAlive);
            m_Player.SetDevice(GetWindowsFormsHookControl(), @"fakename");
            m_Player.StateChanged += OnStateChanged_Player;
            m_Player.AudioPlaybackFinished += OnAudioPlaybackFinished;
            
            //m_Player.ResetVuMeter += OnPlayerResetVuMeter;

            m_Recorder = new AudioRecorder();
            m_Recorder.SetDevice(@"fakename");
            m_Recorder.StateChanged += OnStateChanged_Recorder;
            
            //m_Recorder.ResetVuMeter += OnRecorderResetVuMeter;

            m_VuMeter = new VuMeter(m_Player, m_Recorder);
            m_VuMeter.PeakMeterUpdated += OnPeakMeterUpdated;
            m_VuMeter.PeakMeterOverloaded += OnPeakMeterOverloaded;
            
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

        //private bool m_SkipTreeNodeSelectedEvent = false;

        private void OnSubTreeNodeSelected(TreeNode node)
        {
            Logger.Log("AudioPaneViewModel.OnSubTreeNodeSelected", Category.Debug, Priority.Medium);

            if (node == null || State.CurrentTreeNode == null)
            {
                return;
            }
            if (State.CurrentSubTreeNode == node)
            {
                return;
            }
            if (!IsAudioLoadedWithSubTreeNodes)
            {
                return;
            }
            if (!node.IsDescendantOf(State.CurrentTreeNode))
            {
                return;
            }

            if (m_Player.CurrentState != AudioPlayer.State.NotReady && m_Player.CurrentState != AudioPlayer.State.Stopped)
            {
                m_Player.Stop();
            }

            State.CurrentSubTreeNode = node;

            long sumData = 0;
            long sumDataPrev = 0;
            foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
            {
                sumDataPrev = sumData;
                if (State.CurrentSubTreeNode == marker.m_TreeNode)
                {
                    sumData += marker.m_LocalStreamDataLength;

                    LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(sumDataPrev);

                    if (View != null)
                    {
                        View.RefreshUI_WaveFormChunkMarkers(sumDataPrev, sumData);
                    }
                    break;
                }
                sumData += marker.m_LocalStreamDataLength;
            }
        }

        private void OnTreeNodeSelected(TreeNode node)
        {
            Logger.Log("AudioPaneViewModel.OnTreeNodeSelected", Category.Debug, Priority.Medium);

            if (node == null)
            {
                return;
            }

            if (State.CurrentTreeNode == node)
            {
                return;
            }

            /*if (m_SkipTreeNodeSelectedEvent)
            {
                m_SkipTreeNodeSelectedEvent = false;
                return;
            }*/

            if (m_Player.CurrentState != AudioPlayer.State.NotReady && m_Player.CurrentState != AudioPlayer.State.Stopped)
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

            State.CurrentTreeNode = node;

            m_CurrentAudioStreamProvider = () =>
            {
                if (State.CurrentTreeNode == null) return null;

                if (State.Audio.PlayStream == null)
                {
                    StreamWithMarkers? sm = State.CurrentTreeNode.GetManagedAudioDataFlattened();

                    if (sm == null)
                    {
                        TreeNode ancerstor = State.CurrentTreeNode.GetFirstAncestorWithManagedAudio();
                        if (ancerstor != null)
                        {
                            StreamWithMarkers? sma = ancerstor.GetManagedAudioData();
                            if (sma != null)
                            {
                                TreeNode theCurrentSubTreeNode = State.CurrentTreeNode;
                                State.CurrentTreeNode = ancerstor;

                                State.Audio.PlayStream = sma.GetValueOrDefault().m_Stream;
                                State.Audio.PlayStreamMarkers = sma.GetValueOrDefault().m_SubStreamMarkers;

                                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.m_CurrentAudioStreamProvider", Category.Debug, Priority.Medium);

                                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(State.CurrentTreeNode);

                                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.m_CurrentAudioStreamProvider", Category.Debug, Priority.Medium);

                                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(theCurrentSubTreeNode);
                            }
                        }
                    }
                    else
                    {
                        State.Audio.PlayStream = sm.GetValueOrDefault().m_Stream;
                        State.Audio.PlayStreamMarkers = sm.GetValueOrDefault().m_SubStreamMarkers;
                    }

                    if (State.Audio.PlayStream == null)
                    {
                        //State.ResetAll();
                        //m_LastPlayHeadTime = -1;
                        //IsWaveFormLoading = false;
                        return null;
                    }

                    //if (State.Audio.PlayStreamMarkers.Count == 1)
                    //{
                    //    State.CurrentSubTreeNode = State.CurrentTreeNode;
                    //}
                }

                return State.Audio.PlayStream;
            };

            if (m_CurrentAudioStreamProvider() == null)
            {
                return;
            }

            //m_LastPlayHeadTime = 0; Set after waveform loaded

            loadAndPlay();
        }


        //private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName.StartsWith("Can"))
        //    {
        //        Logger.Log("@@ AudioPaneViewModel.OnViewModelPropertyChanged: [" + e.PropertyName + "]", Category.Debug, Priority.High);

        //        CommandManager.InvalidateRequerySuggested();
        //    }
        //}

        private void OnProjectUnLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.Changed -= OnUndoRedoManagerChanged;
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            State.ResetAll();

            m_LastPlayHeadTime = -1;
            IsWaveFormLoading = false;

            //var shell = Container.Resolve<IShellPresenter>();
            //shell.DocumentProject
            if (project != null)
            {
                project.Presentations.Get(0).UndoRedoManager.Changed += OnUndoRedoManagerChanged;
                setRecordingDirectory(project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath);
            }
        }

        #endregion Event / Callbacks

        #region Private Class Attributes

        private const bool AudioPlaybackStreamKeepAlive = true;

        struct StateToRestore
        {
            public double SelectionBegin;
            public double SelectionEnd;
            public double LastPlayHeadTime;
        }
        private StateToRestore? m_StateToRestore = null;

        private double getTimeOffset(TreeNode treeNode, ManagedAudioMedia managedMedia)
        {
            if (!State.IsTreeNodeShownInAudioWaveForm(treeNode))
            {
                return 0;
            }

            double timeOffset = 0;

            if (State.CurrentTreeNode != treeNode)
            {
                foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                {
                    if (marker.m_TreeNode == treeNode) break;

                    timeOffset += State.Audio.ConvertBytesToMilliseconds(marker.m_LocalStreamDataLength);
                }
            }

            if (managedMedia == null)
            {
                return timeOffset;
            }

            ManagedAudioMedia managedAudioMedia = treeNode.GetManagedAudioMedia();
            if (managedAudioMedia == null)
            {
                SequenceMedia seqAudioMedia = treeNode.GetAudioSequenceMedia();
                bool isSeqValid = seqAudioMedia != null && !seqAudioMedia.AllowMultipleTypes;
                if (isSeqValid)
                {
                    foreach (Media media in seqAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
                    {
                        if (!(media is ManagedAudioMedia))
                        {
                            isSeqValid = false;
                            break;
                        }
                    }
                }
                if (isSeqValid)
                {
                    foreach (Media media in seqAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
                    {
                        var manMedia = (ManagedAudioMedia)media;
                        if (media == managedMedia)
                        {
                            break;
                        }
                        timeOffset += manMedia.Duration.TimeDeltaAsMillisecondDouble;
                    }
                }
            }

            return timeOffset;
        }

        #endregion Private Class Attributes

        #region Public Properties

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
                if (!IsAudioLoaded)
                {
                    return "";
                }
                var timeSpan = TimeSpan.FromMilliseconds(State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength));
                return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
            }
        }

        private static readonly PropertyChangedEventArgs m_RecorderCurrentDurationArgs
            = new PropertyChangedEventArgs("RecorderCurrentDuration");
        public double RecorderCurrentDuration
        {
            get
            {
                return m_Recorder.TimeOfAsset;
            }
        }

        private static readonly PropertyChangedEventArgs m_TimeStringCurrentArgs
            = new PropertyChangedEventArgs("TimeStringCurrent");
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOnFast("m_LastPlayHeadTimeArgs", "m_TimeStringCurrentArgs")]
        [NotifyDependsOnFast("m_RecorderCurrentDurationArgs", "m_TimeStringCurrentArgs")]
        public String TimeStringCurrent
        {
            get
            {
                if (IsRecording || IsMonitoring)
                {
                    var timeSpan = TimeSpan.FromMilliseconds(RecorderCurrentDuration);
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                }

                if (!IsAudioLoaded)
                {
                    return "";
                }

                if (IsPlaying)
                {
                    var timeSpan = TimeSpan.FromMilliseconds(m_Player.CurrentTime);
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                }

                if (m_Player.CurrentState == AudioPlayer.State.Paused || m_Player.CurrentState == AudioPlayer.State.Stopped)
                {
                    var timeSpan = TimeSpan.FromMilliseconds(LastPlayHeadTime);
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                }

                return "";
            }
        }

        private static readonly PropertyChangedEventArgs m_LastPlayHeadTimeArgs
            = new PropertyChangedEventArgs("LastPlayHeadTime");
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

                RaisePropertyChanged(m_LastPlayHeadTimeArgs);
                //RaisePropertyChanged(() => LastPlayHeadTime);

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
                long byteOffset = State.Audio.ConvertMillisecondsToBytes(m_LastPlayHeadTime);

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
                RaisePropertyChanged(() => PeakOverloadCountCh1);
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
                RaisePropertyChanged(() => PeakOverloadCountCh2);
            }
        }

        public void UpdatePeakMeter()
        {
            if (m_Player.CurrentState != AudioPlayer.State.Playing
                && (m_Recorder.CurrentState != AudioRecorder.State.Recording && m_Recorder.CurrentState != AudioRecorder.State.Monitoring))
            {
                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                }
                return;
            }
            PeakMeterBarDataCh1.ValueDb = m_PeakMeterValues[0];
            PCMFormatInfo pcm = State.Audio.PcmFormat;
            if (pcm == null)
            {
                pcm = m_PcmFormatOfAudioToInsert;
            }
            if (pcm.NumberOfChannels > 1)
            {
                PeakMeterBarDataCh2.ValueDb = m_PeakMeterValues[1];
            }

            if (View != null)
            {
                View.RefreshUI_PeakMeterBlackout(false);

                View.RefreshUI_PeakMeter();
            }
        }

        private void resetPeakMeter()
        {
            Logger.Log("AudioPaneViewModel.resetPeakMeter", Category.Debug, Priority.Medium);

            PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            //PeakMeterBarDataCh1.ForceFullFallback();

            PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;
            //PeakMeterBarDataCh2.ForceFullFallback();

            m_PeakMeterValues[0] = PeakMeterBarDataCh1.ValueDb;
            m_PeakMeterValues[1] = PeakMeterBarDataCh2.ValueDb;

            UpdatePeakMeter();

            bool oldVal = IsAutoPlay;
            IsAutoPlay = false;
            AudioPlayer_UpdateWaveFormPlayHead();
            IsAutoPlay = oldVal;
        }

        #region Event / Callbacks

        private void OnPeakMeterUpdated(object sender, VuMeter.PeakMeterUpdateEventArgs e)
        {
            if (IsRecording || IsMonitoring)
            {
                if (View != null)
                {
                    View.TimeMessageRefresh();
                }

                RaisePropertyChanged(m_RecorderCurrentDurationArgs);
                //RaisePropertyChanged(() => RecorderCurrentDuration);
            }

            if (e.PeakDb != null && e.PeakDb.Length > 0)
            {
                m_PeakMeterValues[0] = e.PeakDb[0];
                PCMFormatInfo pcmInfo = State.Audio.PcmFormat;
                if (pcmInfo == null)
                {
                    pcmInfo = m_PcmFormatOfAudioToInsert;
                }
                if (pcmInfo.NumberOfChannels > 1)
                {
                    m_PeakMeterValues[1] = e.PeakDb[1];
                }
                UpdatePeakMeter();
            }
        }

        private void OnPeakMeterOverloaded(object sender, VuMeter.PeakOverloadEventArgs e)
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

        private ManagedAudioMedia makeManagedAudioMediaFromFile(string filePath)
        {
            ManagedAudioMedia managedAudioMediaNew = null;

            TreeNode nodeRecord = (State.CurrentSubTreeNode ?? State.CurrentTreeNode);

            Stream recordingStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                //unused, but necessary to reach byte offset after RIFF header
                PCMDataInfo pcmFormat = PCMDataInfo.ParseRiffWaveHeader(recordingStream);
                long dataLength = recordingStream.Length - recordingStream.Position;

                //double recordingDuration = pcmFormat.GetDuration(dataLength).TimeDeltaAsMillisecondDouble;
                double recordingDuration = State.Audio.ConvertBytesToMilliseconds(dataLength);

                managedAudioMediaNew = nodeRecord.Presentation.MediaFactory.CreateManagedAudioMedia();

                var mediaData =
                    (WavAudioMediaData)
                    nodeRecord.Presentation.MediaDataFactory.CreateAudioMediaData();

                managedAudioMediaNew.MediaData = mediaData;

                //mediaData.AppendAudioDataFromRiffWave(m_Recorder.RecordedFilePath);
                mediaData.AppendAudioData(recordingStream, new TimeDelta(recordingDuration));
            }
            finally
            {
                recordingStream.Close();
            }

            File.Delete(filePath);

            return managedAudioMediaNew;
        }


        private void openFile(String str, bool insert)
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

            if (insert)
            {
                if (m_PcmFormatOfAudioToInsert == null)
                {
                    Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        m_PcmFormatOfAudioToInsert = PCMDataInfo.ParseRiffWaveHeader(fileStream);
                    }
                    finally
                    {
                        fileStream.Close();
                    }
                }
                //var presenter = Container.Resolve<IShellPresenter>();
                //presenter.PlayAudioCueTockTock();

                if (View != null)
                {
                    View.ResetAll();
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    m_PcmFormatOfAudioToInsert = null;
                    return;
                }

                var session = Container.Resolve<IUrakawaSession>();

                if (session.DocumentProject != null)
                {
                    if (State.CurrentTreeNode == null)
                    {
                        m_PcmFormatOfAudioToInsert = null;
                        return;
                    }

                    TreeNode nodeRecord = (State.CurrentSubTreeNode ?? State.CurrentTreeNode);

                    ManagedAudioMedia recordingManagedAudioMedia = makeManagedAudioMediaFromFile(filePath);

                    ManagedAudioMedia managedAudioMedia = nodeRecord.GetManagedAudioMedia();
                    if (managedAudioMedia == null)
                    {
                        SequenceMedia seqAudioMedia = nodeRecord.GetAudioSequenceMedia();
                        bool isSeqValid = seqAudioMedia != null && !seqAudioMedia.AllowMultipleTypes;
                        if (isSeqValid)
                        {
                            foreach (Media media in seqAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
                            {
                                if (!(media is ManagedAudioMedia))
                                {
                                    isSeqValid = false;
                                    break;
                                }
                            }
                        }
                        if (isSeqValid)
                        {
                            var byteOffset = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                            double timeOffset = 0;
                            long sumData = 0;
                            long sumDataPrev = 0;
                            foreach (Media media in seqAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
                            {
                                var manangedMediaSeqItem = (ManagedAudioMedia)media;
                                AudioMediaData audioData = manangedMediaSeqItem.AudioMediaData;
                                sumData += AudioLibPCMFormat.ConvertTimeToBytes(audioData.AudioDuration.TimeDeltaAsMillisecondDouble, (int)audioData.PCMFormat.SampleRate, audioData.PCMFormat.BlockAlign);
                                if (byteOffset < sumData)
                                {
                                    timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - sumDataPrev);

                                    if (AudioPlaybackStreamKeepAlive)
                                    {
                                        ensurePlaybackStreamIsDead();
                                    }

                                    if (manangedMediaSeqItem.AudioMediaData == null)
                                    {
                                        Debug.Fail("This should never happen !!!");
                                        //recordingStream.Close();
                                        m_PcmFormatOfAudioToInsert = null;
                                        return;
                                    }

                                    var command = nodeRecord.Presentation.CommandFactory.
                                        CreateManagedAudioMediaInsertDataCommand(
                                        nodeRecord, manangedMediaSeqItem, recordingManagedAudioMedia,
                                        new Time(timeOffset));

                                    nodeRecord.Presentation.UndoRedoManager.Execute(command);

                                    //manangedMediaSeqItem.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new TimeDelta(recordingDuration));
                                    //recordingStream.Close();
                                    break;
                                }
                                sumDataPrev = sumData;
                            }
                        }
                        else
                        {
                            var command = nodeRecord.Presentation.CommandFactory.
                                CreateTreeNodeSetManagedAudioMediaCommand(
                                nodeRecord, recordingManagedAudioMedia);

                            nodeRecord.Presentation.UndoRedoManager.Execute(command);
                        }
                    }
                    else
                    {
                        double timeOffset = LastPlayHeadTime;
                        if (State.CurrentSubTreeNode != null)
                        {
                            var byteOffset = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

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
                                    if (State.CurrentSubTreeNode != marker.m_TreeNode)
                                    {
                                        Debug.Fail("This should never happen !!!");
                                        //recordingStream.Close();
                                        m_PcmFormatOfAudioToInsert = null;
                                        return;
                                    }

                                    timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - sumDataPrev);
                                    break;
                                }
                                sumDataPrev = sumData;
                            }
                        }

                        if (AudioPlaybackStreamKeepAlive)
                        {
                            ensurePlaybackStreamIsDead();
                        }

                        if (managedAudioMedia.AudioMediaData == null)
                        {
                            Debug.Fail("This should never happen !!!");
                            //recordingStream.Close();
                            m_PcmFormatOfAudioToInsert = null;
                            return;
                        }

                        var command = nodeRecord.Presentation.CommandFactory.
                            CreateManagedAudioMediaInsertDataCommand(
                            nodeRecord, managedAudioMedia, recordingManagedAudioMedia,
                            new Time(timeOffset));

                        nodeRecord.Presentation.UndoRedoManager.Execute(command);

                        //managedAudioMedia.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new TimeDelta(recordingDuration));
                        //recordingStream.Close();
                    }

                    //SelectionBegin = (LastPlayHeadTime < 0 ? 0 : LastPlayHeadTime);
                    //SelectionEnd = SelectionBegin + recordingManagedAudioMedia.Duration.TimeDeltaAsMillisecondDouble;

                    //ReloadWaveForm(); UndoRedoManager.Changed callback will take care of that.
                }
                else
                {
                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    AudioPlayer_LoadAndPlayFromFile(filePath);
                }

                m_PcmFormatOfAudioToInsert = null;
            }
            else
            {
                AudioPlayer_LoadAndPlayFromFile(filePath);
            }
        }
    }
}
