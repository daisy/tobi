﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa;
using urakawa.command;
using urakawa.core;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    ///<summary>
    /// Single shared instance (singleton) of the audio view
    ///</summary>
    [Export(typeof(AudioPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class AudioPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        #region Construction


        private readonly ILoggerFacade Logger;
        private readonly IEventAggregator EventAggregator;

        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_UrakawaSession;


        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="eventAggregator">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the Unity container, it's a Tobi built-in type</param>
        ///<param name="session">normally obtained from the Unity container, it's a Tobi built-in type</param>
        [ImportingConstructor]
        public AudioPaneViewModel(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session
            )
        {
            EventAggregator = eventAggregator;
            Logger = logger;

            m_ShellView = shellView;
            m_UrakawaSession = session;

            Initialize();

            m_AudioStreamProvider_File = () =>
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
                        State.Audio.SetPlayStream_FromFile(File.Open(State.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read), State.FilePath);
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

            m_AudioStreamProvider_TreeNode = () =>
            {
                if (State.CurrentTreeNode == null) return null;

                if (State.Audio.PlayStream == null)
                {
                    StreamWithMarkers? sm = State.CurrentTreeNode.OpenPcmInputStreamOfManagedAudioMediaFlattened();

                    if (sm == null)
                    {
                        TreeNode ancerstor = State.CurrentTreeNode.GetFirstAncestorWithManagedAudio();
                        if (ancerstor != null)
                        {
                            StreamWithMarkers? sma = ancerstor.OpenPcmInputStreamOfManagedAudioMedia();
                            if (sma != null)
                            {
                                TreeNode theCurrentSubTreeNode = State.CurrentTreeNode;
                                State.CurrentTreeNode = ancerstor;

                                State.Audio.SetPlayStream_FromTreeNode(sma.GetValueOrDefault().m_Stream);
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
                        State.Audio.SetPlayStream_FromTreeNode(sm.GetValueOrDefault().m_Stream);
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

            EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Subscribe(str => StatusBarMessage = str, StatusBarMessageUpdateEvent.THREAD_OPTION);

            Settings.Default.PropertyChanged += SettingsPropertyChanged;
        }

        private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.StartsWith(@"AudioWaveForm_")) return;

            if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Resolution))
            {
                WaveStepX = Settings.Default.AudioWaveForm_Resolution;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_IsFilled))
            {
                IsEnvelopeFilled = Settings.Default.AudioWaveForm_IsFilled;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_IsBordered))
            {
                IsEnvelopeVisible = Settings.Default.AudioWaveForm_IsBordered;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_IsStroked))
            {
                IsWaveFillVisible = Settings.Default.AudioWaveForm_IsStroked;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Stroke))
            {
                ColorWaveBars = Settings.Default.AudioWaveForm_Color_Stroke;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Border))
            {
                ColorEnvelopeOutline = Settings.Default.AudioWaveForm_Color_Border;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Fill))
            {
                ColorEnvelopeFill = Settings.Default.AudioWaveForm_Color_Fill;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_CursorBorder))
            {
                ColorPlayhead = Settings.Default.AudioWaveForm_Color_CursorBorder;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_CursorFill))
            {
                ColorPlayheadFill = Settings.Default.AudioWaveForm_Color_CursorFill;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Phrases))
            {
                ColorMarkers = Settings.Default.AudioWaveForm_Color_Phrases;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Selection))
            {
                ColorTimeSelection = Settings.Default.AudioWaveForm_Color_Selection;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_TimeText))
            {
                ColorTimeInfoText = Settings.Default.AudioWaveForm_Color_TimeText;
            }
        }

        //StatusBarMessageUpdateEvent
        private string m_StatusBarMessage;
        public string StatusBarMessage
        {
            private set
            {
                if (m_StatusBarMessage == value)
                {
                    return;
                }
                m_StatusBarMessage = value;

                RaisePropertyChanged(() => StatusBarMessage);
            }
            get { return m_StatusBarMessage; }
        }

        public IInputBindingManager InputBindingManager
        {
            get { return m_ShellView; }
        }

        ~AudioPaneViewModel()
        {
#if DEBUG
            Logger.Log("AudioPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
            Settings.Default.PropertyChanged -= SettingsPropertyChanged;
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

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);
            
            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
            EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, SubTreeNodeSelectedEvent.THREAD_OPTION);

            EventAggregator.GetEvent<EscapeEvent>().Subscribe(OnEscape, EscapeEvent.THREAD_OPTION);
        }

        private void OnEscape(object obj)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<object>)OnEscape, obj);
                return;
            }

            if (View != null)
            {
                View.CancelWaveFormLoad();
            }
            if (IsPlaying)
            {
                CommandPause.Execute();
                return;
            }
            if (IsMonitoring)
            {
                CommandStopMonitor.Execute();
                return;
            }
            if (IsRecording)
            {
                CommandStopRecord.Execute();
                return;
            }
        }

        private void initializeAudioStuff()
        {
            Logger.Log("AudioPaneViewModel.initializeAudioStuff", Category.Debug, Priority.Medium);

            m_Player = new AudioPlayer(AudioPlaybackStreamKeepAlive);
            m_Player.SetOutputDevice(GetWindowsFormsHookControl(), @"fakename");
            m_Player.StateChanged += OnStateChanged_Player;
            m_Player.AudioPlaybackFinished += OnAudioPlaybackFinished;

            //m_Player.ResetVuMeter += OnPlayerResetVuMeter;

            m_Recorder = new AudioRecorder();
            m_Recorder.SetDevice(@"fakename");
            m_Recorder.StateChanged += OnStateChanged_Recorder;
            m_Recorder.AudioRecordingFinished += OnAudioRecordingFinished;
            m_Recorder.RecordingDirectory = AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY; // Directory.GetCurrentDirectory();

            //m_Recorder.ResetVuMeter += OnRecorderResetVuMeter;

            m_VuMeter = new VuMeter(m_Player, m_Recorder);
            m_VuMeter.PeakMeterUpdated += OnPeakMeterUpdated;
            m_VuMeter.PeakMeterOverloaded += OnPeakMeterOverloaded;

            PeakMeterBarDataCh1 = new PeakMeterBarData();
            PeakMeterBarDataCh2 = new PeakMeterBarData();
            PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;
            m_PeakMeterValues = new double[2];

            m_AudioFormatConvertorSession_NoProject =
                new AudioFormatConvertorSession(AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY, null);
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
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<TreeNode>)OnSubTreeNodeSelected, node);
                return;
            }
            //Logger.Log("AudioPaneViewModel.OnSubTreeNodeSelected", Category.Debug, Priority.Medium);

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

            RefreshWaveFormChunkMarkersForCurrentSubTreeNode(true);
        }

        private void RefreshWaveFormChunkMarkersForCurrentSubTreeNode(bool placePlayHead)
        {
            if (!State.Audio.HasContent || State.CurrentSubTreeNode == null)
            {
                return;
            }

            long bytesRight = 0;
            long bytesLeft = 0;
            foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
            {
                bytesRight += marker.m_LocalStreamDataLength;

                if (State.CurrentSubTreeNode == marker.m_TreeNode)
                {
                    if (placePlayHead)
                    {
                        LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(bytesLeft);
                    }

                    if (View != null)
                    {
                        View.RefreshUI_WaveFormChunkMarkers(bytesLeft, bytesRight);
                    }
                    break;
                }

                bytesLeft = bytesRight;
            }
        }

        private void OnTreeNodeSelected(TreeNode node)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<TreeNode>)OnTreeNodeSelected, node);
                return;
            }

            //Logger.Log("AudioPaneViewModel.OnTreeNodeSelected", Category.Debug, Priority.Medium);

            if (node == null)
            {
                return;
            }

            //if (IsWaveFormLoading && View != null)
            //{
            //    View.CancelWaveFormLoad();
            //}

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
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            State.ResetAll();

            m_LastPlayHeadTime = -1;
            IsWaveFormLoading = false;

            State.CurrentTreeNode = node;

            m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;

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

        private AudioFormatConvertorSession m_AudioFormatConvertorSession;
        private AudioFormatConvertorSession m_AudioFormatConvertorSession_NoProject;

        private void OnProjectUnLoaded(Project project)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<Project>)OnProjectUnLoaded, project);
                return;
            }

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;

            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<Project>)OnProjectLoaded, project);
                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            State.ResetAll();

            AudioClipboard = null;
            m_LastPlayHeadTime = -1;
            IsWaveFormLoading = false;

            //var shell = Container.Resolve<IShellView>();
            //shell.DocumentProject
            if (project != null)
            {
                m_AudioFormatConvertorSession =
                    new AudioFormatConvertorSession(
                    //AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY,
                    project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath,
                project.Presentations.Get(0).MediaDataManager.DefaultPCMFormat);

                project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
                m_Recorder.RecordingDirectory = project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath; //AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY

                EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Ready);
            }
            else
            {
                m_AudioFormatConvertorSession = null;

                m_Recorder.RecordingDirectory = AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY; // Directory.GetCurrentDirectory();

                EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("No document."); // TODO Localize NoDocument
            }
        }

        #endregion Event / Callbacks

        #region Private Class Attributes

        private const bool AudioPlaybackStreamKeepAlive = true;

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

            SequenceMedia seqManAudioMedia = treeNode.GetManagedAudioSequenceMedia();
            if (seqManAudioMedia != null)
            {
                Debug.Fail("SequenceMedia is normally removed at import time...have you tried re-importing the DAISY book ?");

                foreach (Media media in seqManAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
                {
                    var manMedia = (ManagedAudioMedia)media;
                    if (media == managedMedia)
                    {
                        break;
                    }
                    timeOffset += manMedia.Duration.AsMilliseconds;
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
                return m_ShellView.SplitterDrag;
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOn("IsSelectionSet")]
        public bool TimeStringSelectionVisible
        {
            get
            {
                return IsAudioLoaded && IsSelectionSet;
            }
        }

        [NotifyDependsOn("TimeStringSelectionVisible")]
        public string TimeStringSelectionStart
        {
            get
            {
                if (!TimeStringSelectionVisible)
                {
                    return "";
                }

                return FormatTimeSpan_Units(State.Selection.SelectionBegin);
            }
        }

        [NotifyDependsOn("TimeStringSelectionVisible")]
        public string TimeStringSelectionEnd
        {
            get
            {
                if (!TimeStringSelectionVisible)
                {
                    return "";
                }

                return FormatTimeSpan_Units(State.Selection.SelectionEnd);
            }
        }

        [NotifyDependsOn("TimeStringSelectionVisible")]
        public string TimeStringSelectionDur
        {
            get
            {
                if (!TimeStringSelectionVisible)
                {
                    return "";
                }

                return FormatTimeSpan_Units(State.Selection.SelectionEnd - State.Selection.SelectionBegin);
            }
        }

        [NotifyDependsOn("IsAudioLoaded")]
        [NotifyDependsOnEx("DataLength", typeof(StreamStateData))]
        public string TimeStringTotalWaveform
        {
            get
            {
                if (!IsAudioLoaded)
                {
                    return "";
                }

                return FormatTimeSpan_Units(State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength));
            }
        }

        private static readonly PropertyChangedEventArgs m_RecorderCurrentDurationArgs
            = new PropertyChangedEventArgs("RecorderCurrentDuration");
        public double RecorderCurrentDuration
        {
            get
            {
                return m_Recorder.CurrentDuration;
            }
        }

        public static string FormatTimeSpan_Units(double ms)
        {
            TimeSpan time = TimeSpan.FromTicks((long) (ms*TimeSpan.TicksPerMillisecond));

            if (Settings.Default.UseFriendlyTimeFormat)
                return Time.Format_H_MN_S_MS(time);

            return Time.Format_Standard(time);
        }


        [NotifyDependsOn("TimeStringSelectionEnd")]
        [NotifyDependsOn("TimeStringSelectionStart")]
        [NotifyDependsOn("TimeStringSelectionDur")]
        public String TimeStringSelection
        {
            get
            {
                return "Selection: " + TimeStringSelectionStart + " - " + TimeStringSelectionEnd + " (" + TimeStringSelectionDur + ")";
            }
        }

        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsAudioLoaded")]
        public bool TimeStringCurrentVisible
        {
            get
            {
                return IsAudioLoaded || IsRecording || IsMonitoring;
            }
        }

        private static readonly PropertyChangedEventArgs m_TimeStringCurrentArgs
        = new PropertyChangedEventArgs("TimeStringCurrent");
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOn("TimeStringCurrentVisible")]
        [NotifyDependsOn("TimeStringTotalWaveform")]
        [NotifyDependsOnFast("m_LastPlayHeadTimeArgs", "m_TimeStringCurrentArgs")]
        [NotifyDependsOnFast("m_RecorderCurrentDurationArgs", "m_TimeStringCurrentArgs")]
        public String TimeStringCurrent
        {
            get
            {
                if (IsRecording || IsMonitoring)
                {
                    return FormatTimeSpan_Units(RecorderCurrentDuration);
                }

                string strToDisplay = null;


                if (!IsAudioLoaded)
                {
                    return "";
                }
                else if (IsPlaying)
                {
                    strToDisplay = FormatTimeSpan_Units(m_Player.CurrentTime);
                }
                else if (LastPlayHeadTime >= 0 && (
                                                 m_Player.CurrentState == AudioPlayer.State.Paused ||
                                                 m_Player.CurrentState == AudioPlayer.State.Stopped
                                             ))
                {
                    strToDisplay = FormatTimeSpan_Units(LastPlayHeadTime);
                }

                if (!String.IsNullOrEmpty(strToDisplay))
                {
                    return "Time: " + strToDisplay + " / " + TimeStringTotalWaveform;
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
                    RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);
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
                    //double time = PcmFormat.GetDuration(DataLength).AsMilliseconds;
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

                long bytesRight = 0;
                long bytesLeft = 0;
                int index = -1;
                foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                {
                    index++;
                    bytesRight += marker.m_LocalStreamDataLength;
                    if (byteOffset < bytesRight
                    || index == (State.Audio.PlayStreamMarkers.Count - 1) && byteOffset >= bytesRight)
                    {
                        subTreeNode = marker.m_TreeNode;

                        if (View != null && subTreeNode != State.CurrentSubTreeNode)
                        {
                            View.RefreshUI_WaveFormChunkMarkers(bytesLeft, bytesRight);
                        }
                        break;
                    }
                    bytesLeft = bytesRight;
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

            PCMFormatInfo pcmInfo = State.Audio.GetCurrentPcmFormat();
            if (pcmInfo.Data.NumberOfChannels > 1)
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
            //Logger.Log("AudioPaneViewModel.resetPeakMeter", Category.Debug, Priority.Medium);

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
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<object, VuMeter.PeakMeterUpdateEventArgs>)OnPeakMeterUpdated, sender, e);
                return;
            }

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

                PCMFormatInfo pcmInfo = State.Audio.GetCurrentPcmFormat();

                if (pcmInfo.Data.NumberOfChannels > 1)
                {
                    m_PeakMeterValues[1] = e.PeakDb[1];
                }
                UpdatePeakMeter();
            }
        }

        private void OnPeakMeterOverloaded(object sender, VuMeter.PeakOverloadEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<object, VuMeter.PeakOverloadEventArgs>)OnPeakMeterOverloaded, sender, e);
                return;
            }
            if (e != null)
            {
                AudioCues.PlayHi();

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


        private void openFile(String str, bool insert, bool deleteAfterInsert, PCMFormatInfo pcmInfo)
        {
            Logger.Log("AudioPaneViewModel.OpenFile", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();

            string filePath = str;

            if (String.IsNullOrEmpty(filePath) && View != null)
            {
                filePath = View.OpenFileDialog();
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            bool isWav = Path.GetExtension(filePath).ToLower() == ".wav";

            AudioLibPCMFormat wavFormat = (pcmInfo != null ? pcmInfo.Copy().Data : null);
            if (isWav && wavFormat == null)
            {
                Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    uint dataLength;
                    wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                }
                finally
                {
                    fileStream.Close();
                }
            }

            if (insert && m_UrakawaSession.DocumentProject != null && State.CurrentTreeNode != null)
            {
                string originalFilePath = null;

                Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);

                bool wavNeedsConversion = false;
                if (wavFormat != null)
                {
                    if (wavFormat.IsCompatibleWith(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Data))
                    {
                        State.Audio.PcmFormat = new PCMFormatInfo(wavFormat);
                        //RaisePropertyChanged(() => State.Audio.PcmFormat);
                    }
                    else
                    {
                        wavNeedsConversion = true;
                    }
                }

                if (!isWav || wavNeedsConversion)
                {
                    originalFilePath = filePath;

                    filePath = m_AudioFormatConvertorSession.ConvertAudioFileFormat(filePath);

                    Logger.Log(string.Format("Converted audio {0} to {1}", originalFilePath, filePath),
                               Category.Debug, Priority.Medium);

                    Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        uint dataLength;
                        wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                        State.Audio.PcmFormat = new PCMFormatInfo(wavFormat);
                        //RaisePropertyChanged(() => State.Audio.PcmFormat);
                    }
                    finally
                    {
                        fileStream.Close();
                    }
                }

                //AudioCues.PlayTockTock();


                TreeNode treeNode = (State.CurrentSubTreeNode ?? State.CurrentTreeNode);

                ManagedAudioMedia managedAudioMedia = treeNode.Presentation.MediaFactory.CreateManagedAudioMedia();

                var mediaData = (WavAudioMediaData)treeNode.Presentation.MediaDataFactory.CreateAudioMediaData();

                managedAudioMedia.AudioMediaData = mediaData;

                //Directory.GetParent(filePath).FullName
                //bool recordedFileIsInDataDir = Path.GetDirectoryName(filePath) == nodeRecord.Presentation.DataProviderManager.DataFileDirectoryFullPath;

                if (deleteAfterInsert)
                {
                    FileDataProvider dataProv = (FileDataProvider)treeNode.Presentation.DataProviderFactory.Create(DataProviderFactory.AUDIO_WAV_MIME_TYPE);
                    dataProv.InitByMovingExistingFile(filePath);
                    mediaData.AppendPcmData(dataProv);
                }
                else
                {
                    // TODO: progress ! (time consuming file copy)
                    mediaData.AppendPcmData_RiffHeader(filePath);
                }

                if (deleteAfterInsert && File.Exists(filePath)) //check exist just in case file adopted by DataProviderManager
                {
                    File.Delete(filePath);
                }

                if (!string.IsNullOrEmpty(originalFilePath)
                    && deleteAfterInsert && File.Exists(originalFilePath)) //check exist just in case file adopted by DataProviderManager
                {
                    File.Delete(originalFilePath);
                }

                insertAudioAtCursorOrSelectionReplace(treeNode, managedAudioMedia);

                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            if (!isWav)
            {
                string originalFilePath = filePath;

                filePath = m_AudioFormatConvertorSession_NoProject.ConvertAudioFileFormat(filePath);

                Logger.Log(string.Format("Converted audio {0} to {1}", originalFilePath, filePath),
                           Category.Debug, Priority.Medium);

                Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    uint dataLength;
                    wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                }
                finally
                {
                    fileStream.Close();
                }
            }

            State.Audio.PcmFormat = new PCMFormatInfo(wavFormat);
            //RaisePropertyChanged(() => State.Audio.PcmFormat);

            AudioPlayer_LoadAndPlayFromFile(filePath);
        }

        private void insertAudioAtCursorOrSelectionReplace(TreeNode treeNode, ManagedAudioMedia manMedia)
        {
            if (View != null)
            {
                View.ResetAll();
            }

            Media audioMedia = treeNode.GetManagedAudioMediaOrSequenceMedia();
            if (audioMedia == null)
            {
                var command = treeNode.Presentation.CommandFactory.
                    CreateTreeNodeSetManagedAudioMediaCommand(
                    treeNode, manMedia);

                treeNode.Presentation.UndoRedoManager.Execute(command);
            }
            else if (audioMedia is ManagedAudioMedia)
            {
                var managedAudioMedia = (ManagedAudioMedia)audioMedia;

                double timeOffset = LastPlayHeadTime;
                if (State.CurrentSubTreeNode != null)
                {
                    var byteOffset = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                    long bytesRight = 0;
                    long bytesLeft = 0;
                    int index = -1;
                    foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                    {
                        index++;

                        bytesRight += marker.m_LocalStreamDataLength;
                        if (byteOffset < bytesRight
                                || index == (State.Audio.PlayStreamMarkers.Count - 1) && byteOffset >= bytesRight)
                        {
                            if (State.CurrentSubTreeNode != marker.m_TreeNode)
                            {
                                //recordingStream.Close();
                                Debug.Fail("This should never happen !!!");
                                return;
                            }

                            timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - bytesLeft);
                            break;
                        }
                        bytesLeft = bytesRight;
                    }
                }


                if (!managedAudioMedia.HasActualAudioMediaData)
                {
                    Debug.Fail("This should never happen !!!");
                    //recordingStream.Close();
                    return;
                }

                if (IsSelectionSet)
                {
                    treeNode.Presentation.UndoRedoManager.StartTransaction("Replace audio selection", "delete the current audio selection and insert new audio");

                    var timeInsert = State.Selection.SelectionBegin;

                    CommandDeleteAudioSelection.Execute();

                    Command command;

                    Media newManMedia = treeNode.GetManagedAudioMedia();
                    if (newManMedia == null)
                    {
                        command = treeNode.Presentation.CommandFactory.
                            CreateTreeNodeSetManagedAudioMediaCommand(
                            treeNode, manMedia);
                    }
                    else
                    {
                        command = treeNode.Presentation.CommandFactory.
                           CreateManagedAudioMediaInsertDataCommand(
                               treeNode, manMedia,
                               new Time(timeInsert),
                               State.CurrentTreeNode);
                    }

                    treeNode.Presentation.UndoRedoManager.Execute(command);

                    treeNode.Presentation.UndoRedoManager.EndTransaction();
                }
                else
                {
                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    var command = treeNode.Presentation.CommandFactory.
                        CreateManagedAudioMediaInsertDataCommand(
                            treeNode, manMedia,
                            new Time(timeOffset),
                            State.CurrentTreeNode);

                    treeNode.Presentation.UndoRedoManager.Execute(command);
                }

                //managedAudioMedia.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new Time(recordingDuration));
                //recordingStream.Close();
            }
            else if (audioMedia is SequenceMedia)
            {
                Debug.Fail("SequenceMedia is normally removed at import time...have you tried re-importing the DAISY book ?");

                var seqManAudioMedia = (SequenceMedia)audioMedia;

                var byteOffset = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                double timeOffset = 0;
                long sumData = 0;
                long sumDataPrev = 0;
                foreach (Media media in seqManAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
                {
                    var manangedMediaSeqItem = (ManagedAudioMedia)media;
                    if (!manangedMediaSeqItem.HasActualAudioMediaData)
                    {
                        continue;
                    }

                    AudioMediaData audioData = manangedMediaSeqItem.AudioMediaData;
                    sumData += audioData.PCMFormat.Data.ConvertTimeToBytes(audioData.AudioDuration.AsMilliseconds);
                    if (byteOffset < sumData)
                    {
                        timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - sumDataPrev);

                        if (AudioPlaybackStreamKeepAlive)
                        {
                            ensurePlaybackStreamIsDead();
                        }

                        if (!manangedMediaSeqItem.HasActualAudioMediaData)
                        {
                            Debug.Fail("This should never happen !!!");
                            //recordingStream.Close();
                            return;
                        }

                        var command = treeNode.Presentation.CommandFactory.
                            CreateManagedAudioMediaInsertDataCommand(
                            treeNode, manMedia,
                            new Time(timeOffset),
                            State.CurrentTreeNode); //manangedMediaSeqItem

                        treeNode.Presentation.UndoRedoManager.Execute(command);

                        //manangedMediaSeqItem.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new Time(recordingDuration));
                        //recordingStream.Close();
                        break;
                    }
                    sumDataPrev = sumData;
                }
            }

            //SelectionBegin = (LastPlayHeadTime < 0 ? 0 : LastPlayHeadTime);
            //SelectionEnd = SelectionBegin + recordingManagedAudioMedia.Duration.AsMilliseconds;

            //ReloadWaveForm(); UndoRedoManager.Changed callback will take care of that.
        }
    }
}