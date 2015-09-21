using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.core;
using urakawa.data;
using urakawa.media.data.audio;
using urakawa.media.timing;
using urakawa.xuk;
using InputDevice = AudioLib.InputDevice;

namespace Tobi.Plugin.AudioPane
{
    public struct DeferredRecordingData
    {
        public string RecordedFilePath;
        public TreeNode TreeNode1;
        public TreeNode TreeNode2;
        public long PlayBytePosition;
        public long SelectionBeginBytePosition;
        public long SelectionEndBytePosition;
        //#if !DISABLE_SINGLE_RECORD_FILE
        public long RecordAndContinue_StopBytePos;
        //#endif
    }

    public partial class AudioPaneViewModel
    {
        #region Audio Recorder

        public RichDelegateCommand CommandStartRecord { get; private set; }
        public RichDelegateCommand CommandStopRecord { get; private set; }
        public RichDelegateCommand CommandStopRecordAndContinue { get; private set; }

        public RichDelegateCommand CommandTogglePlayPreviewBeforeRecord { get; private set; }
        public RichDelegateCommand CommandToggleRecordOverwrite { get; private set; }
        
        public RichDelegateCommand CommandStartMonitor { get; private set; }
        public RichDelegateCommand CommandStopMonitor { get; private set; }

        private bool m_RecordAndContinue = false;

        private bool m_punchInRecordOverSelection = false;
        
        //#if !DISABLE_SINGLE_RECORD_FILE
        private long m_RecordAndContinue_StopBytePos = -1;
        private bool m_RecordAndContinue_StopSingleFileRecord = false;
        //#endif

