using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AudioLib;
using AudioLib.Events.Player;
using AudioLib.Events.VuMeter;
using Microsoft.Win32;
using urakawa.media.data.audio;
using urakawa.media.timing;

namespace WpfDtbookTest
{
    public class PeakMeterBarData
    {
        public delegate void PeakMeterRefreshDelegate();

        private double m_ValueDb;
        private double m_MinimumDb = -72;
        private PeakMeterRefreshDelegate m_PeakMeterRefreshDelegate;
        private int m_PeakOverloadCount;

        public PeakMeterBarData(PeakMeterRefreshDelegate del)
        {
            m_PeakOverloadCount = 0;
            m_PeakMeterRefreshDelegate = del;
            ValueDb = MinimumDb;
            //ShownValueDb = MinimumDb;
            //m_FallbackSecondsPerDb = TimeSpan.Parse("00:00:00.5000000");
            //m_FallbackThread = new Thread(new ThreadStart(FallbackWorker));
        }

        public double ValueDb
        {
            get
            {
                return m_ValueDb;
            }
            set
            {
                double newValue;
                if (value > 0)
                {
                    newValue = 0;
                }
                else if (value < MinimumDb)
                {
                    newValue = MinimumDb;
                }
                else
                {
                    newValue = value;
                }
                if (newValue != m_ValueDb)
                {
                    m_ValueDb = newValue;

                    /*
                    if (!m_FallbackThread.IsAlive)
                    {
                        m_FallbackThread = new Thread(new ThreadStart(FallbackWorker));
                        m_FallbackThread.Start();
                    }*/
                }
            }
        }


        public double MinimumDb
        {
            get
            {
                return m_MinimumDb;
            }
            set
            {
                double newValue = value;
                if (newValue > -1)
                {
                    newValue = -1;
                }
                if (m_MinimumDb != newValue)
                {
                    m_MinimumDb = newValue;
                    ValueDb = ValueDb;
                    m_PeakMeterRefreshDelegate();
                    //ShownValueDb = ValueDb;
                }
            }
        }

        public int PeakOverloadCount
        {
            get
            {
                return m_PeakOverloadCount;
            }
            set
            {
                m_PeakOverloadCount = value;
            }
        }

        /*
         * 
        private TimeSpan m_FallbackSecondsPerDb;

        private Thread m_FallbackThread;
        private Mutex m_ValueDbMutex = new Mutex();

        private double m_ShownValueDb;
         
        public void ForceFullFallback()
        {
            if (m_FallbackThread.IsAlive)
            {
                m_FallbackThread.Abort();
            }
            ShownValueDb = ValueDb;
        }

    
        private double ShownValueDb
        {
            get
            {
                return m_ShownValueDb;
            }

            set
            {
                double newValue;
                if (value > 0)
                {
                    newValue = 0;
                }
                else if (value < MinimumDb)
                {
                    newValue = MinimumDb;
                }
                else
                {
                    newValue = value;
                }
                if (newValue != m_ShownValueDb)
                {
                    m_ShownValueDb = newValue;
                    m_PeakMeterRefreshDelegate();
                }
            }
        }

        private void FallbackWorker()
        {
            try
            {
                DateTime latestUpdateTime = DateTime.Now;
                while (true)
                {
                    TimeSpan timeSinceLatestUpdate = DateTime.Now.Subtract(latestUpdateTime);
                    double maxDiff = Double.PositiveInfinity;
                    if (m_FallbackSecondsPerDb.TotalMilliseconds > 0)
                    {
                        maxDiff = timeSinceLatestUpdate.TotalMilliseconds / m_FallbackSecondsPerDb.TotalMilliseconds;
                    }
                    latestUpdateTime += timeSinceLatestUpdate;
                    if (ValueDb < ShownValueDb - maxDiff)
                    {
                        ShownValueDb -= maxDiff;
                    }
                    else
                    {
                        ShownValueDb = ValueDb;
                    }
                    
                    if (ShownValueDb == ValueDb)
                    {
                        return;
                    }
                    m_ValueDbMutex.WaitOne();
                    try
                    {
                        if (ShownValueDb == ValueDb)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        m_ValueDbMutex.ReleaseMutex();
                    }
                     
                    Thread.Sleep(10);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }
         ~PeakMeterBarData()
        {
            if (m_FallbackThread.IsAlive) m_FallbackThread.Abort();
        }
         */

