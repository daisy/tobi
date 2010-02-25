using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.media.data.audio;

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
                UserInterfaceStrings.Audio_StopRecord,
                UserInterfaceStrings.Audio_StopRecord_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-stop"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopRecord", Category.Debug, Priority.Medium);

                    m_Recorder.StopRecording();

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("Recording stopped."); // TODO Localize RecordingStopped
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
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartRecord", Category.Debug, Priority.Medium);

                    if (m_UrakawaSession.DocumentProject == null)
                    {
                        State.ResetAll();

                        State.Audio.PcmFormatAlt = new PCMFormatInfo();
                    }
                    else
                    {
                        if (State.CurrentTreeNode == null)
                        {
                            return;
                        }

                        Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatAlt = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Copy();
                    }

                    m_Recorder.StartRecording(new AudioLibPCMFormat(State.Audio.PcmFormatAlt.Data.NumberOfChannels, State.Audio.PcmFormatAlt.Data.SampleRate, State.Audio.PcmFormatAlt.Data.BitDepth));

                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("Recording..."); // TODO Localize Recording
                },
                ()=>
                {
                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                        && (
                        (m_UrakawaSession.DocumentProject != null && State.CurrentTreeNode != null)
                        ||
                        (m_UrakawaSession.DocumentProject == null)
                        );
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopRecord));

            m_ShellView.RegisterRichCommand(CommandStartRecord);

            //
            CommandStartMonitor = new RichDelegateCommand(
                UserInterfaceStrings.Audio_StartMonitor,
                UserInterfaceStrings.Audio_StartMonitor_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_audio-x-generic"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartMonitor", Category.Debug, Priority.Medium);

                    if (m_UrakawaSession.DocumentProject == null)
                    {
                        State.ResetAll();

                        State.Audio.PcmFormatAlt = new PCMFormatInfo();

                        //m_PcmFormatOfAudioToInsert = IsAudioLoaded ? State.Audio.PcmFormat : new PCMFormatInfo();
                        //m_Recorder.InputDevice.Capture.Caps.Format44KhzMono16Bit
                    }
                    else
                    {
                        Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatAlt = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Copy();
                    }

                    m_Recorder.StartMonitoring(new AudioLibPCMFormat(State.Audio.PcmFormatAlt.Data.NumberOfChannels, State.Audio.PcmFormatAlt.Data.SampleRate, State.Audio.PcmFormatAlt.Data.BitDepth));


                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("Monitoring..."); // TODO Localize Monitoring
                    
                    AudioCues.PlayTock();
                },
                () => !IsWaveFormLoading && !IsPlaying && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStartMonitor);
            //
            CommandStopMonitor = new RichDelegateCommand(
                UserInterfaceStrings.Audio_StopMonitor,
                UserInterfaceStrings.Audio_StopMonitor_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-playback-stop"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopMonitor", Category.Debug, Priority.Medium);

                    m_Recorder.StopRecording();


                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("Monitoring stopped.");// TODO Localize MonitoringStopped

                    State.Audio.PcmFormatAlt = null;
                    
                    AudioCues.PlayTockTock();
                },
                () => !IsWaveFormLoading && IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StartStopMonitor));

            m_ShellView.RegisterRichCommand(CommandStopMonitor);

            //
        }

        private AudioRecorder m_Recorder;

        public List<InputDevice> InputDevices
        {
            get
            {
                return m_Recorder.InputDevices;
            }
        }
        public InputDevice InputDevice
        {
            get
            {
                return m_Recorder.InputDevice;
            }
            set
            {
                if (m_Recorder.CurrentState == AudioRecorder.State.Stopped && value != null && m_Recorder.InputDevice != value)
                {
                    m_Recorder.InputDevice = value;
                }
            }
        }

        private void OnAudioRecordingFinished(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<object, AudioRecorder.AudioRecordingFinishEventArgs>)OnAudioRecordingFinished, sender, e);
                return;
            }

            if (!String.IsNullOrEmpty(e.RecordedFilePath))
            {
                openFile(e.RecordedFilePath, true, true);
            }
        }

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnStateChanged_Recorder(object sender, AudioRecorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            //Logger.Log("AudioPaneViewModel.OnStateChanged_Recorder", Category.Debug, Priority.Medium);
            
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
