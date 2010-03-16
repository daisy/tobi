using System;
using System.Collections.Generic;
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
using InputDevice = AudioLib.InputDevice;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Recorder

        public RichDelegateCommand CommandStartRecord { get; private set; }
        public RichDelegateCommand CommandStopRecord { get; private set; }
        public RichDelegateCommand CommandStartMonitor { get; private set; }
        public RichDelegateCommand CommandStopMonitor { get; private set; }

        private void initializeCommands_Recorder()
        {
            CommandStopRecord = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_StopRecord,
                Tobi_Plugin_AudioPane_Lang.Audio_StopRecord_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-stop"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopRecord", Category.Debug, Priority.Medium);

                    m_Recorder.StopRecording();

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.RecordingStopped); // TODO Localize RecordingStopped

                },
                () => !IsWaveFormLoading && IsRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopRecord));

            m_ShellView.RegisterRichCommand(CommandStopRecord);
            //
            CommandStartRecord = new RichDelegateCommand(
                UserInterfaceStrings.Audio_StartRecord,
                UserInterfaceStrings.Audio_StartRecord_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-record"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartRecord", Category.Debug, Priority.Medium);

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

                    m_Recorder.StartRecording(State.Audio.PcmFormatRecordingMonitoring.Copy().Data);

                    RaisePropertyChanged(() => State.Audio.PcmFormatRecordingMonitoring);

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Recording); // TODO Localize Recording
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
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
                Tobi_Plugin_AudioPane_Lang.Audio_StartMonitor,
                Tobi_Plugin_AudioPane_Lang.Audio_StartMonitor_,
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
                Tobi_Plugin_AudioPane_Lang.Audio_StopMonitor,
                Tobi_Plugin_AudioPane_Lang.Audio_StopMonitor_,
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
                if (value != null && m_Recorder.InputDevice != value)
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
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                  (Action<object, AudioRecorder.AudioRecordingFinishEventArgs>)OnAudioRecordingFinished_,
                                  sender, e);
                return;
            }

#if DEBUG
            Debugger.Break();
#endif
        }
        private void OnAudioRecordingFinished_(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.RecordedFilePath))
            {
                openFile(e.RecordedFilePath, true, true, State.Audio.PcmFormatRecordingMonitoring);
            }

            State.Audio.PcmFormatRecordingMonitoring = null;
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
