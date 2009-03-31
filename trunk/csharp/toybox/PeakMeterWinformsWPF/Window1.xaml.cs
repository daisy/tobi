
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AudioEngine.PPMeter;
using AudioLib;
using AudioLib.Events.Player;
using Microsoft.Win32;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.timing;

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
        //private GraphicalPeakMeter m_GraphicalPeakMeter;
        //private GraphicalVuMeter m_GraphicalVuMeter;


        private string m_WavFilePath;
        private FileStream m_FilePlayStream;
        private PCMDataInfo m_pcmFormat;
        private int m_bytesPerPixel;

        public Window1()
        {
            InitializeComponent();
            InitializeAudioStuff();
            DataContext = this;
        }

        private void InitializeAudioStuff()
        {

            mCurrentAudioStreamProvider = () =>
            {
                if (!String.IsNullOrEmpty(FilePath))
                {
                    if (m_FilePlayStream == null)
                    {
                        m_FilePlayStream = File.Open(FilePath, FileMode.Open);
                    }
                    return m_FilePlayStream;
                }
                return null;
            };

            m_Player = new AudioPlayer();
            m_Player.StateChanged += Player_StateChanged;

            m_Recorder = new AudioRecorder();
            m_Recorder.StateChanged += Recorder_StateChanged;

            m_VuMeter = new AudioLib.VuMeter(m_Player, m_Recorder);
            /*m_GraphicalPeakMeter = new GraphicalPeakMeter
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
             */
            WinFormPeakMeter.Child = new System.Windows.Forms.Control("test");
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

        private AudioPlayer.StreamProviderDelegate mCurrentAudioStreamProvider;

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            if (m_Player.State == AudioPlayerState.Playing)
            {
                m_Player.Pause();
            }
            else if (m_Player.State == AudioPlayerState.Paused || m_Player.State == AudioPlayerState.Stopped)
            {
                m_Player.Resume();
            }

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "audio"; // Default file name
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "WAV files (.wav)|*.wav;*.aiff";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            FilePath = dlg.FileName;

            m_pcmFormat = null;
            loadWaveForm();

            m_Player.Play(mCurrentAudioStreamProvider,
                        m_pcmFormat.GetDuration(m_pcmFormat.DataLength), m_pcmFormat);
            m_Player.EndOfAudioAsset += new EndOfAudioAssetHandler(OnEndOfAudioAsset);
            m_Player.UpdateVuMeter += new UpdateVuMeterHandler(OnUpdateVuMeter);
            m_Player.ResetVuMeter += new ResetVuMeterHandler(OnUpdateVuMeter);
            m_Player.StateChanged += new StateChangedHandler(OnAudioPlayerStateChanged);
        }

        private void OnAudioPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                m_FilePlayStream = null;
            }
        }

        private void OnUpdateVuMeter(object sender, UpdateVuMeterEventArgs e)
        {
            updateWaveFormPlayHeadPath();
        }

        private void OnEndOfAudioAsset(object sender, EndOfAudioAssetEventArgs e)
        {
            //
        }

        private void updateWaveFormPlayHeadPath()
        {
            if (m_pcmFormat == null)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                double time = m_Player.CurrentTimePosition;
                long byteOffset = m_pcmFormat.GetByteForTime(new Time(time));
                double pixels = ((double)byteOffset) / m_bytesPerPixel;

                StreamGeometry geometry = null;
                if (WaveFormPlayHeadPath.Data == null)
                {
                    geometry = new StreamGeometry();
                }
                else
                {
                    geometry = (StreamGeometry)WaveFormPlayHeadPath.Data;
                }

                using (StreamGeometryContext sgc = geometry.Open())
                {
                    sgc.BeginFigure(new Point(pixels, 5), false, false);
                    sgc.LineTo(new Point(pixels, WaveFormCanvas.Height - 5), true, false);

                    sgc.Close();
                }

                if (WaveFormPlayHeadPath.Data == null)
                {
                    WaveFormPlayHeadPath.Data = geometry;
                }
                else
                {
                    double left = WaveFormScroll.HorizontalOffset;
                    double right = left + WaveFormScroll.ActualWidth;
                    //bool b = WaveFormPlayHeadPath.IsVisible;
                    if (pixels < left || pixels > right)
                    {
                        //WaveFormPlayHeadPath.BringIntoView();
                        double offset = pixels - 10;
                        if (offset < 0)
                        {
                            offset = 0;
                        }
                        WaveFormScroll.ScrollToHorizontalOffset(offset);
                    }
                    else
                    {
                        WaveFormPlayHeadPath.InvalidateVisual();
                    }

                }
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(updateWaveFormPlayHeadPath));
            }
        }

        private void loadWaveForm()
        {
            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            bool wasPlaying = (m_Player.State == AudioPlayerState.Playing);

            if (m_Player.State != AudioPlayerState.NotReady)
            {
                if (wasPlaying)
                {
                    m_Player.Pause();
                }
            }

            if (mCurrentAudioStreamProvider() == null)
            {
                if (wasPlaying)
                {
                    m_Player.Resume();
                }
                return;
            }

            m_FilePlayStream.Position = 0;
            m_FilePlayStream.Seek(0, SeekOrigin.Begin);

            if (true || m_pcmFormat == null)
            {
                m_pcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_FilePlayStream);
            }

            if (m_pcmFormat.BitDepth != 16)
            {
                return;
            }
            ushort channels = m_pcmFormat.NumberOfChannels;
            ushort frameSize = m_pcmFormat.BlockAlign;

            int samplesPerPixel = (int)Math.Ceiling(m_pcmFormat.DataLength / (float)frameSize / WaveFormCanvas.Width * channels);
            m_bytesPerPixel = samplesPerPixel * frameSize / channels;

            byte[] bytes = new byte[m_bytesPerPixel];
            short[] samples = new short[samplesPerPixel];

            if (WaveFormPathCh1.Data != null)
            {
                WaveFormPathCh1.Data = null;
            }
            if (WaveFormPathCh2.Data != null)
            {
                WaveFormPathCh2.Data = null;
            }

            StreamGeometry geometryCh1 = new StreamGeometry();
            StreamGeometryContext sgcCh1 = geometryCh1.Open();

            StreamGeometry geometryCh2 = null;
            StreamGeometryContext sgcCh2 = null;

            if (channels > 1)
            {
                geometryCh2 = new StreamGeometry();
                sgcCh2 = geometryCh2.Open();
            }

            for (double x = 0; x < WaveFormCanvas.Width; ++x)
            {
                int read = m_FilePlayStream.Read(bytes, 0, m_bytesPerPixel);
                if (read <= 0)
                {
                    continue;
                }
                Buffer.BlockCopy(bytes, 0, samples, 0, read);

                short min = short.MaxValue;
                short max = short.MinValue;
                for (int channel = 0; channel < channels; channel++)
                {
                    int limit = (int)Math.Ceiling(read / (float)frameSize);

                    for (int i = channel; i < limit; i += channels)
                    {
                        if (samples[i] < min) min = samples[i];
                        if (samples[i] > max) max = samples[i];
                    }

                    double y1 = WaveFormCanvas.Height
                                - ((min - short.MinValue) * WaveFormCanvas.Height)
                                / ushort.MaxValue;

                    if (channel == 0)
                    {
                        sgcCh1.BeginFigure(new Point(x, y1), false, false);
                    }
                    else
                    {
                        sgcCh2.BeginFigure(new Point(x, y1), false, false);
                    }


                    double y2 = WaveFormCanvas.Height
                                - ((max - short.MinValue) * WaveFormCanvas.Height)
                                / ushort.MaxValue;
                    if (channel == 0)
                    {
                        sgcCh1.LineTo(new Point(x, y2), true, false);
                    }
                    else
                    {
                        sgcCh2.LineTo(new Point(x, y2), true, false);
                    }
                }
            }

            m_FilePlayStream.Close();
            m_FilePlayStream = null;

            sgcCh1.Close();
            geometryCh1.Freeze();
            WaveFormPathCh1.Data = geometryCh1;
            if (channels > 1)
            {
                sgcCh2.Close();
                geometryCh2.Freeze();
                WaveFormPathCh2.Data = geometryCh2;
            }

            updateWaveFormPlayHeadPath();

            if (wasPlaying)
            {
                m_Player.Resume();
            }
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

        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            loadWaveForm();
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (m_pcmFormat == null)
            {
                return;
            }

            if (m_Player.State == AudioPlayerState.Paused)
            {
                m_Player.Resume();
            }
            else if (m_Player.State == AudioPlayerState.Stopped)
            {
                m_Player.Play(mCurrentAudioStreamProvider,
                            m_pcmFormat.GetDuration(m_pcmFormat.DataLength), m_pcmFormat);
            }

            Point p = e.GetPosition(WaveFormCanvas);
            long byteOffset = (long)(p.X * m_bytesPerPixel);
            TimeDelta d = m_pcmFormat.GetDuration((uint)byteOffset);
            m_Player.CurrentTimePosition = d.TimeDeltaAsMillisecondFloat;
        }
    }
}