        private void initializeCommands_Recorder()
        {
            Stopwatch stopWatchRecorder = null;

            CommandStopRecordAndContinue = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecordAndContinue_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecordAndContinue_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("start-here"), //weather-clear-night -- emblem-symbolic-link
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopRecordAndContinue", Category.Debug, Priority.Medium);

                    if (stopWatchRecorder != null)
                    {
                        stopWatchRecorder.Stop();
                        if (stopWatchRecorder.ElapsedMilliseconds <= 100)
                        {
                            Console.WriteLine("stopWatchRecorder.ElapsedMilliseconds<=100, skipping stop record");
                            stopWatchRecorder.Start();
                            return;
                        }
                        Console.WriteLine("stopWatchRecorder.ElapsedMilliseconds, elapsed record :" + stopWatchRecorder.ElapsedMilliseconds);
                    }
                    stopWatchRecorder = null;

                    IsAutoPlay = false;
                    m_RecordAndContinue = true;
                    m_InterruptRecording = false;

                    if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                    {
                        m_RecordAndContinue_StopBytePos = (long)m_Recorder.CurrentDurationBytePosition_BufferLookAhead;
                        OnAudioRecordingFinished(null,
                                                 new AudioRecorder.AudioRecordingFinishEventArgs(
                                                     m_Recorder.RecordedFilePath));
                    }
                    else
                    {
                        m_Recorder.StopRecording();

                        if (EventAggregator != null)
                        {
                            EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.RecordingStopped);
                        }
                    }
                    //ENABLE_SINGLE_RECORD_FILE
                },
                () =>
                {
                    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return !IsWaveFormLoading && IsRecording
                           && m_UrakawaSession.DocumentProject != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StopRecordAndContinue));

            m_ShellView.RegisterRichCommand(CommandStopRecordAndContinue);
            //
            CommandStopRecord = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecord_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecord_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-stop"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopRecord", Category.Debug, Priority.Medium);

                    if (stopWatchRecorder != null)
                    {
                        stopWatchRecorder.Stop();
                        if (stopWatchRecorder.ElapsedMilliseconds <= 50)
                        {
                            Console.WriteLine("stopWatchRecorder.ElapsedMilliseconds<=50, skipping stop record");
                            stopWatchRecorder.Start();
                            return;
                        }
                        Console.WriteLine("stopWatchRecorder.ElapsedMilliseconds, elapsed record :" + stopWatchRecorder.ElapsedMilliseconds);
                    }
                    stopWatchRecorder = null;

                    m_RecordAndContinue = false;
                    m_Recorder.StopRecording();

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.RecordingStopped);
                    }

                    if (IsMonitoringAlways)
                    {
                        CommandStartMonitor.Execute();
                    }
                },
                () => !IsWaveFormLoading && IsRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopRecord));

            m_ShellView.RegisterRichCommand(CommandStopRecord);
            //
            CommandStartRecord = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStartRecord_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStartRecord_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-record"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartRecord", Category.Debug, Priority.Medium);

                    m_RecordAndContinue_StopBytePos = -1;

                    if (stopWatchRecorder != null)
                    {
                        Console.WriteLine("stopWatchRecorder != null, skipping start record");
                        return;
                    }

                    if (IsMonitoring)
                    {
                        CommandStopMonitor.Execute();
                    }

                    IsAutoPlay = false;

                    m_punchInRecordOverSelection = false;
                    if (IsPlaying) // Punch-in recording
                    {
                        SetRecordAfterPlayOverwriteSelection(-1);

                        m_punchInRecordOverSelection = true;

                        CommandPause.Execute();

                        CommandSelectRight.Execute();
                    }
                    else if (Settings.Default.Audio_EnableRecordOverwrite
                                && !IsSelectionSet)
                    {
                        SetRecordAfterPlayOverwriteSelection(-1);

                        m_punchInRecordOverSelection = true;

                        if (PlayBytePosition >= 0)
                        {
                            CommandSelectRight.Execute();
                        }
                        else
                        {
                            CommandSelectAll.Execute();
                        }
                    }

                    if (IsWaveFormLoading && View != null)
                    {
                        View.CancelWaveFormLoad(true);
                    }

                    m_RecordAndContinue = false;

                    if (m_UrakawaSession.DocumentProject == null)
                    {
                        SetRecordAfterPlayOverwriteSelection(-1);

                        State.ResetAll();

                        State.Audio.PcmFormatRecordingMonitoring = new PCMFormatInfo();
                    }
                    else
                    {
                        Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                        if (treeNodeSelection.Item1 == null)
                        {
                            SetRecordAfterPlayOverwriteSelection(-1);

                            return;
                        }

                        if (!m_punchInRecordOverSelection) // let's check auto punch in/out based on audio selection
                        {
                            var bytesForRequiredOffsetTime =
                                m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.
                                    Data.ConvertTimeToBytes(150 * AudioLibPCMFormat.TIME_UNIT);
                            if (State.Selection.SelectionBeginBytePosition > 0
                                && PlayBytePosition >= 0
                                && PlayBytePosition < State.Selection.SelectionBeginBytePosition - bytesForRequiredOffsetTime)
                            {
                                AudioPlayer_PlayFromTo(PlayBytePosition, State.Selection.SelectionBeginBytePosition);
                                SetRecordAfterPlayOverwriteSelection(State.Selection.SelectionBeginBytePosition);
                                return;
                            }
                            else if (Settings.Default.Audio_EnablePlayPreviewBeforeRecord && m_RecordAfterPlayOverwriteSelection < 0 && PlayBytePosition > 0)
                            {
                                var playbackStartBytePos =
                                    m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.
                                        Data.ConvertTimeToBytes((long)Settings.Default.AudioWaveForm_TimeStepPlayPreview * 2 * AudioLibPCMFormat.TIME_UNIT);
                                playbackStartBytePos = PlayBytePosition - playbackStartBytePos;
                                if (playbackStartBytePos < 0)
                                {
                                    playbackStartBytePos = 0;
                                }

                                SetRecordAfterPlayOverwriteSelection(State.Selection.SelectionBeginBytePosition > 0 ? State.Selection.SelectionBeginBytePosition : PlayBytePosition);
                                AudioPlayer_PlayFromTo(playbackStartBytePos, PlayBytePosition);
                                return;
                            }

                            SetRecordAfterPlayOverwriteSelection(-1);
                        }

                        DebugFix.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatRecordingMonitoring = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                    }

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));

                    stopWatchRecorder = Stopwatch.StartNew();
                    m_Recorder.StartRecording(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Recording);
                    }
                },
                () =>
                {
                    return (!IsMonitoring || IsMonitoringAlways)
                        && !IsRecording
                        && !IsWaveFormLoading //!IsPlaying && 
                        && canDeleteInsertReplaceAudio();
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopRecord));

            m_ShellView.RegisterRichCommand(CommandStartRecord);

            //
            //
            CommandStopMonitor = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopMonitor_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopMonitor_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-stop"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopMonitor", Category.Debug, Priority.Medium);

                    m_Recorder.StopRecording();

                    //AudioCues.PlayTockTock();

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.MonitoringStopped);
                    }

                    State.Audio.PcmFormatRecordingMonitoring = null;
                },
                () => !IsWaveFormLoading
                    && IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStopMonitor);
            //

            CommandStartMonitor = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStartMonitor_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStartMonitor_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("audio-input-microphone"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartMonitor", Category.Debug, Priority.Medium);

                    m_RecordAndContinue_StopBytePos = -1; // to avoid display of m_RecordAndContinue time stamp

                    IsAutoPlay = false;

                    if (m_UrakawaSession.DocumentProject == null)
                    {
                        State.ResetAll();

                        State.Audio.PcmFormatRecordingMonitoring = new PCMFormatInfo();

                        //m_PcmFormatOfAudioToInsert = IsAudioLoaded ? State.Audio.PcmFormat : new PCMFormatInfo();
                        //m_Recorder.InputDevice.Capture.Caps.Format44KhzMono16Bit
                    }
                    else
                    {
                        DebugFix.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatRecordingMonitoring = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                    }

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));

                    //AudioCues.PlayTock();

                    m_Recorder.StartMonitoring(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Monitoring);
                    }
                },
                () => !IsWaveFormLoading
                    && !IsPlaying
                    && !IsRecording
                    && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStartMonitor);
            //

            CommandTogglePlayPreviewBeforeRecord = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioTogglePlayPreviewBeforeRecord_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioTogglePlayPreviewBeforeRecord_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeGionIcon("Gion_music-player"),
                () =>
                {
                    Settings.Default.Audio_EnablePlayPreviewBeforeRecord =
                        !Settings.Default.Audio_EnablePlayPreviewBeforeRecord;

                    RaisePropertyChanged(() => RecordPlayPreviewString);

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.AudioRecordPlayPreview + (Settings.Default.Audio_EnablePlayPreviewBeforeRecord ? " [ON]" : " [OFF]"));
                    }
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_TogglePlayPreviewBeforeRecord));

            m_ShellView.RegisterRichCommand(CommandTogglePlayPreviewBeforeRecord);
            //
            CommandToggleRecordOverwrite = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioToggleRecordOverwrite_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioToggleRecordOverwrite_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeGionIcon("Gion_music-player"),
                () =>
                {
                    Settings.Default.Audio_EnableRecordOverwrite =
                        !Settings.Default.Audio_EnableRecordOverwrite;

                    RaisePropertyChanged(() => RecordOverwriteString);

                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.AudioRecordOverwrite + (Settings.Default.Audio_EnableRecordOverwrite ? " [ON]" : " [OFF]"));
                    }
                },
                () => true,
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_TogglePlayPreviewBeforeRecord)
                );

            m_ShellView.RegisterRichCommand(CommandToggleRecordOverwrite);
        }

        private long m_RecordAfterPlayOverwriteSelection = -1;
        public void SetRecordAfterPlayOverwriteSelection(long selectionBeginBytePosition)
        {
            m_RecordAfterPlayOverwriteSelection = selectionBeginBytePosition;
            if (View != null)
            {
                if (selectionBeginBytePosition > 0)
                {
                    View.TimeMessageShow();
                }
                else
                {
                    View.TimeMessageHide();
                }
            }
        }

        internal AudioRecorder m_Recorder;

        [NotifyDependsOn("IsMonitoringAlways")]
        [NotifyDependsOn("IsRecording")]
        [NotifyDependsOn("IsMonitoring")]
        public bool CanSwapInputDevice
        {
            get
            {
                return (!IsMonitoring || IsMonitoringAlways) && !IsRecording;
            }
        }

        private List<InputDevice> m_InputDevices;
        public List<InputDevice> InputDevices
        {
            get
            {

                m_InputDevices = m_Recorder.InputDevices;
                return m_InputDevices;
            }
        }
        public InputDevice InputDevice
        {
            get
            {
                if (m_InputDevices != null)
                    foreach (var inputDevice in m_InputDevices)
                    {
                        if (inputDevice.Name == m_Recorder.InputDevice.Name)
                            return inputDevice;
                    }
                return m_Recorder.InputDevice;
            }
            set
            {
                if (value != null
                    && (m_Recorder.InputDevice == null || m_Recorder.InputDevice.Name != value.Name))
                {
                    Settings.Default.Audio_InputDevice = value.Name;
                }
            }
        }

        //// ReSharper disable RedundantDefaultFieldInitializer
        //private bool m_IsAutoRecordNext = false;
        //// ReSharper restore RedundantDefaultFieldInitializer
        //public bool IsAutoRecordNext
        //{
        //    get
        //    {
        //        return m_IsAutoRecordNext;
        //    }
        //    set
        //    {
        //        if (m_IsAutoRecordNext == value) return;
        //        m_IsAutoRecordNext = value;

        //        RaisePropertyChanged(() => IsAutoRecordNext);
        //    }
        //}

        private void OnAudioRecordingFinished(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, AudioRecorder.AudioRecordingFinishEventArgs>)OnAudioRecordingFinished_,
                                  sender, e);
                return;
            }

            OnAudioRecordingFinished_(sender, e);
        }

        private List<DeferredRecordingData> m_DeferredRecordingDataItems;

        private void registerRecordedAudioFileForDeferredAddition(string filePath)
        {
            CommandPause.Execute();

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            var data = new DeferredRecordingData
            {
                RecordedFilePath = filePath,
                TreeNode1 = treeNodeSelection.Item1,
                TreeNode2 = treeNodeSelection.Item2,
                PlayBytePosition = PlayBytePosition,
                SelectionBeginBytePosition = State.Selection.SelectionBeginBytePosition,
                SelectionEndBytePosition = State.Selection.SelectionEndBytePosition,
            };
            if (!Settings.Default.Audio_DisableSingleWavFileRecord)
            {
                data.RecordAndContinue_StopBytePos = m_RecordAndContinue ? m_RecordAndContinue_StopBytePos : -1;
            }

            if (m_DeferredRecordingDataItems == null)
            {
                m_DeferredRecordingDataItems = new List<DeferredRecordingData>();
            }
            m_DeferredRecordingDataItems.Add(data);
        }

        private void checkAndAddDeferredRecordingDataItems()
        {
            if (m_DeferredRecordingDataItems == null) return;

            IsAutoPlay = false;

            bool needsRefresh = false;

            bool skipDrawing = Settings.Default.AudioWaveForm_DisableDraw;
            Settings.Default.AudioWaveForm_DisableDraw = true;


            //#if !DISABLE_SINGLE_RECORD_FILE
            string previousRecordedFile = null;
            FileDataProvider currentFileDataProvider = null;
            AudioLibPCMFormat currentPcmFormat = null;
            long currentPcmDataLength = -1;
            long previousBytePosEnd = 0;
            //#endif


            foreach (var deferredRecordingDataItem in m_DeferredRecordingDataItems)
            {
                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.PerformTreeNodeSelection(deferredRecordingDataItem.TreeNode1, false, deferredRecordingDataItem.TreeNode2);
                if (treeNodeSelection.Item1 != deferredRecordingDataItem.TreeNode1
                    || treeNodeSelection.Item2 != deferredRecordingDataItem.TreeNode2)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    continue;
                }

                if (IsWaveFormLoading && View != null)
                {
                    View.CancelWaveFormLoad(true);
                }

                if (deferredRecordingDataItem.PlayBytePosition >= 0)
                {
                    PlayBytePosition = deferredRecordingDataItem.PlayBytePosition;
                }
                else
                {
                    m_LastSetPlayBytePosition = deferredRecordingDataItem.PlayBytePosition;
                }

                if (PlayBytePosition != deferredRecordingDataItem.PlayBytePosition)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    continue;
                }

                if (deferredRecordingDataItem.SelectionBeginBytePosition >= 0
                    && deferredRecordingDataItem.SelectionEndBytePosition > 0)
                {
                    State.Selection.SetSelectionBytes(deferredRecordingDataItem.SelectionBeginBytePosition, deferredRecordingDataItem.SelectionEndBytePosition);
                }
                else
                {
                    State.Selection.ClearSelection();
                }

                if (State.Selection.SelectionBeginBytePosition != deferredRecordingDataItem.SelectionBeginBytePosition
                    || State.Selection.SelectionEndBytePosition != deferredRecordingDataItem.SelectionEndBytePosition)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    continue;
                }

                if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                {

                    TreeNode treeNode = deferredRecordingDataItem.TreeNode1 ?? deferredRecordingDataItem.TreeNode2;

                    if (string.IsNullOrEmpty(previousRecordedFile)
                        || previousRecordedFile != deferredRecordingDataItem.RecordedFilePath)
                    {
                        PCMFormatInfo pcmInfo = State.Audio.PcmFormatRecordingMonitoring;
                        currentPcmFormat = (pcmInfo != null ? pcmInfo.Copy().Data : null);
                        if (currentPcmFormat == null)
                        {
                            Stream fileStream = File.Open(deferredRecordingDataItem.RecordedFilePath, FileMode.Open, FileAccess.Read,
                                                          FileShare.Read);
                            try
                            {
                                uint dataLength;
                                currentPcmFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);

                                currentPcmDataLength = dataLength;
                            }
                            finally
                            {
                                fileStream.Close();
                            }
                        }

                        currentFileDataProvider =
                            (FileDataProvider)treeNode.Presentation.DataProviderFactory.Create(DataProviderFactory.AUDIO_WAV_MIME_TYPE);
                        currentFileDataProvider.InitByMovingExistingFile(deferredRecordingDataItem.RecordedFilePath);
                        if (File.Exists(deferredRecordingDataItem.RecordedFilePath))
                        //check exist just in case file adopted by DataProviderManager
                        {
                            File.Delete(deferredRecordingDataItem.RecordedFilePath);
                        }
                    }

                    //Time duration = new Time(currentPcmFormat.ConvertBytesToTime(currentPcmDataLength));

                    if (previousBytePosEnd < 0) previousBytePosEnd = 0;
                    long bytePosEnd = deferredRecordingDataItem.RecordAndContinue_StopBytePos;

                    openFile(treeNode, currentFileDataProvider, previousBytePosEnd, bytePosEnd, currentPcmFormat, currentPcmDataLength);
                }
                else
                {
                    openFile(deferredRecordingDataItem.RecordedFilePath, true, true,
                             State.Audio.PcmFormatRecordingMonitoring);
                }

                needsRefresh = true;

                //m_viewModel.CommandRefresh.Execute();
                //if (m_viewModel.View != null)
                //{
                //    m_viewModel.View.CancelWaveFormLoad(true);
                //}

                if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                {
                    previousRecordedFile = deferredRecordingDataItem.RecordedFilePath;
                    previousBytePosEnd = deferredRecordingDataItem.RecordAndContinue_StopBytePos;
                }
            }

            m_DeferredRecordingDataItems = null;

            Settings.Default.AudioWaveForm_DisableDraw = skipDrawing;

            if (needsRefresh)
            {
                CommandRefresh.Execute();
            }
        }


        private void OnAudioRecordingFinished_(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {

            if (!Settings.Default.Audio_DisableSingleWavFileRecord)
            {
                if (m_RecordAndContinue_StopSingleFileRecord)
                {
                    m_RecordAndContinue_StopSingleFileRecord = false;
                    return;
                }
            }

            if (m_InterruptRecording)
            {
                m_RecordAndContinue = false;
                m_InterruptRecording = false;
                checkAndAddDeferredRecordingDataItems();

                if (IsMonitoringAlways)
                {
                    CommandStartMonitor.Execute();
                }
                return;
            }

            if (!String.IsNullOrEmpty(e.RecordedFilePath))
            {
                registerRecordedAudioFileForDeferredAddition(e.RecordedFilePath);

                ////m_RecordAndContinue && 
                ////!Settings.Default.Audio_EnableSkippability
                //if (m_RecordAndContinue)
                //{

                //}
                //else
                //{
                //    openFile(e.RecordedFilePath, true, true, State.Audio.PcmFormatRecordingMonitoring);
                //}
            }

            if (m_RecordAndContinue)
            {
                if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                {
#if DEBUG
                    DebugFix.Assert(IsRecording);
#endif
                }

                IsAutoPlay = false;

                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode treeNode = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;
                if (treeNode != null)
                {
                //TreeNode next = electNextRecordableNode(treeNode);
                tryNext:
                    //TreeNode nested;
                    //TreeNode next = TreeNode.GetNextTreeNodeWithNoSignificantTextOnlySiblings(false, treeNode, out nested);
                    TreeNode next = TreeNode.NavigatePreviousNextSignificantText(false, treeNode);
                    if (next != null)
                    {
                        TreeNode math = next.GetFirstAncestorWithXmlElement("math");
                        if (math != null)
                        {
                            next = math;
                        }
                        else
                        {
                            TreeNode svg = next.GetFirstAncestorWithXmlElement("svg");
                            if (svg != null)
                            {
                                next = svg;
                            }
                            else
                            {
                                TreeNode candidate = m_UrakawaSession.AdjustTextSyncGranularity(next, treeNode);
                                if (candidate != null)
                                {
                                    next = candidate;
                                }
                            }
                        }


                        if (Settings.Default.Audio_EnableSkippability && m_UrakawaSession.isTreeNodeSkippable(next))
                        {
                            treeNode = next;
                            goto tryNext;
                        }

                        m_StateToRestore = null;

                        m_UrakawaSession.PerformTreeNodeSelection(next, false, null); //nested);
                        State.Selection.ClearSelection();

                        //must appear after tree node selection!!!
                        m_RecordAndContinue = false;

                        if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                        {
                            //RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                            //if (EventAggregator != null)
                            //{
                            //    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Recording);
                            //}
                            //PCMFormatInfo pcmFormatInfo = State.Audio.GetCurrentPcmFormat();
                            OnStateChanged_Recorder(null,
                                                    new AudioRecorder.StateChangedEventArgs(AudioRecorder.State.Stopped));
                        }
                        else
                        {
                            State.Audio.PcmFormatRecordingMonitoring = null;
                        }

                        Tuple<TreeNode, TreeNode> treeNodeSelectionNew = m_UrakawaSession.GetTreeNodeSelection();
                        TreeNode treeNodeNew = treeNodeSelectionNew.Item2 ?? treeNodeSelectionNew.Item1;
                        if (treeNodeNew != null)
                        {
                            //#if DEBUG
                            //                    DebugFix.Assert(treeNodeNew == next);
                            //#endif //DEBUG

                            if (treeNodeNew.GetManagedAudioMedia() == null
                                && treeNodeNew.GetFirstDescendantWithManagedAudio() == null)
                            {
                                if (IsWaveFormLoading && View != null)
                                {
                                    View.CancelWaveFormLoad(true);
                                }

                                if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                                {
                                    // NOOP
                                }
                                else
                                {
                                    CommandStartRecord.Execute();
                                }
                                return;
                            }
                            else
                            {
                                if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                                {
                                    m_RecordAndContinue_StopSingleFileRecord = true;
                                    CommandStopRecord.Execute();
                                }

                                if (m_DeferredRecordingDataItems != null)
                                {
                                    checkAndAddDeferredRecordingDataItems();

                                    if (IsWaveFormLoading && View != null)
                                    {
                                        View.CancelWaveFormLoad(true);
                                    }

                                    m_UrakawaSession.PerformTreeNodeSelection(treeNodeSelectionNew.Item1, false, treeNodeSelectionNew.Item2);
                                }
                                //CommandPlay.Execute();
                                
                                if (Settings.Default.Audio_EnableRecordOverwrite)
                                {
                                    CommandSelectAll.Execute();
                                    CommandStartRecord.Execute();
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            Debugger.Break();
#endif
                        }

                        if (IsMonitoringAlways && !IsMonitoring)
                        {
                            CommandStartMonitor.Execute();
                        }
                        return;
                    }
                }

                if (!Settings.Default.Audio_DisableSingleWavFileRecord)
                {
                    m_RecordAndContinue_StopSingleFileRecord = true;
                    CommandStopRecord.Execute();
                }
            }

            m_RecordAndContinue = false;
            checkAndAddDeferredRecordingDataItems();
            State.Audio.PcmFormatRecordingMonitoring = null;

            if (IsMonitoringAlways && !IsMonitoring)
            {
                CommandStartMonitor.Execute();
            }
        }


        //private TreeNode electNextRecordableNode(TreeNode current)
        //{
        //    TreeNode node = current;
        //tryNext:
        //    TreeNode next = node.GetNextSiblingWithText(true);
        //    if (next != null)
        //    {
        //    tryParent:
        //        if (next.Parent != null)
        //        {
        //            foreach (var child in next.Parent.Children.ContentsAs_YieldEnumerable)
        //            {
        //                string text = child.GetTextFlattened(true);
        //                if (!string.IsNullOrEmpty(text))
        //                {
        //                    text = text.Trim(); // we discard punctuation

        //                    if (TreeNode.TextOnlyContainsPunctuation(text))
        //                    {
        //                        if (child == next)
        //                        {
        //                            node = next;
        //                            goto tryNext;
        //                        }
        //                    }
        //                    else if (child.GetXmlElementQName() == null)
        //                    {
        //                        next = next.Parent;
        //                        goto tryParent;
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            QualifiedName qName = next.GetXmlElementQName();
        //            if (qName == null)
        //            {
        //                node = next;
        //                goto tryNext;
        //            }
        //        }

        //        return next;
        //    }

        //    return null;
        //}


        private void OnStateChanged_Recorder(object sender, AudioRecorder.StateChangedEventArgs e)
        {
            if (!TheDispatcher.CheckAccess())
            {

                TheDispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, AudioRecorder.StateChangedEventArgs>)OnStateChanged_Recorder_, sender,
                                  e);
                return;
            }

            OnStateChanged_Recorder_(sender, e);
        }
        private void OnStateChanged_Recorder_(object sender, AudioRecorder.StateChangedEventArgs e)
        {

            //Logger.Log("AudioPaneViewModel.OnStateChanged_Recorder", Category.Debug, Priority.Medium);

            CommandManager.InvalidateRequerySuggested();

            resetPeakMeter();

            RaisePropertyChanged(() => IsRecording);
            RaisePropertyChanged(() => IsMonitoring);

            m_UrakawaSession.isAudioMonitoring = IsMonitoring;
            m_UrakawaSession.isAudioRecording = IsRecording;

            if ((e.OldState == AudioRecorder.State.Recording || e.OldState == AudioRecorder.State.Monitoring)
                && m_Recorder.CurrentState == AudioRecorder.State.Stopped)
            {
                UpdatePeakMeter();
                //if (View != null)
                //{
                //    View.StopPeakMeterTimer();
                //}

                if (View != null)
                {
                    View.TimeMessageHide();
                }

                //if (e.OldState != AudioRecorder.State.Monitoring && IsMonitoringAlways)
                //{
                //    CommandStartMonitor.Execute();
                //}
            }
            if (IsRecording || IsMonitoring)
            {
                if (e.OldState == AudioRecorder.State.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                UpdatePeakMeter();
                //if (View != null)
                //{
                //    View.StartPeakMeterTimer();
                //}

                if (View != null && (!IsMonitoring || !IsMonitoringAlways))
                {
                    View.TimeMessageShow();
                }

                //AudioCues.PlayTock();
            }
        }

        public bool IsRecording
        {
            get
            {
                return m_Recorder.CurrentState == AudioRecorder.State.Recording;
            }
        }

        public bool IsMonitoring
        {
            get
            {
                return m_Recorder.CurrentState == AudioRecorder.State.Monitoring;
            }
        }

        public bool IsMonitoringAlways
        {
            get
            {
                return Settings.Default.Audio_AlwaysMonitor;
            }
            set
            {
                if (Settings.Default.Audio_AlwaysMonitor != value)
                {
                    Settings.Default.Audio_AlwaysMonitor = value;
                    RaisePropertyChanged(() => IsMonitoringAlways);

                    CommandManager.InvalidateRequerySuggested();
                }

                if (!Settings.Default.Audio_AlwaysMonitor)
                {
                    if (IsMonitoring)
                    {
                        CommandStopMonitor.Execute();
                    }
                }
                else if (!IsPlaying && !IsRecording && !IsMonitoring)
                {
                    CommandStartMonitor.Execute();
                }
            }
        }


        //private void OnRecorderResetVuMeter(object sender, UpdateVuMeterEventArgs e)
        //{
        //    resetPeakMeter();
        //}

        #endregion Audio Recorder
    }
}
