using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AudioLib;
using AudioLib.Events.VuMeter;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.commands;
using urakawa.core;
using urakawa.events;
using urakawa.events.undo;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.utilities;
using urakawa.media.timing;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// ViewModel for the AudioPane
    /// </summary>
    public partial class AudioPaneViewModel : ViewModelBase
    {
        public class StreamStateData
        {
            private AudioPaneViewModel m_viewModel;
            private PropertyChangedNotifyBase m_notifier;
            public StreamStateData(PropertyChangedNotifyBase notifier, AudioPaneViewModel vm)
            {
                m_viewModel = vm;
                m_notifier = notifier;
            }

            public bool HasContent
            {
                get { return PlayStream != null; }
            }

            public double ConvertBytesToMilliseconds(double bytes)
            {
                PCMFormatInfo pcm = null;
                if (PcmFormat == null)
                {
                    pcm = m_viewModel.m_RecordingPcmFormat;
                }
                return pcm.GetDuration((long)bytes).TimeDeltaAsMillisecondDouble;
                //return 1000.0 * bytes / ((double)PcmFormat.SampleRate * PcmFormat.NumberOfChannels * PcmFormat.BitDepth / 8.0);
            }

            public double ConvertMillisecondsToBytes(double ms)
            {
                PCMFormatInfo pcm = null;
                if (PcmFormat == null)
                {
                    pcm = m_viewModel.m_RecordingPcmFormat;
                }
                return pcm.GetDataLength(new TimeDelta(ms));
                //return (ms * PcmFormat.SampleRate * PcmFormat.NumberOfChannels * PcmFormat.BitDepth / 8.0) / 1000.0;
            }


            // The single stream of contiguous PCM data,
            // regardless of the sub chunks / tree nodes
            private Stream m_PlayStream;
            public Stream PlayStream
            {
                get
                {
                    return m_PlayStream;
                }
                set
                {
                    if (m_PlayStream == value) return;
                    m_PlayStream = value;
                    if (m_PlayStream != null)
                    {
                        m_PlayStream.Position = 0;
                        m_PlayStream.Seek(0, SeekOrigin.Begin);

                        if (m_viewModel.State.CurrentTreeNode != null)
                        {
                            Debug.Assert(m_viewModel.State.CurrentTreeNode.Presentation.MediaDataManager.EnforceSinglePCMFormat);
                            PcmFormat =
                                m_viewModel.State.CurrentTreeNode.Presentation.MediaDataManager.DefaultPCMFormat.Copy();

                            DataLength = m_PlayStream.Length;
                            EndOffsetOfPlayStream = DataLength;
                        }
                        else
                        {
                            PcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_PlayStream);

                            long dataLength = m_PlayStream.Length - m_PlayStream.Position;

                            m_PlayStream = new SubStream(m_PlayStream, m_PlayStream.Position, dataLength);

                            DataLength = m_PlayStream.Length;
                            EndOffsetOfPlayStream = DataLength;

                            Debug.Assert(dataLength == DataLength);
                        }
                    }
                    m_notifier.OnPropertyChanged(() => PlayStream);
                }
            }

            // The total byte length of the stream of audio PCM data.
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
                    m_notifier.OnPropertyChanged(() => DataLength);
                }
            }

            // The PCM format of the stream of audio data.
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
                    m_notifier.OnPropertyChanged(() => PcmFormat);
                }
            }

            // The stream offset in bytes where the audio playback should stop.
            // By default: it is the DataLength, but it can be changed when dealing with selections and preview-playback modes.
            private long m_EndOffsetOfPlayStream;
            public long EndOffsetOfPlayStream
            {
                get
                {
                    return m_EndOffsetOfPlayStream;
                }
                set
                {
                    if (m_EndOffsetOfPlayStream == value) return;
                    m_EndOffsetOfPlayStream = value;
                    m_notifier.OnPropertyChanged(() => EndOffsetOfPlayStream);
                }
            }

            // The list that defines the sub treenodes with associated chunks of audio data
            // This is never null: the count is 1 when the current main tree node has direct audio (no sub tree nodes)
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
                    m_notifier.OnPropertyChanged(() => PlayStreamMarkers);
                }
            }

            public void ResetAll()
            {
                PlayStream = null; // must be first because NotifyPropertyChange chain-reacts for DataLegth (TimeString data binding) 
                
                EndOffsetOfPlayStream = -1;
                PcmFormat = null;
                PlayStreamMarkers = null;
                DataLength = -1;
            }
        }

        public class SelectionStateData
        {
            private AudioPaneViewModel m_viewModel;
            private PropertyChangedNotifyBase m_notifier;
            public SelectionStateData(PropertyChangedNotifyBase notifier, AudioPaneViewModel vm)
            {
                m_notifier = notifier;
                m_viewModel = vm;
            }

            public void SetSelection(double begin, double end)
            {
                SelectionBegin = begin;
                SelectionEnd = end;

                if (m_viewModel.View != null && m_viewModel.State.Audio.HasContent)
                {
                    m_viewModel.View.SetSelection(
                        m_viewModel.State.Audio.ConvertMillisecondsToBytes(SelectionBegin),
                        m_viewModel.State.Audio.ConvertMillisecondsToBytes(SelectionEnd));
                }
            }

            public void ClearSelection()
            {
                SelectionBegin = -1.0;
                SelectionEnd = -1.0;
                if (m_viewModel.View != null)
                {
                    m_viewModel.View.ClearSelection();
                }
            }

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
                private set
                {
                    if (m_SelectionBegin == value) return;
                    m_SelectionBegin = value;
                    m_notifier.OnPropertyChanged(() => SelectionBegin);
                }
            }

            private double m_SelectionEnd;
            public double SelectionEnd
            {
                get
                {
                    return m_SelectionEnd;
                }
                private set
                {
                    if (m_SelectionEnd == value) return;
                    m_SelectionEnd = value;
                    m_notifier.OnPropertyChanged(() => SelectionEnd);
                }
            }

            public void ResetAll()
            {
                SelectionBegin = -1;
                SelectionEnd = -1;
            }
        }

        public StateData State { get; private set; }
        public class StateData
        {
            public SelectionStateData Selection { get; private set; }
            public StreamStateData Audio { get; private set; }

            private AudioPaneViewModel m_viewModel;
            private PropertyChangedNotifyBase m_notifier;
            public StateData(PropertyChangedNotifyBase notifier, AudioPaneViewModel vm)
            {
                m_notifier = notifier;
                m_viewModel = vm;
                Audio = new StreamStateData(m_notifier, vm);
                Selection = new SelectionStateData(m_notifier, vm);
            }

            public bool IsTreeNodeShownInAudioWaveForm(TreeNode treeNode)
            {
                if (CurrentTreeNode == null || !Audio.HasContent)
                {
                    return false;
                }

                if (CurrentTreeNode == treeNode || CurrentSubTreeNode == treeNode)
                {
                    return true;
                }

                foreach (TreeNodeAndStreamDataLength marker in Audio.PlayStreamMarkers)
                {
                    if (marker.m_TreeNode == treeNode) return true;
                }
                return false;
            }

            // Main selected node. There are sub tree nodes when no audio is directly
            // attached to this tree node.
            // Automatically implies that FilePath is null
            // (they are mutually-exclusive state values).
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
                    m_notifier.OnPropertyChanged(() => CurrentTreeNode);

                    CurrentSubTreeNode = null;

                    FilePath = null;
                }
            }

            // Secondary selected node. By default is the first one in the series.
            // It is equal to the main selected tree node when the audio data is attached directly to it.
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
                    m_notifier.OnPropertyChanged(() => CurrentSubTreeNode);
                }
            }

            // Path to a WAV file,
            // only used when the user opens such file for playback / preview.
            // Automatically implies that CurrentTreeNode and CurrentSubTreeNode are null
            // (they are mutually-exclusive state values).
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
                    m_notifier.OnPropertyChanged(() => FilePath);

                    CurrentTreeNode = null;
                }
            }

            public void ResetAll()
            {
                m_viewModel.Logger.Log("Audio StateData reset.", Category.Debug, Priority.Medium);

                FilePath = null;
                CurrentTreeNode = null;
                CurrentSubTreeNode = null;

                Selection.ResetAll();
                Audio.ResetAll();

                if (m_viewModel.View != null)
                {
                    m_viewModel.View.ResetAll();
                }
            }
        }

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

        //private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName.StartsWith("Can"))
        //    {
        //        Logger.Log("@@ AudioPaneViewModel.OnViewModelPropertyChanged: [" + e.PropertyName + "]", Category.Debug, Priority.High);

        //        CommandManager.InvalidateRequerySuggested();
        //    }
        //}

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

        private void OnUndoRedoManagerChanged(object sender, DataModelChangedEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            bool refresh = e is TransactionStartedEventArgs
                           || e is TransactionEndedEventArgs
                           || e is TransactionCancelledEventArgs
                           || e is DoneEventArgs
                           || e is UnDoneEventArgs
                           || e is ReDoneEventArgs;
            if (!refresh)
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            var eventt = (UndoRedoManagerEventArgs)e;

            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTockTock();

            if (View != null)
            {
                View.ResetAll();
            }

            if (eventt.Command is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)eventt.Command;

                UndoRedoManagerChanged(command, eventt);
                return;
            }
            else if (eventt.Command is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)eventt.Command;

                UndoRedoManagerChanged(command);
                return;
            }
            // TODO: TreeNode delete command, etc. (make sure CurrentTreeNode / CurrentSubTreeNode is up to date)

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }
            ReloadWaveForm();
        }

        struct StateToRestore
        {
            public double SelectionBegin;
            public double SelectionEnd;
            public double LastPlayHeadTime;
        }
        private StateToRestore? m_StateToRestore = null;

        private void UndoRedoManagerChanged(TreeNodeSetManagedAudioMediaCommand command)
        {
            TreeNode treeNode = command.TreeNode;

            if (treeNode == null)
            {
                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
                m_CurrentAudioStreamProvider();
            }

            double timeOffset = getTimeOffset(treeNode, command.ManagedAudioMedia);

            m_StateToRestore = new StateToRestore
                                 {
                                     SelectionBegin = -1,
                                     SelectionEnd = -1,
                                     LastPlayHeadTime = timeOffset
                                 };

            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

            if (!isTreeNodeInAudioWaveForm)
            {
                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }
            else
            {
                ReloadWaveForm();
            }
        }

        private void UndoRedoManagerChanged(ManagedAudioMediaInsertDataCommand command, UndoRedoManagerEventArgs e)
        {
            TreeNode treeNode = command.TreeNode;

            if (treeNode == null)
            {
                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
                m_CurrentAudioStreamProvider();
            }

            double timeOffset = getTimeOffset(treeNode, command.ManagedAudioMediaTarget);

            if (e is DoneEventArgs || e is ReDoneEventArgs)
            {
                double begin = command.TimeInsert.TimeAsMillisecondFloat + timeOffset;
                m_StateToRestore = new StateToRestore
                                       {
                                           SelectionBegin = begin,
                                           SelectionEnd = begin + command.ManagedAudioMediaSource.Duration.TimeDeltaAsMillisecondDouble,
                                           LastPlayHeadTime = begin
                                       };
            }
            else if (e is UnDoneEventArgs)
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = -1,
                    SelectionEnd = -1,
                    LastPlayHeadTime = command.TimeInsert.TimeAsMillisecondFloat + timeOffset
                };
            }

            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

            if (!isTreeNodeInAudioWaveForm)
            {
                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }
            else
            {
                ReloadWaveForm();
            }
        }

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

        private const bool AudioPlaybackStreamKeepAlive = true;

        private void initializeAudioStuff()
        {
            Logger.Log("AudioPaneViewModel.initializeAudioStuff", Category.Debug, Priority.Medium);

            m_Player = new AudioPlayer(AudioPlaybackStreamKeepAlive);
            m_Player.SetDevice(GetWindowsFormsHookControl(), @"fakename");
            m_Player.StateChanged += OnPlayerStateChanged;
            m_Player.EndOfAudioAsset += OnEndOfAudioAsset;

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

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
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

        #endregion Event / Callbacks

        #region Private Class Attributes

        #endregion Private Class Attributes

        #region Public Properties

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
                if (!IsAudioLoaded)
                {
                    return "";
                }
                var timeSpan = TimeSpan.FromMilliseconds(State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength));
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
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("LastPlayHeadTime")]
        [NotifyDependsOn("RecorderCurrentDuration")]
        public String TimeStringCurrent
        {
            get
            {
                if (!IsAudioLoaded)
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
            if (!State.Audio.HasContent
                || m_Player.State != AudioPlayerState.Playing
                && (m_Recorder.State != AudioRecorderState.Recording && m_Recorder.State != AudioRecorderState.Monitoring))
            {
                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                }
                return;
            }
            PeakMeterBarDataCh1.ValueDb = m_PeakMeterValues[0];
            PCMFormatInfo pcm = null;
            if (State.Audio.PcmFormat == null)
            {
                pcm = m_RecordingPcmFormat;
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

            bool oldVal = IsAutoPlay;
            IsAutoPlay = false;
            AudioPlayer_UpdateWaveFormPlayHead();
            IsAutoPlay = oldVal;
        }

        private void OnUpdateVuMeter(object sender, UpdatePeakMeter e)
        {
            if (IsRecording || IsMonitoring)
            {
                if (View != null)
                {
                    View.TimeMessageRefresh();
                }

                // TODO: generates too many events per seconds in the data binding pipeline ?
                OnPropertyChanged(() => RecorderCurrentDuration);
            }

            if (e.PeakValues != null && e.PeakValues.Length > 0)
            {
                m_PeakMeterValues[0] = e.PeakValues[0];
                PCMFormatInfo pcmInfo = null;
                if (State.Audio.PcmFormat == null)
                {
                    pcmInfo = m_RecordingPcmFormat;
                }
                if (State.Audio.HasContent && pcmInfo.NumberOfChannels > 1)
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
