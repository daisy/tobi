using System.Collections.Generic;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Recorder

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
                if (value != null && m_Recorder.InputDevice != value)
                {
                    if (m_Recorder.State != AudioRecorderState.Stopped)
                    {
                        return;
                    }
                    m_Recorder.InputDevice = value;
                }
            }
        }

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnRecorderStateChanged(object sender, AudioLib.Events.Recorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            Logger.Log("AudioPaneViewModel.OnRecorderStateChanged", Category.Debug, Priority.Medium);

            OnPropertyChanged(() => IsRecording);
            OnPropertyChanged(() => IsMonitoring);

            if ((e.OldState == AudioRecorderState.Recording || e.OldState == AudioRecorderState.Monitoring)
                && m_Recorder.State == AudioRecorderState.Stopped)
            {
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StopPeakMeterTimer();
                }

                if (View != null)
                {
                    View.TimeMessageHide();
                }
            }
            if (m_Recorder.State == AudioRecorderState.Recording || m_Recorder.State == AudioRecorderState.Monitoring)
            {
                if (e.OldState == AudioRecorderState.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StartPeakMeterTimer();
                }

                if (View != null)
                {
                    View.TimeMessageShow();
                }
            }
        }

        public bool IsRecording
        {
            get
            {
                return m_Recorder.State == AudioRecorderState.Recording;
            }
        }

        public bool IsMonitoring
        {
            get
            {
                return m_Recorder.State == AudioRecorderState.Monitoring;
            }
        }

        public void AudioRecorder_StartStopMonitor()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StartStopMonitor", Category.Debug, Priority.Medium);
            if (IsMonitoring)
            {
                AudioRecorder_StopMonitor();
            }
            else
            {
                AudioRecorder_StartMonitor();
            }
        }

        public void AudioRecorder_StartMonitor()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StartMonitor", Category.Debug, Priority.Medium);

            var session = Container.Resolve<IUrakawaSession>();

            if (session.DocumentProject == null)
            {
                setRecordingDirectory(Directory.GetCurrentDirectory());
                PcmFormat = new PCMFormatInfo();
            }
            else
            {
                if (PcmFormat == null)
                {
                    PcmFormat = session.DocumentProject.GetPresentation(0).MediaDataManager.DefaultPCMFormat;
                }
            }

            m_Recorder.StartListening(new AudioLibPCMFormat ( PcmFormat.NumberOfChannels, PcmFormat.SampleRate ,PcmFormat.BitDepth));
        }

        public void AudioRecorder_StopMonitor()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StopMonitor", Category.Debug, Priority.Medium);

            m_Recorder.StopRecording();
        }

        public void AudioRecorder_StartStop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StartStop", Category.Debug, Priority.Medium);
            if (IsRecording)
            {
                AudioRecorder_Stop();
            }
            else
            {
                AudioRecorder_Start();
            }
        }

        public void AudioRecorder_Start()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Start", Category.Debug, Priority.Medium);

            
            var session = Container.Resolve<IUrakawaSession>();

            if (session.DocumentProject == null)
            {
                setRecordingDirectory(Directory.GetCurrentDirectory());
                PcmFormat = new PCMFormatInfo();
            }
            else
            {
                if (PcmFormat == null)
                {
                    PcmFormat = session.DocumentProject.GetPresentation(0).MediaDataManager.DefaultPCMFormat;
                }
            }

            m_Recorder.StartRecording(new AudioLibPCMFormat ( PcmFormat.NumberOfChannels , PcmFormat.SampleRate, PcmFormat.BitDepth));
        }

        public void AudioRecorder_Stop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Stop", Category.Debug, Priority.Medium);

            var session = Container.Resolve<IUrakawaSession>();

            if (session.DocumentProject != null)
            {
                var mediaData =
                    (WavAudioMediaData)session.DocumentProject.GetPresentation(0).MediaDataFactory.CreateAudioMediaData();

                ManagedAudioMedia managedMedia =
                    session.DocumentProject.GetPresentation(0).MediaFactory.CreateManagedAudioMedia();
                managedMedia.MediaData = mediaData;
            }

            m_Recorder.StopRecording();

            if (!string.IsNullOrEmpty(m_Recorder.RecordedFilePath))
            {
                OpenFile(m_Recorder.RecordedFilePath);
            }
        }

        #endregion Audio Recorder
    }
}
