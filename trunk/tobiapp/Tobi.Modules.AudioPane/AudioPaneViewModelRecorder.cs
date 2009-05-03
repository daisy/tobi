using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {

        #region Audio Recorder

        private AudioRecorder m_Recorder;

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnRecorderStateChanged(object sender, AudioLib.Events.Recorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            //m_Recorder.State == AudioLib.AudioRecorderState.Monitoring
            Logger.Log("AudioPaneViewModel.OnRecorderStateChanged", Category.Debug, Priority.Medium);

            if ((e.OldState == AudioRecorderState.Recording || e.OldState == AudioRecorderState.Monitoring)
                && m_Recorder.State == AudioRecorderState.Stopped)
            {
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StopPeakMeterTimer();
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
            }
        }

        public bool IsRecording
        {
            get;
            set;
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

            var shell = Container.Resolve<IShellPresenter>();

            if (shell.DocumentProject == null)
            {
                setRecordingDirectory(Directory.GetCurrentDirectory());
                m_PcmFormat = new PCMFormatInfo();
            }
            else
            {
                if (m_PcmFormat == null)
                {
                    m_PcmFormat = shell.DocumentProject.GetPresentation(0).MediaDataManager.DefaultPCMFormat;
                }
            }

            m_Recorder.StartRecording(m_PcmFormat);

            IsRecording = true;
            OnPropertyChanged("IsRecording");
        }

        public void AudioRecorder_Stop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Stop", Category.Debug, Priority.Medium);

            var shell = Container.Resolve<IShellPresenter>();

            if (shell.DocumentProject != null)
            {
                var mediaData =
                    (WavAudioMediaData) shell.DocumentProject.GetPresentation(0).MediaDataFactory.CreateAudioMediaData();

                ManagedAudioMedia managedMedia =
                    shell.DocumentProject.GetPresentation(0).MediaFactory.CreateManagedAudioMedia();
                managedMedia.MediaData = mediaData;
            }

            m_Recorder.StopRecording();

            IsRecording = false;
            OnPropertyChanged("IsRecording");

            if (!string.IsNullOrEmpty(m_Recorder.RecordedFilePath))
            {
                OpenFile(m_Recorder.RecordedFilePath);
            }
        }

        #endregion Audio Recorder
    }
}
