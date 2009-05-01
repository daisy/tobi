using AudioLib;
using Microsoft.Practices.Composite.Logging;

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
            IsRecording = true;
            OnPropertyChanged("IsRecording");
        }

        public void AudioRecorder_Stop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Stop", Category.Debug, Priority.Medium);
            IsRecording = false;
            OnPropertyChanged("IsRecording");
        }

        #endregion Audio Recorder
    }
}