        public double DbToPixels(double totalPixels)
        {
            double h;
            if (ValueDb < MinimumDb)
            {
                h = 0;
            }
            else if (ValueDb > 0)
            {
                h = totalPixels;
            }
            else
            {
                h = (MinimumDb - ValueDb) * totalPixels / MinimumDb;
            }
            return h;
        }
    }
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
        private double m_bytesPerPixel;
        private AudioPlayer.StreamProviderDelegate mCurrentAudioStreamProvider;
        private DispatcherTimer m_PlaybackTimer;
        private DispatcherTimer m_PeakMeterTimer;
        private DispatcherTimer m_WaveFormLoadTimer;
        private long m_StreamRiffHeaderEndPos;

        private PeakMeterBarData m_PeakMeterBarDataCh1;
        private PeakMeterBarData m_PeakMeterBarDataCh2;

        private double[] m_PeakMeterValues;


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

            m_VuMeter = new VuMeter(m_Player, m_Recorder);

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

            WinFormHost.Child = new System.Windows.Forms.Control("needed by DirectSound");
            m_Player.SetDevice(WinFormHost.Child, @"auto");



            m_PeakMeterBarDataCh1 = new PeakMeterBarData(() => peakMeterCanvasInvalidateVisual());
            m_PeakMeterBarDataCh2 = new PeakMeterBarData(() => peakMeterCanvasInvalidateVisual());

            m_PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            m_PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;

            m_PeakMeterValues = new double[2];

            m_Player.EndOfAudioAsset += new EndOfAudioAssetHandler(OnEndOfAudioAsset);
            m_Player.StateChanged += new StateChangedHandler(OnAudioPlayerStateChanged);

            m_VuMeter.UpdatePeakMeter += new UpdatePeakMeterHandler(OnUpdateVuMeter);
            m_VuMeter.ResetEvent += new ResetHandler(OnResetVuMeter);
            m_VuMeter.PeakOverload += new PeakOverloadHandler(OnPeakOverload);

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

        private void OnOpenAudioFile(object sender, RoutedEventArgs e)
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

            if (PeakMeterPathCh1.Data != null)
            {
                ((StreamGeometry)PeakMeterPathCh1.Data).Clear();
            }
            if (PeakMeterPathCh2.Data != null)
            {
                ((StreamGeometry)PeakMeterPathCh2.Data).Clear();
            }

            PeakOverloadCountCh1 = 0;
            PeakOverloadCountCh2 = 0;

            loadWaveForm();

