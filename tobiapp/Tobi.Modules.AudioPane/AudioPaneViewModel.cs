using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa;
using urakawa.core;
using urakawa.daisy.import;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    ///<summary>
    /// Single shared instance (singleton) of the audio view
    ///</summary>
    [Export(typeof(AudioPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IAudioViewModel))]
    public partial class AudioPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification, IAudioViewModel
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

        internal readonly IUrakawaSession m_UrakawaSession;
        public IUrakawaSession UrakawaSession
        {
            get { return m_UrakawaSession; }
        }

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
                TotalDocumentAudioDurationInLocalUnits = 0;
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
                        State.Audio.SetPlayStream_FromFile(
                            File.Open(State.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                            File.Open(State.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                            State.FilePath);
                    }
                    catch (Exception ex)
                    {
                        State.ResetAll();

                        m_LastSetPlayBytePosition = -1;
                        //IsWaveFormLoading = false;
                        return null;
                    }
                }
                TotalDocumentAudioDurationInLocalUnits = State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(State.Audio.DataLength);
                return State.Audio.PlayStream;
            };

            m_AudioStreamProvider_TreeNode = () =>
            {
                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                if (treeNodeSelection.Item1 == null) return null;

                if (State.Audio.PlayStream == null)
                {
                    DebugFix.Assert(State.Audio.PlayStreamMarkers == null);

                    DebugFix.Assert(treeNodeSelection.Item1.Presentation.MediaDataManager.EnforceSinglePCMFormat);
                    PCMFormatInfo pcmFormat = treeNodeSelection.Item1.Presentation.MediaDataManager.DefaultPCMFormat;

                    Stopwatch stopWatch = Stopwatch.StartNew();

                    long totalLength = 0;

#if USE_NORMAL_LIST
                StreamWithMarkers? 
#else
                    StreamWithMarkers
#endif //USE_NORMAL_LIST
 sm = treeNodeSelection.Item1.OpenPcmInputStreamOfManagedAudioMediaFlattened(

                        streamLength =>
                        {
                            totalLength += streamLength;

                            if (stopWatch.ElapsedMilliseconds > 500)
                            {
                                stopWatch.Stop();

                                TheDispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() =>
                                {
                                    long timeInLocalUnits = pcmFormat.Data.ConvertBytesToTime(totalLength);

                                    m_TimeStringOther = FormatTimeSpan_Units(timeInLocalUnits);
                                    View.TimeMessageShow();
                                    //View.TimeMessageRefresh();
                                }));
                                m_ShellView.PumpDispatcherFrames(DispatcherPriority.Loaded);

                                //stopWatch.Restart(); NET40 only !
                                stopWatch.Reset();
                                stopWatch.Start();
                            }
                        }
                        , true
                        );

                    stopWatch.Stop();
                    m_TimeStringOther = String.Empty;
                    View.TimeMessageHide();

                    if (sm != null)
                    {
                        //    TreeNode ancerstor = State.CurrentTreeNode.GetFirstAncestorWithManagedAudio();
                        //    if (ancerstor != null)
                        //    {
                        //        StreamWithMarkers? sma = ancerstor.OpenPcmInputStreamOfManagedAudioMedia();
                        //        if (sma != null)
                        //        {
                        //            TreeNode theCurrentSubTreeNode = State.CurrentTreeNode;
                        //            State.CurrentTreeNode = ancerstor;

                        //            State.Audio.SetPlayStream_FromTreeNode(sma.GetValueOrDefault().m_Stream);
                        //            State.Audio.PlayStreamMarkers = sma.GetValueOrDefault().m_SubStreamMarkers;

                        //            Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.m_CurrentAudioStreamProvider", Category.Debug, Priority.Medium);

                        //            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(State.CurrentTreeNode);

                        //            Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.m_CurrentAudioStreamProvider", Category.Debug, Priority.Medium);

                        //            EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(theCurrentSubTreeNode);
                        //        }
                        //    }
                        //}
                        //else
                        //{

                        State.Audio.SetPlayStream_FromTreeNode(sm.
#if USE_NORMAL_LIST
            GetValueOrDefault().
#endif //USE_NORMAL_LIST
m_Stream,
sm.
#if USE_NORMAL_LIST
            GetValueOrDefault().
#endif //USE_NORMAL_LIST
m_SecondaryStream
);
                        State.Audio.PlayStreamMarkers = sm.
#if USE_NORMAL_LIST
            GetValueOrDefault().
#endif //USE_NORMAL_LIST
m_SubStreamMarkers;

                        DebugFix.Assert(totalLength == sm.
#if USE_NORMAL_LIST
            GetValueOrDefault().
#endif //USE_NORMAL_LIST
m_Stream.Length);
                    }

                    //if (State.Audio.PlayStream == null)
                    //{
                    //    //State.ResetAll();
                    //    //m_LastPlayHeadTime = -1;
                    //    //IsWaveFormLoading = false;
                    //    return null;
                    //}

                    //if (State.Audio.PlayStreamMarkers.Count == 1)
                    //{
                    //    State.CurrentSubTreeNode = State.CurrentTreeNode;
                    //}
                }

                return State.Audio.PlayStream;
            };

            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Subscribe(str => StatusBarMessage = str,
                                                                                  StatusBarMessageUpdateEvent.
                                                                                      THREAD_OPTION);
                EventAggregator.GetEvent<TotalAudioDurationComputedByFlowDocumentParserEvent>().Subscribe(
                    dur => TotalDocumentAudioDurationInLocalUnits = dur.AsLocalUnits,
                    TotalAudioDurationComputedByFlowDocumentParserEvent.THREAD_OPTION);
            }

            Settings.Default.PropertyChanged += OnSettingsPropertyChanged;
        }


        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.StartsWith(@"AudioWaveForm_")
                && !e.PropertyName.StartsWith(@"Audio_")) return;

            if (e.PropertyName == GetMemberName(() => Settings.Default.Audio_TTS_Voice))
            {
                try
                {
                    m_SpeechSynthesizer.SelectVoice(Settings.Default.Audio_TTS_Voice);

                    if (m_SpeechSynthesizer.Voice.Name != Settings.Default.Audio_TTS_Voice)
                        Settings.Default.Audio_TTS_Voice = m_SpeechSynthesizer.Voice.Name; // will generate a call to OnSettingsPropertyChanged again
                }
                catch (Exception ex)
                {
                    try
                    {
                        Settings.Default.Audio_TTS_Voice = m_SpeechSynthesizer.Voice.Name;
                    }
                    catch (Exception ex_)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.Audio_InputDevice))
            {
                if (m_Recorder.CurrentState == AudioRecorder.State.Stopped
                    || m_Recorder.CurrentState == AudioRecorder.State.NotReady
                    || IsMonitoring // && IsMonitoringAlways
                    )
                {
                    bool monitoring = false;
                    if (IsMonitoring)
                    {
                        //DebugFix.Assert(IsMonitoringAlways);
                        monitoring = true;
                        CommandStopMonitor.Execute();
                    }

                    m_Recorder.SetInputDevice(Settings.Default.Audio_InputDevice);

                    if (m_Recorder.InputDevice.Name != Settings.Default.Audio_InputDevice)
                    {
                        Settings.Default.Audio_InputDevice = m_Recorder.InputDevice.Name; // will generate a call to OnSettingsPropertyChanged again
                    }
                    //Settings.Default.Save();

                    if (monitoring) // && IsMonitoringAlways)
                    {
                        CommandStartMonitor.Execute();
                    }
                }
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.Audio_OutputDevice))
            {
                //double time = -1;
                bool paused = false;
                if (IsPlaying)
                {
                    //time = m_Player.CurrentTime;
                    paused = true;
                    CommandPause.Execute();
                }

                m_Player.SetOutputDevice(GetWindowsFormsHookControl(), Settings.Default.Audio_OutputDevice);
                //m_Player.OutputDevice = value;

                if (m_Player.OutputDevice.Name != Settings.Default.Audio_OutputDevice)
                {
                    Settings.Default.Audio_OutputDevice = m_Player.OutputDevice.Name; // will generate a call to OnSettingsPropertyChanged again
                }
                //Settings.Default.Save();

                if (paused) //time >= 0 && State.Audio.HasContent)
                {
                    CommandPlay.Execute();
                    //AudioPlayer_PlayFromTo(State.Audio.ConvertMillisecondsToBytes(time), -1);
                }
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Resolution))
            {
                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                //WaveStepX = Settings.Default.AudioWaveForm_Resolution;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_DrawStyle)
                || e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_UseDecibels)
                )
            {
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                //IsEnvelopeFilled = Settings.Default.AudioWaveForm_IsFilled;
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.Audio_AlwaysMonitor))
            {
                RaisePropertyChanged(() => IsMonitoringAlways);
            }
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_IsBordered))
            //{
            //    IsEnvelopeVisible = Settings.Default.AudioWaveForm_IsBordered;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_IsStroked))
            //{
            //    IsWaveFillVisible = Settings.Default.AudioWaveForm_IsStroked;
            //}
            else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Selection))
            {
                RaisePropertyChanged(() => ColorSelectionContourBrush);
                //ColorTimeSelection = Settings.Default.AudioWaveForm_Color_Selection;
            }
            else if (e.PropertyName.StartsWith(@"AudioWaveForm_Color"))
            {
                //ColorTimeSelection

                if (View != null
                    && e.PropertyName != GetMemberName(() => Settings.Default.AudioWaveForm_Color_Selection))
                {
                    if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_CursorBorder)
                        || e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_CursorFill))
                    {
                        //ColorPlayhead
                        //ColorPlayheadFill

                        AudioPlayer_UpdateWaveFormPlayHead();
                    }
                    else
                    {
                        //ColorEnvelopeFill
                        //ColorWaveBars
                        //ColorEnvelopeOutline
                        //ColorTimeInfoText
                        //ColorWaveBackground

                        View.ResetWaveFormEmpty();

                        CommandRefresh.Execute();
                    }
                }
            }
            else if (e.PropertyName == GetMemberName(() => Settings.Default.Audio_PlayKeepPitch))
            {
                m_Player.Pause();
                m_Player.UseSoundTouch = Settings.Default.Audio_PlayKeepPitch;
                m_Player.Resume();
            }




            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Stroke))
            //{
            //    ColorWaveBars = Settings.Default.AudioWaveForm_Color_Stroke;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Border))
            //{
            //    ColorEnvelopeOutline = Settings.Default.AudioWaveForm_Color_Border;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Fill))
            //{
            //    ColorEnvelopeFill = Settings.Default.AudioWaveForm_Color_Fill;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_CursorBorder))
            //{
            //    ColorPlayhead = Settings.Default.AudioWaveForm_Color_CursorBorder;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_CursorFill))
            //{
            //    ColorPlayheadFill = Settings.Default.AudioWaveForm_Color_CursorFill;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_Phrases))
            //{
            //    ColorMarkers = Settings.Default.AudioWaveForm_Color_Phrases;
            //}
            //else if (e.PropertyName == GetMemberName(() => Settings.Default.AudioWaveForm_Color_TimeText))
            //{
            //    ColorTimeInfoText = Settings.Default.AudioWaveForm_Color_TimeText;
            //}
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

        private bool m_IsSimpleMode;
        public bool IsSimpleMode
        {
            set
            {
                if (m_IsSimpleMode == value)
                {
                    return;
                }
                m_IsSimpleMode = value;

                RaisePropertyChanged(() => IsSimpleMode);
            }
            get { return m_IsSimpleMode; }
        }

        private IInputBindingManager m_ForeignInputBindingManager;
        public IInputBindingManager InputBindingManager
        {
            get { return m_ForeignInputBindingManager ?? m_ShellView; }
            set { m_ForeignInputBindingManager = value; }
        }

        ~AudioPaneViewModel()
        {
#if DEBUG
            Logger.Log("AudioPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
            Settings.Default.PropertyChanged -= OnSettingsPropertyChanged;
        }

        #endregion Construction

        #region Initialization

        internal IAudioPaneView View { get; private set; }
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

            m_LastSetPlayBytePosition = -1;
            //IsWaveFormLoading = false;
            if (EventAggregator != null)
            {
                //EventAggregator.GetEvent<UserInterfaceScaledEvent>().Subscribe(OnUserInterfaceScaled, ThreadOption.UIThread);

                EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded,
                                                                         ProjectLoadedEvent.THREAD_OPTION);
                EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded,
                                                                           ProjectUnLoadedEvent.THREAD_OPTION);

                //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
                //EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, SubTreeNodeSelectedEvent.THREAD_OPTION);

                EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged,
                                                                                    TreeNodeSelectionChangedEvent.
                                                                                        THREAD_OPTION);

                EventAggregator.GetEvent<EscapeEvent>().Subscribe(
                    OnEscape,
                    EscapeEvent.THREAD_OPTION);
            }
        }

        private void OnEscape(object obj)
        {
            OnStopPlayMonitorRecord();
        }

        private void OnInterruptAudioPlayerRecorder()
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action)OnInterruptAudioPlayerRecorder);
                return;
            }

            IsAutoPlay = false;

            if (View != null)
            {
                View.CancelWaveFormLoad(false);
            }

            InterruptAudioPlayerRecorder();
        }


        private void OnStopPlayMonitorRecord()
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action)OnStopPlayMonitorRecord);
                return;
            }

            IsAutoPlay = false;

            if (View != null)
            {
                View.CancelWaveFormLoad(false);
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
                m_RecordAndContinue = false;
                m_InterruptRecording = false;
                CommandStopRecord.Execute();
                return;
            }
        }

        private bool m_InterruptRecording;
        public void InterruptAudioPlayerRecorder()
        {
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
#if !DISABLE_SINGLE_RECORD_FILE
                if (m_RecordAndContinue)
                {
                    return;
                }
#endif
                m_RecordAndContinue = false;
                m_InterruptRecording = true;

                CommandStopRecord.Execute();
                return;
            }
        }

        public event EventHandler InputDeviceAdded;
        public event EventHandler InputDeviceRemoved;

        public event EventHandler OuputDeviceAdded;
        public event EventHandler OuputDeviceRemoved;
        private List<AudioLib.InputDevice> m_PreviousInputDevices = new List<AudioLib.InputDevice>();
        private List<OutputDevice> m_PreviousOutputDevices;


        private void OnDeviceArrived(object sender, EventArgs e)
        {
            if (IsAudioDeviceChanged())
            {
                m_Player.ClearDeviceCache();
                m_Recorder.ClearDeviceCache();
                Console.WriteLine("Audio Device Arrived");
            }

            // TODO: raise the OuputDeviceAdded or InputDeviceAdded event if necessary
            Console.WriteLine("=========>> OnDeviceArrived");
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private void OnDeviceRemoved(object sender, EventArgs e)
        {
            if (IsAudioDeviceChanged())
            {
                m_Player.ClearDeviceCache();
                m_Recorder.ClearDeviceCache();
                Console.WriteLine("Audio Device Removed");
            }

            // implementing simple and fundalmental logic for now
            bool isNewInputAudioDevice = false;
            bool isNewOutputDevice = false;

            if (m_PreviousInputDevices.Count != m_Recorder.InputDevices.Count)
            {
                isNewInputAudioDevice = true;
            }


            if (isNewInputAudioDevice)
            {
                m_PreviousInputDevices = m_Recorder.InputDevices;
            }

            if (m_PreviousOutputDevices.Count != m_Player.OutputDevices.Count)
            {
                m_PreviousOutputDevices = m_Player.OutputDevices;
            }
            // TODO: raise the OuputDeviceRemoved or InputDeviceRemoved event if necessary
            Console.WriteLine("=========>> OnDeviceRemoved");
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }


        private bool IsAudioDeviceChanged()
        {
            // implementing simple and fundalmental logic for now
            bool isNewInputAudioDevice = false;
            bool isNewOutputDevice = false;

            if (m_PreviousInputDevices.Count != m_Recorder.InputDevices.Count)
            {
                isNewInputAudioDevice = true;
            }

            foreach (AudioLib.InputDevice d in m_Recorder.InputDevices)
            {
                bool isDevicePresent = false;
                foreach (AudioLib.InputDevice prevDevice in m_PreviousInputDevices)
                {
                    if (prevDevice.Name == d.Name)
                    {
                        isDevicePresent = true;
                        break;
                    }
                }
                if (!isDevicePresent)
                {
                    isNewInputAudioDevice = true;
                    break;
                }
            }




            if (isNewInputAudioDevice)
            {
                m_PreviousInputDevices = m_Recorder.InputDevices;

            }

            if (m_PreviousOutputDevices.Count != m_Player.OutputDevices.Count)
            {
                m_PreviousOutputDevices = m_Player.OutputDevices;

            }

            foreach (AudioLib.OutputDevice d in m_Player.OutputDevices)
            {
                bool isDevicePresent = false;
                foreach (AudioLib.OutputDevice prevDevice in m_PreviousOutputDevices)
                {
                    if (prevDevice.Name == d.Name)
                    {
                        isDevicePresent = true;
                        break;
                    }
                }
                if (!isDevicePresent)
                {
                    isNewInputAudioDevice = true;
                    break;
                }
            }

            return isNewInputAudioDevice || isNewOutputDevice;
        }

        private void initializeAudioStuff()
        {
            Logger.Log("AudioPaneViewModel.initializeAudioStuff", Category.Debug, Priority.Medium);

            OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_TTS_Voice)));
            //m_SpeechSynthesizer.AddLexicon();

            m_ShellView.DeviceArrived += new EventHandler(OnDeviceArrived);
            m_ShellView.DeviceRemoved += new EventHandler(OnDeviceRemoved);

            m_Player = new AudioPlayer(AudioPlaybackStreamKeepAlive, true);

            OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_OutputDevice)));
            //m_Player.SetOutputDevice(GetWindowsFormsHookControl(), Settings.Default.Audio_OutputDevice);
            //Settings.Default.Audio_OutputDevice = m_Player.OutputDevice.Name;
            Settings.Default.Save();

            m_Player.StateChanged += OnStateChanged_Player;
            m_Player.AudioPlaybackFinished += OnAudioPlaybackFinished;
            m_PreviousOutputDevices = m_Player.OutputDevices;

            //m_Player.ResetVuMeter += OnPlayerResetVuMeter;

            m_Recorder = new AudioRecorder();

            OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));
            //m_Recorder.SetInputDevice(Settings.Default.Audio_InputDevice);
            //Settings.Default.Audio_InputDevice = m_Recorder.InputDevice.Name;
            Settings.Default.Save();

            m_Recorder.StateChanged += OnStateChanged_Recorder;
            m_Recorder.AudioRecordingFinished += OnAudioRecordingFinished;
            m_Recorder.RecordingDirectory = AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY; // Directory.GetCurrentDirectory();
            m_PreviousInputDevices = m_Recorder.InputDevices;

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
                new AudioFormatConvertorSession(AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY, null, false, m_UrakawaSession.IsAcmCodecsDisabled);
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

        //private void OnSubTreeNodeSelected(TreeNode node)
        //{
        //    if (!Dispatcher.CheckAccess())
        //    {
        //        //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
        //        Dispatcher.BeginInvoke(DispatcherPriority.Normal,
        //            (Action<TreeNode>)OnSubTreeNodeSelected, node);
        //        return;
        //    }
        //    //Logger.Log("AudioPaneViewModel.OnSubTreeNodeSelected", Category.Debug, Priority.Medium);


        //    if (View != null)
        //    {
        //        View.CancelWaveFormLoad();
        //    }

        //    if (node == null || State.CurrentTreeNode == null)
        //    {
        //        return;
        //    }
        //    if (State.CurrentSubTreeNode == node)
        //    {
        //        return;
        //    }
        //    if (!IsAudioLoadedWithSubTreeNodes)
        //    {
        //        return;
        //    }
        //    if (!node.IsDescendantOf(State.CurrentTreeNode))
        //    {
        //        return;
        //    }

        //    if (m_Player.CurrentState != AudioPlayer.State.NotReady && m_Player.CurrentState != AudioPlayer.State.Stopped)
        //    {
        //        m_Player.Stop();
        //    }

        //    State.CurrentSubTreeNode = node;

        //    RefreshWaveFormChunkMarkersForCurrentSubTreeNode(true);
        //}

        private void RefreshWaveFormChunkMarkersForCurrentSubTreeNode(bool placePlayHead)
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            TreeNode treeNodeToMatch = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

            if (!State.Audio.HasContent || treeNodeToMatch == null)
            {
                return;
            }

            long bytesRight;
            long bytesLeft;
            int index;
            bool match = State.Audio.FindInPlayStreamMarkers(treeNodeToMatch, out index, out bytesLeft, out bytesRight);

            if (match)
            {
                if (View != null)
                {
                    View.RefreshUI_WaveFormChunkMarkers(bytesLeft, bytesRight);
                }
                if (placePlayHead)
                {
                    long bytes = PlayBytePosition;

                    if (!(
                        bytes >= bytesLeft &&
                        (bytes < bytesRight
                        || index == State.Audio.PlayStreamMarkers.Count - 1 && bytes >= bytesRight)
                        )
                        )
                    {
                        PlayBytePosition = bytesLeft;
                    }
                }
            }
            else
            {
                if (View != null)
                {
                    View.ResetWaveFormChunkMarkers();
                }
                if (placePlayHead)
                {
                    m_LastSetPlayBytePosition = -1;
                    if (View != null)
                    {
                        View.RefreshUI_WaveFormPlayHead(true);
                    }
                }
            }
        }

        private bool m_UpdatingTreeNodeSelection;
        public void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>>)OnTreeNodeSelectionChanged, oldAndNewTreeNodeSelection);
                return;
            }

            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            m_UpdatingTreeNodeSelection = true;

            if (State.Audio.PlayStreamMarkers == null // audio file may have been previously loaded, no relation to the document. 
                || oldTreeNodeSelection.Item1 != newTreeNodeSelection.Item1)
            {
                if (View != null)
                {
                    View.CancelWaveFormLoad(false);
                }
                InterruptAudioPlayerRecorder();

                //if (m_Player.CurrentState != AudioPlayer.State.NotReady && m_Player.CurrentState != AudioPlayer.State.Stopped)
                //{
                //    m_Player.Stop();
                //}

                //if (View != null)
                //{
                //    View.CancelWaveFormLoad(true);
                //}

                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }

                State.ResetAll();

                m_LastSetPlayBytePosition = -1;

                m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;

                if (m_CurrentAudioStreamProvider() == null)
                {
                    if (IsMonitoringAlways)
                    {
                        CommandStartMonitor.Execute();
                    }

                    m_UpdatingTreeNodeSelection = false;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                m_LastSetPlayBytePosition = 0;

                if (State.Audio.HasContent && State.Audio.PlayStreamMarkers != null
                    //&& oldTreeNodeSelection != newTreeNodeSelection
                    )
                {
                    m_UpdatingTreeNodeSelection = false;

                    RefreshWaveFormChunkMarkersForCurrentSubTreeNode(true);
                }

                m_UpdatingTreeNodeSelection = false;

                CommandManager.InvalidateRequerySuggested();

                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                    //View.ResetPeakLines();
                }

                PeakOverloadCountCh1 = 0;
                PeakOverloadCountCh2 = 0;

                //#if DEBUG
                //            Logger.Log("CALLING AudioPlayer_LoadWaveForm (loadAndPlay)", Category.Debug, Priority.Medium);
                //#endif
                AudioPlayer_LoadWaveForm(false);

                if (m_PlayAutoAdvance)
                {
                    CommandPlayAutoAdvance.Execute();
                }
                else if (IsMonitoringAlways)
                {
                    CommandStartMonitor.Execute();
                }
            }
            else
            {
                if (//State.Audio.HasContent && State.Audio.PlayStreamMarkers != null &&
                    oldTreeNodeSelection != newTreeNodeSelection)
                {
                    if (!IsPlaying)
                    {
                        InterruptAudioPlayerRecorder();
                    }
                    else
                    {
                        long bytesRight;
                        long bytesLeft;
                        int index;
                        TreeNode treeNode;
                        bool match = State.Audio.FindInPlayStreamMarkers(PlayBytePosition, out treeNode, out index, out bytesLeft, out bytesRight);
                        if (match)
                        {
                            if (treeNode != newTreeNodeSelection.Item2)
                            {
                                CommandPause.Execute();
                            }
                        }
                        else
                        {
                            CommandPause.Execute();
                        }
                    }

                    m_UpdatingTreeNodeSelection = false;

                    RefreshWaveFormChunkMarkersForCurrentSubTreeNode(true);
                }

                m_UpdatingTreeNodeSelection = false;

                CommandManager.InvalidateRequerySuggested();

                if (IsMonitoringAlways)
                {
                    CommandStartMonitor.Execute();
                }

                //if (IsAutoPlay)
                //{
                //    CommandPlay.Execute();
                //}
            }


            //Logger.Log("AudioPaneViewModel.OnTreeNodeSelected", Category.Debug, Priority.Medium);


            /*if (m_SkipTreeNodeSelectedEvent)
            {
                m_SkipTreeNodeSelectedEvent = false;
                return;
            }*/

            //IsWaveFormLoading = false;


            //if (mustLoadWaveForm)
            //{
            //    loadAndPlay();
            //}
            //else
            //{
            //    checkAndDoAutoPlay();
            //    //bool wasPlaying = (m_Player.CurrentState == AudioPlayer.State.Playing);
            //    //AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying, IsAutoPlay);
            //}
        }

        //private void OnTreeNodeSelected(TreeNode node)
        //{
        //    if (node == null)
        //    {
        //        return;
        //    }

        //    if (!Dispatcher.CheckAccess())
        //    {
        //        //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
        //        Dispatcher.BeginInvoke(DispatcherPriority.Normal,
        //            (Action<TreeNode>)OnTreeNodeSelected, node);
        //        return;
        //    }

        //    //Logger.Log("AudioPaneViewModel.OnTreeNodeSelected", Category.Debug, Priority.Medium);


        //    if (View != null)
        //    {
        //        View.CancelWaveFormLoad();
        //    }

        //    if (State.CurrentTreeNode == node)
        //    {
        //        return;
        //    }

        //    /*if (m_SkipTreeNodeSelectedEvent)
        //    {
        //        m_SkipTreeNodeSelectedEvent = false;
        //        return;
        //    }*/

        //    if (m_Player.CurrentState != AudioPlayer.State.NotReady && m_Player.CurrentState != AudioPlayer.State.Stopped)
        //    {
        //        m_Player.Stop();
        //    }

        //    if (AudioPlaybackStreamKeepAlive)
        //    {
        //        ensurePlaybackStreamIsDead();
        //    }

        //    State.ResetAll();

        //    m_LastPlayHeadTime = -1;
        //    IsWaveFormLoading = false;

        //    State.CurrentTreeNode = node;

        //    m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;

        //    if (m_CurrentAudioStreamProvider() == null)
        //    {
        //        return;
        //    }

        //    //m_LastPlayHeadTime = 0; Set after waveform loaded

        //    loadAndPlay();
        //}


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

        public void OnProjectUnLoaded(Project project)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<Project>)OnProjectUnLoaded, project);
                return;
            }

            TotalDocumentAudioDurationInLocalUnits = 0;

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled -= OnUndoRedoManagerChanged;

            OnProjectLoaded(null);
        }

        public void OnProjectLoaded(Project project)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<Project>)OnProjectLoaded, project);
                return;
            }

            if (m_UrakawaSession.IsXukSpine)
            {
                return;
            }

            TotalSessionAudioDurationInLocalUnits = 0;

            if (View != null)
            {
                View.CancelWaveFormLoad(false);
            }
            InterruptAudioPlayerRecorder();

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            State.ResetAll();

            AudioClipboard = null;
            m_LastSetPlayBytePosition = -1;
            //IsWaveFormLoading = false;

            //var shell = Container.Resolve<IShellView>();
            //shell.DocumentProject
            if (project != null)
            {
                m_AudioFormatConvertorSession =
                    new AudioFormatConvertorSession(
                    //AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY,
                    project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath,
                project.Presentations.Get(0).MediaDataManager.DefaultPCMFormat,
                false,
                m_UrakawaSession.IsAcmCodecsDisabled);

                project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;

                m_Recorder.RecordingDirectory = project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath; //AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY

                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Ready);
                }

                if (IsMonitoringAlways)
                {
                    CommandStartMonitor.Execute();
                }
            }
            else
            {
                m_AudioFormatConvertorSession = null;

                m_Recorder.RecordingDirectory = AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY; // Directory.GetCurrentDirectory();

                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("-"); // TODO Localize 
                }
            }

            RaisePropertyChanged(() => State.Audio.PcmFormat);
        }

        #endregion Event / Callbacks

        #region Private Class Attributes

        private const bool AudioPlaybackStreamKeepAlive = true;

        private long getByteOffset(TreeNode treeNode, ManagedAudioMedia managedMedia)
        {
            //if (!State.IsTreeNodeShownInAudioWaveForm(treeNode))
            //{
            //    return 0;
            //}

            long byteOffset = 0;

            //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            if (State.Audio.PlayStreamMarkers != null)
            {
                long bytesRight;
                long bytesLeft;
                int index;
                bool match = State.Audio.FindInPlayStreamMarkers(treeNode, out index, out bytesLeft, out bytesRight);

                if (match)
                {
                    byteOffset = bytesLeft;
                }
                else
                {
                    return 0;
                }
            }

            if (managedMedia == null)
            {
                return byteOffset;
            }

#if ENABLE_SEQ_MEDIA

            SequenceMedia seqManAudioMedia = treeNode.GetManagedAudioSequenceMedia();
            if (seqManAudioMedia != null)
            {
                Debug.Fail("SequenceMedia is normally removed at import time...have you tried re-importing the DAISY book ?");

                foreach (Media media in seqManAudioMedia.ChildMedias.ContentsAs_Enumerable)
                {
                    var manMedia = (ManagedAudioMedia)media;
                    if (media == managedMedia)
                    {
                        break;
                    }
                    byteOffset += manMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(manMedia.Duration.AsLocalUnits);
                }
            }
            
#endif //ENABLE_SEQ_MEDIA

            return byteOffset;
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

        [NotifyDependsOnEx("PlayStream", typeof(StreamStateData))]
        [NotifyDependsOn("IsSelectionSet")]
        public bool TimeStringSelectionVisible
        {
            get
            {
                return State.Audio.HasContent && IsSelectionSet;
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

                return FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(State.Selection.SelectionBeginBytePosition));
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

                return FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(State.Selection.SelectionEndBytePosition));
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

                return FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(State.Selection.SelectionEndBytePosition - State.Selection.SelectionBeginBytePosition));
            }
        }

        [NotifyDependsOnEx("PlayStream", typeof(StreamStateData))]
        [NotifyDependsOnEx("DataLength", typeof(StreamStateData))]
        public string TimeStringTotalWaveform
        {
            get
            {
                if (!State.Audio.HasContent)
                {
                    return "";
                }

                return FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(State.Audio.DataLength));
            }
        }

        private static readonly PropertyChangedEventArgs m_RecorderCurrentDurationArgs
            = new PropertyChangedEventArgs(@"RecorderCurrentDurationBytePosition");
        public long RecorderCurrentDurationBytePosition
        {
            get
            {
#if !DISABLE_SINGLE_RECORD_FILE
                if (m_RecordAndContinue_StopBytePos >= 0) // m_RecordAndContinue && 
                {
                    return (long)m_Recorder.CurrentDurationBytePosition - m_RecordAndContinue_StopBytePos;
                }
#endif
                return (long)m_Recorder.CurrentDurationBytePosition;
            }
        }


        //public static string FormatTimeSpan_Units(Time time)
        //{
        //    if (Settings.Default.UseFriendlyTimeFormat)
        //        return time.Format_H_MN_S_MS();

        //    return time.Format_Standard();
        //}


        //public static string FormatTimeSpan_Units(TimeSpan timeSpan)
        //{
        //    if (Settings.Default.UseFriendlyTimeFormat)
        //        return Time.Format_H_MN_S_MS(timeSpan);

        //    return Time.Format_Standard(timeSpan);
        //}


        public static string FormatTimeSpan_Units(long timeInLocalUnits)
        {
            TimeSpan timeSpan = Time.ConvertFromLocalUnitsToTimeSpan(timeInLocalUnits);

            if (Settings.Default.UseFriendlyTimeFormat)
                return Time.Format_H_MN_S_MS(timeSpan);

            return Time.Format_Standard(timeSpan);
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

        [NotifyDependsOn("IsMonitoringAlways")]
        [NotifyDependsOn("IsMonitoring")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOnEx("PlayStream", typeof(StreamStateData))]
        public bool TimeStringCurrentVisible
        {
            get
            {
                return State.Audio.HasContent || IsRecording ||
                    (IsMonitoring && !IsMonitoringAlways);
            }
        }

        public string m_TimeStringOther = String.Empty;

        private static readonly PropertyChangedEventArgs m_TimeStringCurrentArgs
            = new PropertyChangedEventArgs("TimeStringCurrent");
        [NotifyDependsOn("IsPlaying")]
        [NotifyDependsOnEx("PlayStream", typeof(StreamStateData))]
        [NotifyDependsOn("TimeStringCurrentVisible")]
        [NotifyDependsOn("TimeStringTotalWaveform")]
        [NotifyDependsOnFast("m_LastPlayBytePositionArgs", "m_TimeStringCurrentArgs")]
        [NotifyDependsOnFast("m_RecorderCurrentDurationArgs", "m_TimeStringCurrentArgs")]
        public String TimeStringCurrent
        {
            get
            {
                if (IsRecording
                    || (IsMonitoring && !IsMonitoringAlways))
                {
                    //if ()
                    //{
                    //    return string.Empty; // @"...";
                    //}

                    return FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(RecorderCurrentDurationBytePosition));
                }

                string strToDisplay = null;


                if (!State.Audio.HasContent || IsWaveFormLoading)
                {
                    return m_TimeStringOther;
                }

                if (IsPlaying
                    || PlayBytePosition >= 0 && (
                    //m_Player.CurrentState == AudioPlayer.State.Paused ||
                                                 IsStopped
                                             ))
                {
                    if (IsPlaying && m_RecordAfterPlayOverwriteSelection > 0)
                    {
                        strToDisplay = FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(m_RecordAfterPlayOverwriteSelection - PlayBytePosition));
                    }
                    else
                    {
                        strToDisplay = FormatTimeSpan_Units(State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(PlayBytePosition));
                    }
                }

                if (!String.IsNullOrEmpty(strToDisplay))
                {
                    return (m_RecordAfterPlayOverwriteSelection > 0 ? Tobi_Plugin_AudioPane_Lang.Countdown : "") + strToDisplay + " / " + TimeStringTotalWaveform;
                }

                return String.Empty;
            }
        }

        public void SetPlayHeadTimeBypassAutoPlay(long bytePosition)
        {
            bool wasAutoPlay = IsAutoPlay;
            if (wasAutoPlay) IsAutoPlay = false;
            PlayBytePosition = bytePosition;
            if (wasAutoPlay) IsAutoPlay = true;
        }

        [NotifyDependsOn("TotalSessionAudioDurationInLocalUnits")]
        public string TotalSessionAudioDurationString
        {
            get
            {
                return Tobi_Plugin_AudioPane_Lang.SessionDuration
                    + (TotalSessionAudioDurationInLocalUnits < 0
                    ? "-" + FormatTimeSpan_Units(-TotalSessionAudioDurationInLocalUnits)
                    : FormatTimeSpan_Units(TotalSessionAudioDurationInLocalUnits));
            }
        }
        private long m_TotalSessionAudioDurationInLocalUnits;
        public long TotalSessionAudioDurationInLocalUnits
        {
            get
            {
                return m_TotalSessionAudioDurationInLocalUnits;
            }
            set
            {
                if (m_TotalSessionAudioDurationInLocalUnits == value)
                {
                    return;
                }
                m_TotalSessionAudioDurationInLocalUnits = value;
                RaisePropertyChanged(() => TotalSessionAudioDurationInLocalUnits);
            }
        }

        [NotifyDependsOn("TotalDocumentAudioDurationInLocalUnits")]
        public string TotalDocumentAudioDurationString
        {
            get
            {
                return Tobi_Plugin_AudioPane_Lang.TotalDuration
                    + FormatTimeSpan_Units(TotalDocumentAudioDurationInLocalUnits);
            }
        }

        private long m_TotalDocumentAudioDurationInLocalUnits;
        public long TotalDocumentAudioDurationInLocalUnits
        {
            get
            {
                return m_TotalDocumentAudioDurationInLocalUnits;
            }
            set
            {
                if (m_TotalDocumentAudioDurationInLocalUnits == value)
                {
                    return;
                }
                m_TotalDocumentAudioDurationInLocalUnits = value;
                RaisePropertyChanged(() => TotalDocumentAudioDurationInLocalUnits);
            }
        }

        private static readonly PropertyChangedEventArgs m_LastPlayBytePositionArgs
            = new PropertyChangedEventArgs(@"PlayBytePosition");
        private long m_LastSetPlayBytePosition;
        public long PlayBytePosition
        {
            get
            {
                if (IsPlaying) return m_Player.CurrentBytePosition;
                return m_LastSetPlayBytePosition;
            }
            set
            {
                if (m_LastSetPlayBytePosition == value)
                {
                    if (View != null)
                    {
                        View.RefreshUI_WaveFormPlayHead(false);
                    }
                    RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);

                    if (IsAutoPlay)
                    {
                        CommandPlay.Execute();
                    }
                    return;
                }

                m_LastSetPlayBytePosition = value;

                if (m_LastSetPlayBytePosition < 0)
                {
                    Debug.Fail(String.Format("m_LastPlayHeadTime < 0 ?? {0}", m_LastSetPlayBytePosition));
                    m_LastSetPlayBytePosition = 0;
                }

                if (State.Audio.HasContent)
                {
                    if (m_LastSetPlayBytePosition > State.Audio.DataLength)
                    {
                        Debug.Fail(String.Format("m_LastSetPlayBytePosition > DataLength ?? {0}", m_LastSetPlayBytePosition));
                        m_LastSetPlayBytePosition = State.Audio.DataLength;
                    }
                }

                RaisePropertyChanged(m_LastPlayBytePositionArgs);
                //RaisePropertyChanged(() => LastPlayHeadTime);

                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead(false);
                }

                if (!State.Audio.HasContent)
                {
                    return;
                }
                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                if (treeNodeSelection.Item1 == null)
                {
                    if (IsAutoPlay)
                    {
                        CommandPlay.Execute();
                    }
                    return;
                }

                long bytesRight;
                long bytesLeft;
                int index;
                TreeNode treeNode;
                bool match = State.Audio.FindInPlayStreamMarkers(m_LastSetPlayBytePosition, out treeNode, out index, out bytesLeft, out bytesRight);

                TreeNode treeNodeToMatch = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;


                if (match)
                {
                    if (treeNodeToMatch == treeNode || treeNodeToMatch.IsDescendantOf(treeNode))
                    {
                        if (IsAutoPlay)
                        {
                            CommandPlay.Execute();
                        }
                        return;
                    }

                    if (View != null)
                    {
                        View.RefreshUI_WaveFormChunkMarkers(bytesLeft, bytesRight);
                    }

                    if (m_UpdatingTreeNodeSelection) return;

                    if (IsAutoPlay)
                    {
                        CommandPlay.Execute();
                    }

                    //Logger.Log("++++++ PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.LastPlayHeadTime", Category.Debug, Priority.Medium);

                    //#if DEBUG
                    //                    Stopwatch stopwatch = new Stopwatch();
                    //                    stopwatch.Start();
                    //#endif //DEBUG
                    if (!TheDispatcher.CheckAccess())
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }

                    if (true || !IsPlaying)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(treeNode);
                    }
                    else
                    {
                        TheDispatcher.BeginInvoke((Action)(() =>
                        {
                            m_UrakawaSession.PerformTreeNodeSelection(treeNode);
                        }),
                            DispatcherPriority.Background);
                    }

                    //#if DEBUG
                    //                    TimeSpan time = stopwatch.Elapsed;
                    //                    Logger.Log("%%%%%%%%%%%%%% PLAYBACK CHUNK SWITCH TREENODE SELECT: " + time, Category.Debug, Priority.Medium);
                    //                    stopwatch.Stop();
                    //#endif //DEBUG
                }
                else
                {
                    Debug.Fail("audio chunk not found ??");
                    return;
                }
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
            if (!IsPlaying
                && !IsRecording
                && !IsMonitoring)
            {
                if (View != null)
                {
                    View.RefreshUI_PeakMeterBlackout(true);
                    //View.ResetPeakLines();
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

            AudioPlayer_UpdateWaveFormPlayHead();
        }

        #region Event / Callbacks

        private void OnPeakMeterUpdated(object sender, VuMeter.PeakMeterUpdateEventArgs e)
        {
            if (!TheDispatcher.CheckAccess())
            {
                TheDispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, VuMeter.PeakMeterUpdateEventArgs>)OnPeakMeterUpdated_, sender, e);
                return;
            }
            OnPeakMeterUpdated_(sender, e);
        }
        private void OnPeakMeterUpdated_(object sender, VuMeter.PeakMeterUpdateEventArgs e)
        {
            PCMFormatInfo pcmInfo = State.Audio.GetCurrentPcmFormat();
            if (pcmInfo == null) return;

            if (IsRecording ||
                (IsMonitoring && !IsMonitoringAlways))
            {
                if (View != null)
                {
                    View.TimeMessageRefresh();
                }

                RaisePropertyChanged(m_RecorderCurrentDurationArgs);
                //RaisePropertyChanged(() => RecorderCurrentDuration);
            }

            if (IsPlaying && m_RecordAfterPlayOverwriteSelection > 0)
            {
                if (View != null)
                {
                    View.TimeMessageRefresh();
                }
            }

            if (e.PeakDb != null && e.PeakDb.Length > 0)
            {
                m_PeakMeterValues[0] = e.PeakDb[0];

                if (pcmInfo.Data.NumberOfChannels > 1)
                {
                    m_PeakMeterValues[1] = e.PeakDb[1];
                }
                UpdatePeakMeter();
            }
        }

        private void OnPeakMeterOverloaded(object sender, VuMeter.PeakOverloadEventArgs e)
        {
            if (!TheDispatcher.CheckAccess())
            {
                TheDispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, VuMeter.PeakOverloadEventArgs>)OnPeakMeterOverloaded_, sender, e);
                return;
            }

            OnPeakMeterOverloaded_(sender, e);

            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }
        private void OnPeakMeterOverloaded_(object sender, VuMeter.PeakOverloadEventArgs e)
        {
            if (e != null)
            {
                if (!IsRecording)
                {
                    AudioCues.PlayHi();
                }

                if (e.Channel == 1)
                {
                    PeakOverloadCountCh1++;
                }
                else if (e.Channel == 2)
                {
                    PeakOverloadCountCh2++;
                }
#if DEBUG
                else
                {
                    Debugger.Break();
                }
#endif //DEBUG
            }
        }

        #endregion Event / Callbacks

        #endregion VuMeter / PeakMeter

        private List<TreeNodeAndStreamSelection> getAudioSelectionData()
        {
            var listOfTreeNodeAndStreamSelection = new List<TreeNodeAndStreamSelection>();

            State.Audio.FindInPlayStreamMarkersAndDo(State.Selection.SelectionBeginBytePosition,
               (bytesLeft, bytesRight, markerTreeNode, index)
               =>
               {
                   if (listOfTreeNodeAndStreamSelection.Count == 0)
                   {
                       bool rightBoundaryIsAlsoHere = (State.Selection.SelectionEndBytePosition < bytesRight
                                                       ||
                                                       index == (State.Audio.PlayStreamMarkers.Count - 1) &&
                                                       State.Selection.SelectionEndBytePosition >= bytesRight);

                       TreeNodeAndStreamSelection data = new TreeNodeAndStreamSelection()
                       {
                           m_TreeNode = markerTreeNode,
                           m_LocalStreamLeftMark = State.Selection.SelectionBeginBytePosition - bytesLeft,
                           m_LocalStreamRightMark = (rightBoundaryIsAlsoHere ? State.Selection.SelectionEndBytePosition - bytesLeft : -1)
                       };
                       listOfTreeNodeAndStreamSelection.Add(data);

                       if (rightBoundaryIsAlsoHere)
                       {
                           return -1; // break;
                       }
                       return State.Selection.SelectionEndBytePosition; // continue with new bytesToMatch
                   }
                   else
                   {
                       TreeNodeAndStreamSelection data = new TreeNodeAndStreamSelection()
                       {
                           m_TreeNode = markerTreeNode,
                           m_LocalStreamLeftMark = -1,
                           m_LocalStreamRightMark = State.Selection.SelectionEndBytePosition - bytesLeft
                       };

                       if (data.m_LocalStreamRightMark > 0)
                           listOfTreeNodeAndStreamSelection.Add(data);

                       return -1; // break;
                   }
               }
                ,
               (bytesToMatch_, bytesLeft, bytesRight, markerTreeNode)
               =>
               {
                   if (listOfTreeNodeAndStreamSelection.Count > 0)
                   {
                       TreeNodeAndStreamSelection data = new TreeNodeAndStreamSelection()
                       {
                           m_TreeNode = markerTreeNode,
                           m_LocalStreamLeftMark = -1,
                           m_LocalStreamRightMark = -1
                       };
                       listOfTreeNodeAndStreamSelection.Add(data);
                   }

                   return bytesToMatch_; // continue with same bytesToMatch
               }
                );

            return listOfTreeNodeAndStreamSelection;
        }
    }
}
