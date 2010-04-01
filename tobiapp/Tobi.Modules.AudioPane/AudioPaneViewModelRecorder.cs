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
            CommandStopRecordAndContinue = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecordAndContinue_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecordAndContinue_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("weather-clear-night"),//emblem-symbolic-link
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopRecordAndContinue", Category.Debug, Priority.Medium);

                    IsAutoPlay = false;
                    m_RecordAndContinue = true;
                    m_InterruptRecording = false;
                    m_Recorder.StopRecording();

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.RecordingStopped);

                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

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
                UserInterfaceStrings.CmdAudioStartRecord_ShortDesc,
                UserInterfaceStrings.CmdAudioStartRecord_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-record"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartRecord", Category.Debug, Priority.Medium);

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

                        Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatRecordingMonitoring = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Copy();
                    }

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));
                    m_Recorder.StartRecording(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Recording); // TODO Localize Recording
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return !IsPlaying && !IsMonitoring && !IsRecording //!IsWaveFormLoading
                        && (m_UrakawaSession.DocumentProject == null
                        ||
                           State.Audio.PlayStreamMarkers != null
                           ||
                           treeNodeSelection.Item1 != null
                           && treeNodeSelection.Item1.GetXmlElementQName() != null
                           && treeNodeSelection.Item1.GetFirstAncestorWithManagedAudio() == null
                           );
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
                        State.Audio.PcmFormatRecordingMonitoring = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Copy();
                    }

                    OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(GetMemberName(() => Settings.Default.Audio_InputDevice)));
                    m_Recorder.StartMonitoring(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Monitoring); // TODO Localize Monitoring

                    AudioCues.PlayTock();
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


                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.MonitoringStopped);// TODO Localize MonitoringStopped

                    State.Audio.PcmFormatRecordingMonitoring = null;

                    AudioCues.PlayTockTock();
                },
                () => !IsWaveFormLoading && IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStopMonitor);

            //
        }

        private AudioRecorder m_Recorder;

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
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, AudioRecorder.AudioRecordingFinishEventArgs>)OnAudioRecordingFinished_,
                                  sender, e);
                return;
            }

            OnAudioRecordingFinished_(sender, e);
        }
        private void OnAudioRecordingFinished_(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {
            if (m_InterruptRecording)
            {
                m_RecordAndContinue = false;
                m_InterruptRecording = false;
                return;
            }

            if (!String.IsNullOrEmpty(e.RecordedFilePath))
            {
                openFile(e.RecordedFilePath, true, true, State.Audio.PcmFormatRecordingMonitoring);
            }

            if (m_RecordAndContinue)
            {
                IsAutoPlay = false;

                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode treeNode = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;
                if (treeNode != null)
                {
                    TreeNode node = treeNode;
                tryNext:
                    TreeNode next = node.GetNextSiblingWithText(true);
                    if (next != null)
                    {
                    tryParent:
                        if (next.Parent != null)
                        {
                            foreach (var child in next.Parent.Children.ContentsAs_YieldEnumerable)
                            {
                                string text = child.GetTextMediaFlattened(true);
                                if (!string.IsNullOrEmpty(text))
                                {
                                    text = text.Trim(); // we discard punctuation

                                    if (textOnlyContainsPunctuation(text))
                                    {
                                        if (child == next)
                                        {
                                            node = next;
                                            goto tryNext;
                                        }
                                    }
                                    else if (child.GetXmlElementQName() == null)
                                    {
                                        next = next.Parent;
                                        goto tryParent;
                                    }
                                }
                            }
                        }
                        else
                        {
                            QualifiedName qName = next.GetXmlElementQName();
                            if (qName == null)
                            {
                                node = next;
                                goto tryNext;
                            }
                        }

                        m_UrakawaSession.PerformTreeNodeSelection(next);


                        m_RecordAndContinue = false;
                        State.Audio.PcmFormatRecordingMonitoring = null;

                        Tuple<TreeNode, TreeNode> treeNodeSelectionNew = m_UrakawaSession.GetTreeNodeSelection();
                        TreeNode treeNodeNew = treeNodeSelectionNew.Item2 ?? treeNodeSelectionNew.Item1;
                        if (treeNodeNew != null)
                        {
                            if (treeNodeNew.GetManagedAudioMedia() == null)
                            {
                                CommandStartRecord.Execute();
                            }
                            else
                            {
                                CommandPlay.Execute();
                            }
                        }

                        return;
                    }
                }
            }

            m_RecordAndContinue = false;
            State.Audio.PcmFormatRecordingMonitoring = null;
        }
        private bool textIsPunctuation(char text)
        {
            return text == '.' || text == ',' || text == '?' || text == '!' || text == '"' || text == '\'' ||
                   text == '(' || text == ')' || text == '{' || text == '}' || text == '[' || text == ']';
        }

        private bool textOnlyContainsPunctuation(string text)
        {
            CharEnumerator enumtor = text.GetEnumerator();
            int n = 0;
            while (textIsPunctuation(enumtor.Current))
            {
                n++;
                enumtor.MoveNext();
            }
            return n == text.Length;

            //return text == "." || text == "," || text == "?" || text == "!" || text == "'" || text == "\"" ||
            //       text == "(" || text == ")" || text == "{" || text == "}" || text == "[" || text == "]";
        }

        private void OnStateChanged_Recorder(object sender, AudioRecorder.StateChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {

                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
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

                AudioCues.PlayTock();
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
