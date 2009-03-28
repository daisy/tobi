
using System.ComponentModel;
using System.IO;
using System.Windows;
using AudioEngine.PPMeter;
using AudioLib;
using Microsoft.Win32;
using urakawa.media.data.audio;

namespace WPF_AudioTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : INotifyPropertyChanged
    {
        private AudioPlayer m_Player;
        private AudioRecorder m_Recorder;
        private VuMeter m_VuMeter;
        private GraphicalPeakMeter m_GraphicalPeakMeter;
        //private GraphicalVuMeter m_GraphicalVuMeter;


        private string m_WavFilePath;

        public Window1()
        {
            InitializeComponent();
            InitializeAudioStuff();
            DataContext = this;
        }

        private void InitializeAudioStuff()
        {
            m_Player = new AudioPlayer();
            m_Player.StateChanged += Player_StateChanged;

            m_Recorder = new AudioRecorder();
            m_Recorder.StateChanged += Recorder_StateChanged;

            m_VuMeter = new AudioLib.VuMeter(m_Player, m_Recorder);
            m_GraphicalPeakMeter = new GraphicalPeakMeter
            {
                BarPaddingToWidthRatio = 0.075F,
                Dock = System.Windows.Forms.DockStyle.Fill,
                FontToHeightRatio = 0.03F,
                FontToWidthRatio = 0.075F,
                Location = new System.Drawing.Point(0, 0),
                MinimumSize = new System.Drawing.Size(200, 300),
                Name = "mGraphicalPeakMeter",
                Size = new System.Drawing.Size(400, 500),
                SourceVuMeter = m_VuMeter,
                TabIndex = 0
            };

            WinFormPeakMeter.Child = m_GraphicalPeakMeter;
            m_Player.SetDevice(WinFormPeakMeter.Child, @"auto");

            /*
            m_GraphicalVuMeter = new GraphicalVuMeter()
                                     {
                                         Dock = System.Windows.Forms.DockStyle.Fill,
                                         //Location = new System.Drawing.Point(0, 0),
                                         MinimumSize = new System.Drawing.Size(50, 50),
                                         Name = "mVuMeter",
                                         Size = new System.Drawing.Size(400, 500),
                                         TabIndex = 0,
                                         VuMeter = m_VuMeter
                                     };
            WinFormVuMeter.Child = m_GraphicalVuMeter;
             */
        }


        private void Recorder_StateChanged(object sender, AudioLib.Events.Recorder.StateChangedEventArgs e)
        {
            //m_Recorder.State == AudioLib.AudioRecorderState.Monitoring
        }

        private void Player_StateChanged(object sender, AudioLib.Events.Player.StateChangedEventArgs e)
        {
            //m_Recorder.State == AudioLib.AudioRecorderState.Monitoring
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "audio"; // Default file name
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "WAV files (.wav)|*.wav;*.aiff";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }

            FilePath = dlg.FileName;

            //WavFilePath = @"C:\\Documents and Settings\\Administrator\\My Documents\\audio1.wav";

            Stream stream = File.Open(FilePath, FileMode.Open);

            PCMDataInfo pcmInfo = PCMDataInfo.ParseRiffWaveHeader(stream);
            m_Player.Play(() => stream, pcmInfo.GetDuration(pcmInfo.DataLength), pcmInfo);
        }

        public string FilePath
        {
            get
            {
                return m_WavFilePath;
            }
            set
            {
                if (m_WavFilePath == value) return;
                m_WavFilePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}