            m_Player.Play(mCurrentAudioStreamProvider,
                        m_pcmFormat.GetDuration(m_pcmFormat.DataLength), m_pcmFormat);
        }

        private void OnAudioPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                m_FilePlayStream = null;
                stopWaveFormTimer();
                stopPeakMeterTimer();
            }
            if (m_Player.State == AudioPlayerState.Playing)
            {
                if (e.OldState == AudioPlayerState.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                startWaveFormTimer();
                startPeakMeterTimer();
            }
        }

        private void stopWaveFormTimer()
        {
            if (m_PlaybackTimer != null && m_PlaybackTimer.IsEnabled)
            {
                m_PlaybackTimer.Stop();
            }
        }
        private void stopPeakMeterTimer()
        {
            if (m_PeakMeterTimer != null && m_PeakMeterTimer.IsEnabled)
            {
                m_PeakMeterTimer.Stop();
            }
        }

        private void startPeakMeterTimer()
        {
            if (m_PeakMeterTimer == null)
            {
                m_PeakMeterTimer = new DispatcherTimer();
                m_PeakMeterTimer.Tick += OnPeakMeterTimerTick;
                m_PlaybackTimer.Interval = TimeSpan.FromMilliseconds(60);
            }
            else if (m_PeakMeterTimer.IsEnabled)
            {
                return;
            }

            m_PeakMeterTimer.Start();
        }

        private void startWaveFormTimer()
        {
            if (m_PlaybackTimer == null)
            {
                m_PlaybackTimer = new DispatcherTimer();
                m_PlaybackTimer.Tick += OnPlaybackTimerTick;
            }
            else if (m_PlaybackTimer.IsEnabled)
            {
                return;
            }

            double interval = convertByteToMilliseconds(m_bytesPerPixel);

            if (interval < 50.0)
            {
                interval = 50;
            }
            m_PlaybackTimer.Interval = TimeSpan.FromMilliseconds(interval);

            m_PlaybackTimer.Start();
        }

        //TimeDelta d = m_pcmFormat.GetDuration((uint)m_bytesPerPixel);
        //double interval = d.TimeDeltaAsMillisecondFloat;
        private double convertByteToMilliseconds(double bytes)
        {
            if (m_pcmFormat == null)
            {
                return 0;
            }
            return 1000 * bytes / (m_pcmFormat.SampleRate * m_pcmFormat.NumberOfChannels * m_pcmFormat.BitDepth / 8);
        }
        public double convertMillisecondsToByte(double ms)
        {
            if (m_pcmFormat == null)
            {
                return 0;
            }
            return (ms * m_pcmFormat.SampleRate * m_pcmFormat.NumberOfChannels * m_pcmFormat.BitDepth / 8) / 1000;
        }

        private void OnPeakMeterTimerTick(object sender, EventArgs e)
        {
            updatePeakMeter();
        }

        private void OnPlaybackTimerTick(object sender, EventArgs e)
        {
            updateWaveFormPlayHead();
        }

        private void OnWaveFormLoadTimerTick(object sender, EventArgs e)
        {
            m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            m_WaveFormLoadTimer.Stop();
            loadWaveForm();
        }

        private void OnPeakOverload(object sender, PeakOverloadEventArgs e)
        {
            if (e != null)
            {
                if (e.Channel == 1)
                {
                    PeakOverloadCountCh1++;
                }
                else if (e.Channel == 2)
                {
                    PeakOverloadCountCh2++;
                }
            }
        }

        private void resetPeakMeterValues()
        {
            m_PeakMeterBarDataCh1.ValueDb = Double.NegativeInfinity;
            //m_PeakMeterBarDataCh1.ForceFullFallback();

            m_PeakMeterBarDataCh2.ValueDb = Double.NegativeInfinity;
            //m_PeakMeterBarDataCh2.ForceFullFallback();

            m_PeakMeterValues[0] = m_PeakMeterBarDataCh1.ValueDb;
            m_PeakMeterValues[1] = m_PeakMeterBarDataCh2.ValueDb;

            updatePeakMeter();
            peakMeterCanvasInvalidateVisual();
        }

        private void OnResetVuMeter(object sender, ResetEventArgs e)
        {
            resetPeakMeterValues();

            updateWaveFormPlayHead();
        }

        private void OnUpdateVuMeter(object sender, UpdatePeakMeter e)
        {
            if (e.PeakValues != null && e.PeakValues.Length > 0)
            {
                m_PeakMeterValues[0] = e.PeakValues[0];
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    m_PeakMeterValues[1] = e.PeakValues[1];
                }
            }
        }

        private void OnEndOfAudioAsset(object sender, EndOfAudioAssetEventArgs e)
        {
            if (m_pcmFormat == null)
            {
                return;
            }
            double time = m_pcmFormat.Duration.TimeDeltaAsMillisecondDouble;
            updateWaveFormPlayHead(time);
        }
        private void peakMeterCanvasInvalidateVisual()
        {
            if (Dispatcher.CheckAccess())
            {
                PeakMeterCanvas.InvalidateVisual();
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(peakMeterCanvasInvalidateVisual));
            }
        }

        private void updatePeakMeter()
        {
            if (m_pcmFormat == null)
            {
                return;
            }
            if (Dispatcher.CheckAccess())
            {
                if (m_Player.State != AudioPlayerState.Playing)
                {
                    return;
                }

                double barWidth = PeakMeterCanvas.ActualWidth;
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    barWidth = barWidth / 2;
                }
                double availableHeight = PeakMeterCanvas.ActualHeight;

                StreamGeometry geometry1 = null;
                StreamGeometry geometry2 = null;

                if (PeakMeterPathCh1.Data == null)
                {
                    geometry1 = new StreamGeometry();
                }
                else
                {
                    geometry1 = (StreamGeometry)PeakMeterPathCh1.Data;
                    geometry1.Clear();
                }
                using (StreamGeometryContext sgc = geometry1.Open())
                {
                    m_PeakMeterBarDataCh1.ValueDb = m_PeakMeterValues[0];

                    double pixels = m_PeakMeterBarDataCh1.DbToPixels(availableHeight);

                    sgc.BeginFigure(new Point(0, 0), true, true);
                    sgc.LineTo(new Point(barWidth, 0), true, false);
                    sgc.LineTo(new Point(barWidth, availableHeight - pixels), true, false);
                    sgc.LineTo(new Point(0, availableHeight - pixels), true, false);

                    sgc.Close();
                }

                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    if (PeakMeterPathCh2.Data == null)
                    {
                        geometry2 = new StreamGeometry();
                    }
                    else
                    {
                        geometry2 = (StreamGeometry)PeakMeterPathCh2.Data;
                        geometry2.Clear();
                    }
                    using (StreamGeometryContext sgc = geometry2.Open())
                    {
                        m_PeakMeterBarDataCh2.ValueDb = m_PeakMeterValues[1];

                        double pixels = m_PeakMeterBarDataCh2.DbToPixels(availableHeight);

                        sgc.BeginFigure(new Point(barWidth, 0), true, true);
                        sgc.LineTo(new Point(barWidth + barWidth, 0), true, false);
                        sgc.LineTo(new Point(barWidth + barWidth, availableHeight - pixels), true, false);
                        sgc.LineTo(new Point(barWidth, availableHeight - pixels), true, false);
                        sgc.LineTo(new Point(barWidth, availableHeight), true, false);

                        sgc.Close();
                    }
                }


                if (PeakMeterPathCh1.Data == null)
                {
                    PeakMeterPathCh1.Data = geometry1;
                }
                else
                {
                    PeakMeterPathCh1.InvalidateVisual();
                }
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    if (PeakMeterPathCh2.Data == null)
                    {
                        PeakMeterPathCh2.Data = geometry2;
                    }
                    else
                    {
                        PeakMeterPathCh2.InvalidateVisual();
                    }
                }
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(updatePeakMeter));
            }
        }

        private void updateWaveFormPlayHead(double time)
        {
            long byteOffset = m_pcmFormat.GetByteForTime(new Time(time));
            double pixels = byteOffset / m_bytesPerPixel;

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
                sgc.BeginFigure(new Point(pixels, WaveFormCanvas.ActualHeight - 5), true, false);
                sgc.LineTo(new Point(pixels, 5), true, false);
                sgc.LineTo(new Point(pixels + 5, 5 + 5), true, false);
                sgc.LineTo(new Point(pixels, 5 + 5 + 5), true, false);

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

        private void updateWaveFormPlayHead()
        {
            if (m_pcmFormat == null)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                if (m_Player.State != AudioPlayerState.Playing)
                {
                    return;
                }
                double time = m_Player.CurrentTimePosition;
                updateWaveFormPlayHead(time);
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(updateWaveFormPlayHead));
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
                return;
            }


            if (m_pcmFormat == null)
            {
                m_FilePlayStream.Position = 0;
                m_FilePlayStream.Seek(0, SeekOrigin.Begin);
                m_pcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_FilePlayStream);
                m_StreamRiffHeaderEndPos = m_FilePlayStream.Position;
            }
            else
            {
                m_FilePlayStream.Position = m_StreamRiffHeaderEndPos;
                m_FilePlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
            }

            if (m_pcmFormat.BitDepth != 16)
            {
                return;
            }

            if (m_pcmFormat.NumberOfChannels == 1)
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Collapsed;
            }
            else
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Visible;
            }


            ushort frameSize = (ushort)(m_pcmFormat.NumberOfChannels * m_pcmFormat.BitDepth / 8);
            double samplesPerPixel = Math.Ceiling(m_pcmFormat.DataLength
                                / (double)frameSize
                                / WaveFormCanvas.ActualWidth * m_pcmFormat.NumberOfChannels);
            m_bytesPerPixel = samplesPerPixel * frameSize / m_pcmFormat.NumberOfChannels;

            byte[] bytes = new byte[(int)m_bytesPerPixel];
            short[] samples = new short[(int)samplesPerPixel];

            StreamGeometry geometryCh1 = new StreamGeometry();
            StreamGeometryContext sgcCh1 = geometryCh1.Open();

            StreamGeometry geometryCh2 = null;
            StreamGeometryContext sgcCh2 = null;

            if (m_pcmFormat.NumberOfChannels > 1)
            {
                geometryCh2 = new StreamGeometry();
                sgcCh2 = geometryCh2.Open();
            }

            double height = WaveFormImage.Height;
            if (m_pcmFormat.NumberOfChannels > 1)
            {
                height /= 2;
            }

            for (double x = 0; x < WaveFormImage.Width; ++x)
            {
                int read = m_FilePlayStream.Read(bytes, 0, (int)m_bytesPerPixel);
                if (read <= 0)
                {
                    continue;
                }
                Buffer.BlockCopy(bytes, 0, samples, 0, read);

                short min = short.MaxValue;
                short max = short.MinValue;
                for (int channel = 0; channel < m_pcmFormat.NumberOfChannels; channel++)
                {
                    int limit = (int)Math.Ceiling(read / (float)frameSize);

                    for (int i = channel; i < limit; i += m_pcmFormat.NumberOfChannels)
                    {
                        if (samples[i] < min) min = samples[i];
                        if (samples[i] > max) max = samples[i];
                    }

                    double y1 = height
                                - ((min - short.MinValue) * height)
                                / ushort.MaxValue;

                    if (channel == 0)
                    {
                        sgcCh1.BeginFigure(new Point(x, y1), false, false);
                    }
                    else
                    {
                        y1 += height;
                        sgcCh2.BeginFigure(new Point(x, y1), false, false);
                    }


                    double y2 = height
                                - ((max - short.MinValue) * height)
                                / ushort.MaxValue;
                    if (channel == 0)
                    {
                        sgcCh1.LineTo(new Point(x, y2), true, false);
                    }
                    else
                    {
                        y2 += height;
                        sgcCh2.LineTo(new Point(x, y2), true, false);
                    }
                }
            }

            m_FilePlayStream.Close();
            m_FilePlayStream = null;

            sgcCh1.Close();
            if (m_pcmFormat.NumberOfChannels > 1)
            {
                sgcCh2.Close();
            }


            DrawingImage drawImg = new DrawingImage();
            //
            geometryCh1.Freeze();
            GeometryDrawing geoDraw1 = new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.LimeGreen, 1.0), geometryCh1);
            geoDraw1.Freeze();
            //
            GeometryDrawing geoDraw2 = null;
            if (m_pcmFormat.NumberOfChannels > 1)
            {
                geometryCh2.Freeze();
                geoDraw2 = new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.LimeGreen, 1.0), geometryCh2);
                geoDraw2.Freeze();
            }
            //
            if (m_pcmFormat.NumberOfChannels > 1)
            {
                DrawingGroup drawGrp = new DrawingGroup();
                drawGrp.Children.Add(geoDraw1);
                drawGrp.Children.Add(geoDraw2);
                drawGrp.Freeze();
                drawImg.Drawing = drawGrp;
            }
            else
            {
                drawImg.Drawing = geoDraw1;
            }
            drawImg.Freeze();
            WaveFormImage.Source = drawImg;


            m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;

            if (wasPlaying)
            {
                m_Player.Resume();
            }
        }
        public int PeakOverloadCountCh1
        {
            get
            {
                return m_PeakMeterBarDataCh1.PeakOverloadCount;
            }
            set
            {
                if (m_PeakMeterBarDataCh1.PeakOverloadCount == value) return;
                m_PeakMeterBarDataCh1.PeakOverloadCount = value;
                OnPropertyChanged("PeakOverloadCountCh1");
            }
        }
        public int PeakOverloadCountCh2
        {
            get
            {
                return m_PeakMeterBarDataCh2.PeakOverloadCount;
            }
            set
            {
                if (m_PeakMeterBarDataCh2.PeakOverloadCount == value) return;
                m_PeakMeterBarDataCh2.PeakOverloadCount = value;
                OnPropertyChanged("PeakOverloadCountCh2");
            }
        }
        public string FileAudioPath
        {
            get
            {
                return m_WavFilePath;
            }
            set
            {
                if (m_WavFilePath == value) return;
                m_WavFilePath = value;
                OnPropertyChanged("FileAudioPath");
            }
        }

        private void OnPeakMeterCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            updatePeakMeter();
        }

        private void OnWaveFormCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {

            PresentationSource ps = PresentationSource.FromVisual(this);
            if (ps != null)
            {
                Matrix m = ps.CompositionTarget.TransformToDevice;
                double dpiFactor = 1 / m.M11;
                WaveFormPlayHeadPath.StrokeThickness = 1 * dpiFactor; // 1px
            }
            updateWaveFormPlayHead();



            if (!e.WidthChanged || m_pcmFormat == null)
            {
                return;
            }

            ushort channels = m_pcmFormat.NumberOfChannels;
            ushort frameSize = m_pcmFormat.BlockAlign;
            double samplesPerPixel = Math.Ceiling(m_pcmFormat.DataLength
                / (double)frameSize / WaveFormCanvas.ActualWidth * channels);
            m_bytesPerPixel = samplesPerPixel * frameSize / channels;

            if (m_WaveFormLoadTimer == null)
            {
                m_WaveFormLoadTimer = new DispatcherTimer();
                m_WaveFormLoadTimer.Tick += OnWaveFormLoadTimerTick;
                m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(500);
            }
            else if (m_WaveFormLoadTimer.IsEnabled)
            {
                m_WaveFormLoadTimer.Stop();
                m_WaveFormLoadTimer.Start();
                return;
            }

            m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            m_WaveFormLoadTimer.Start();
        }

        private void OnWaveFormImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
            {
                return;
            }
        }

        private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(WaveFormImage);
            OnSurfaceMouseDown(p);
        }

        private void OnSurfaceMouseDown(Point p)
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
                            m_pcmFormat.GetDuration(m_pcmFormat.DataLength),
                            m_pcmFormat,
                            convertByteToMilliseconds(p.X * m_bytesPerPixel)
                            );
            }
            else if (m_Player.State == AudioPlayerState.Playing)
            {
                m_Player.CurrentTimePosition = convertByteToMilliseconds(p.X * m_bytesPerPixel);
            }
            updateWaveFormPlayHead();
        }

        private void OnResetPeakOverloadCountCh1(object sender, MouseButtonEventArgs e)
        {
            PeakOverloadCountCh1 = 0;
        }
        private void OnResetPeakOverloadCountCh2(object sender, MouseButtonEventArgs e)
        {
            PeakOverloadCountCh2 = 0;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            m_WaveFormLoadingAdorner = new WaveFormLoadingAdorner(WaveFormScroll);
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(WaveFormScroll);
            layer.Add(m_WaveFormLoadingAdorner);
            m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;
        }

        private WaveFormLoadingAdorner m_WaveFormLoadingAdorner;
    }
    public class WaveFormLoadingAdorner : Adorner
    {
        public WaveFormLoadingAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            FormattedText formattedText = new FormattedText(
                         "Loading...",
                         CultureInfo.GetCultureInfo("en-us"),
                         FlowDirection.LeftToRight,
                         new Typeface("Helvetica"),
                         40,
                         Brushes.Black
                         );

            double margin = 20;

            double width = ((ScrollViewer) AdornedElement).ActualWidth;
            double height = ((ScrollViewer) AdornedElement).ActualHeight - 20; 

            double leftOffset = (width - formattedText.Width) / 2;
            double topOffset = (height - formattedText.Height) / 2;


            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Black);
            renderBrush.Opacity = 0.6;
            Pen pen = new Pen(Brushes.White, 1);
            
            drawingContext.DrawRoundedRectangle(renderBrush, pen,
                                                new Rect(new Point(margin, margin),
                                                    new Size(width - margin - margin,
                                                             height - margin - margin)),
                                                10.0, 10.0);

            Geometry textGeometry = formattedText.BuildGeometry(
                new Point(leftOffset, topOffset));
            drawingContext.DrawGeometry(Brushes.White,
                                        new Pen(Brushes.Black, 1),
                                        textGeometry);
        }
    }

}
