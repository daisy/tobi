using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.core;
using urakawa.media.data.audio;
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
    }

    public partial class AudioPaneViewModel
    {
        #region Audio Recorder

        public RichDelegateCommand CommandStartRecord { get; private set; }
        public RichDelegateCommand CommandStopRecord { get; private set; }
        public RichDelegateCommand CommandStopRecordAndContinue { get; private set; }

        public RichDelegateCommand CommandStartMonitor { get; private set; }
        public RichDelegateCommand CommandStopMonitor { get; private set; }

        private bool m_RecordAndContinue = false;

        private void initializeCommands_Recorder()
        {
            Stopwatch stopWatchRecorder = null;

            CommandStopRecordAndContinue = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecordAndContinue_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecordAndContinue_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("weather-clear-night"),//emblem-symbolic-link
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
                    m_Recorder.StopRecording();

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.RecordingStopped);
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

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.RecordingStopped); // TODO Localize RecordingStopped
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

                    if (stopWatchRecorder != null)
                    {
                        Console.WriteLine("stopWatchRecorder != null, skipping start record");
                        return;
                    }

                    IsAutoPlay = false;

                    SetRecordAfterPlayOverwriteSelection(-1);

                    bool punchIn = false;
                    if (IsPlaying) // Punch-in recording
                    {
                        punchIn = true;
                        CommandPause.Execute();
                        CommandSelectRight.Execute();
                    }

                    if (IsWaveFormLoading && View != null)
                    {
                        View.CancelWaveFormLoad(true);
                    }

                    m_RecordAndContinue = false;

                    if (m_UrakawaSession.DocumentProject == null)
                    {
                        State.ResetAll();

                        State.Audio.PcmFormatRecordingMonitoring = new PCMFormatInfo();
                    }
                    else
                    {
                        Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                        if (treeNodeSelection.Item1 == null)
                        {
                            return;
                        }

                        if (!punchIn) // let's check auto punch in/out based on audio selection
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
                        }

                        Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatRecordingMonitoring = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                    }

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));

                    stopWatchRecorder = Stopwatch.StartNew();
                    m_Recorder.StartRecording(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Recording); // TODO Localize Recording
                },
                () =>
                {
                    return !IsMonitoring && !IsRecording && !IsWaveFormLoading //!IsPlaying && 
                        && canDeleteInsertReplaceAudio();
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopRecord));

            m_ShellView.RegisterRichCommand(CommandStartRecord);

            //
            CommandStartMonitor = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStartMonitor_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStartMonitor_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("audio-input-microphone"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartMonitor", Category.Debug, Priority.Medium);

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
                        Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatRecordingMonitoring = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                    }

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));

                    //AudioCues.PlayTock();

                    m_Recorder.StartMonitoring(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Monitoring); // TODO Localize Monitoring

                },
                () => !IsWaveFormLoading && !IsPlaying && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStartMonitor);
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

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.MonitoringStopped);// TODO Localize MonitoringStopped

                    State.Audio.PcmFormatRecordingMonitoring = null;

                },
                () => !IsWaveFormLoading && IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStopMonitor);

            //
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

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_IsAutoRecordNext = false;
        // ReSharper restore RedundantDefaultFieldInitializer
        public bool IsAutoRecordNext
        {
            get
            {
                return m_IsAutoRecordNext;
            }
            set
            {
                if (m_IsAutoRecordNext == value) return;
                m_IsAutoRecordNext = value;

                RaisePropertyChanged(() => IsAutoRecordNext);
            }
        }

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

            bool skipDrawing = Settings.Default.AudioWaveForm_SkipDrawing;
            Settings.Default.AudioWaveForm_SkipDrawing = true;

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

                openFile(deferredRecordingDataItem.RecordedFilePath, true, true, State.Audio.PcmFormatRecordingMonitoring);

                needsRefresh = true;

                //m_viewModel.CommandRefresh.Execute();
                //if (m_viewModel.View != null)
                //{
                //    m_viewModel.View.CancelWaveFormLoad(true);
                //}
            }

            m_DeferredRecordingDataItems = null;

            Settings.Default.AudioWaveForm_SkipDrawing = skipDrawing;

            if (needsRefresh)
            {
                CommandRefresh.Execute();
            }
        }

        private bool isTreeNodeSkippable(TreeNode node)
        {
            QualifiedName qname = node.GetXmlElementQName();
            if (qname != null && qname.LocalName.ToLower() == "pagenum")
            {
                return true;
            }
            if (node.Parent == null)
            {
                return false;
            }
            return isTreeNodeSkippable(node.Parent);
        }

        private void OnAudioRecordingFinished_(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {
            if (m_InterruptRecording)
            {
                m_RecordAndContinue = false;
                m_InterruptRecording = false;
                checkAndAddDeferredRecordingDataItems();
                return;
            }

            if (!String.IsNullOrEmpty(e.RecordedFilePath))
            {
                //m_RecordAndContinue && 
                if (Settings.Default.Audio_EnableDeferredRecord)
                {
                    registerRecordedAudioFileForDeferredAddition(e.RecordedFilePath);
                }
                else
                {
                    openFile(e.RecordedFilePath, true, true, State.Audio.PcmFormatRecordingMonitoring);
                }
            }

            if (m_RecordAndContinue)
            {
                IsAutoPlay = false;

                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode treeNode = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;
                if (treeNode != null)
                {
                //TreeNode next = electNextRecordableNode(treeNode);
                tryNext:
                    TreeNode next = treeNode.GetNextSiblingWithText(true);
                    while (next != null && (next.GetXmlElementQName() == null
                            || TreeNode.TextOnlyContainsPunctuation(next.GetText(true).Trim())
                            ))
                    {
                        next = next.GetNextSiblingWithText(true);
                    }
                    next = TreeNode.EnsureTreeNodeHasNoSignificantTextOnlySiblings(m_UrakawaSession.DocumentProject.Presentations.Get(0).RootNode, next);

                    if (next != null)
                    {
                        if (Settings.Default.Audio_EnableSkipDuringRecord && isTreeNodeSkippable(next))
                        {
                            treeNode = next;
                            goto tryNext;
                        }

                        m_StateToRestore = null;

                        m_UrakawaSession.PerformTreeNodeSelection(next);
                        State.Selection.ClearSelection();

                        m_RecordAndContinue = false;
                        State.Audio.PcmFormatRecordingMonitoring = null;

                        Tuple<TreeNode, TreeNode> treeNodeSelectionNew = m_UrakawaSession.GetTreeNodeSelection();
                        TreeNode treeNodeNew = treeNodeSelectionNew.Item2 ?? treeNodeSelectionNew.Item1;
                        if (treeNodeNew != null)
                        {
                            if (treeNodeNew.GetManagedAudioMedia() == null
                                && treeNodeNew.GetFirstDescendantWithManagedAudio() == null)
                            {
                                if (IsWaveFormLoading && View != null)
                                {
                                    View.CancelWaveFormLoad(true);
                                }

                                CommandStartRecord.Execute();
                            }
                            else
                            {
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
                                //CommandSelectAll.Execute();
                            }
                        }

                        return;
                    }
                }
            }

            m_RecordAndContinue = false;
            checkAndAddDeferredRecordingDataItems();
            State.Audio.PcmFormatRecordingMonitoring = null;
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
            }
            if (m_Recorder.CurrentState == AudioRecorder.State.Recording || m_Recorder.CurrentState == AudioRecorder.State.Monitoring)
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

                if (View != null)
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

        //private void OnRecorderResetVuMeter(object sender, UpdateVuMeterEventArgs e)
        //{
        //    resetPeakMeter();
        //}

        #endregion Audio Recorder
    }
}
