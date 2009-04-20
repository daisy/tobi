using System;
using System.Collections.Generic;
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
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Infrastructure;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.timing;
using Colors = System.Windows.Media.Colors;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// Interaction logic for AudioPaneView.xaml
    /// </summary>
    public partial class AudioPaneView : INotifyPropertyChanged
    {
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

        private double m_LastPlayHeadTime = 0;

        private AudioPlayer m_Player;
        private AudioRecorder m_Recorder;
        private VuMeter m_VuMeter;

        //private GraphicalPeakMeter m_GraphicalPeakMeter;
        //private GraphicalVuMeter m_GraphicalVuMeter;

        private string m_WavFilePath;
        private Stream m_PlayStream;

        private List<TreeNodeAndStreamDataLength> m_PlayStreamMarkers;

        private long m_dataLength;
        private TreeNode m_CurrentTreeNode;
        private PCMFormatInfo m_pcmFormat;
        private double m_bytesPerPixel;
        private AudioPlayer.StreamProviderDelegate mCurrentAudioStreamProvider;
        private DispatcherTimer m_PlaybackTimer;
        private DispatcherTimer m_PeakMeterTimer;
        private DispatcherTimer m_WaveFormLoadTimer;
        private long m_StreamRiffHeaderEndPos;

        private PeakMeterBarData m_PeakMeterBarDataCh1;
        private PeakMeterBarData m_PeakMeterBarDataCh2;

        private double[] m_PeakMeterValues;

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

                double width = ((ScrollViewer)AdornedElement).ActualWidth;
                double height = ((ScrollViewer)AdornedElement).ActualHeight - 20;

                if (width <= margin + margin || height <= margin + margin)
                {
                    return;
                }

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

        private void InitializeAudioStuff()
        {
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



            m_PeakMeterBarDataCh1 = new PeakMeterBarData(peakMeterCanvasInvalidateVisual);
            m_PeakMeterBarDataCh2 = new PeakMeterBarData(peakMeterCanvasInvalidateVisual);

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

        private WaveFormLoadingAdorner m_WaveFormLoadingAdorner;

        protected IUnityContainer Container { get; private set; }
        private IEventAggregator m_eventAggregator;

        private TreeNode m_CurrentSubTreeNode;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public AudioPaneView(IUnityContainer container, IEventAggregator eventAggregator)
        {
            InitializeComponent();
            m_eventAggregator = eventAggregator;
            Container = container;
            PeakMeterCanvasBackground.Freeze();
            InitializeAudioStuff();
            m_eventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
            //m_eventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, ThreadOption.UIThread);
            DataContext = this;
        }

        /*private void OnSubTreeNodeSelected(TreeNode node)
        {
            m_CurrentSubTreeNode = node;
        }*/

        private void OnTreeNodeSelected(TreeNode node)
        {
            if (node == null)
            {
                return;
            }

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

            resetWaveForm();

            m_CurrentTreeNode = node;
            m_CurrentSubTreeNode = node;

            mCurrentAudioStreamProvider = () =>
            {
                if (m_CurrentTreeNode == null) return null;

                if (m_PlayStream == null)
                {
                    if (m_PlayStreamMarkers != null)
                    {
                        m_PlayStreamMarkers.Clear();
                        m_PlayStreamMarkers = null;
                    }

                    StreamWithMarkers? sm = m_CurrentTreeNode.GetManagedAudioDataFlattened();

                    if (sm == null)
                    {
                        TreeNode ancerstor = m_CurrentTreeNode.GetFirstAncestorWithManagedAudio();
                        if (ancerstor == null)
                        {
                            return null;
                        }

                        StreamWithMarkers? sma = ancerstor.GetManagedAudioData();
                        if (sma != null)
                        {
                            m_CurrentTreeNode = ancerstor;
                            m_PlayStream = sma.GetValueOrDefault().m_Stream;
                            m_PlayStreamMarkers = sma.GetValueOrDefault().m_SubStreamMarkers;
                        }
                    }
                    else
                    {
                        m_PlayStream = sm.GetValueOrDefault().m_Stream;
                        m_PlayStreamMarkers = sm.GetValueOrDefault().m_SubStreamMarkers;
                    }
                    if (m_PlayStream == null)
                    {
                        return null;
                    }
                    m_dataLength = m_PlayStream.Length;
                }
                return m_PlayStream;
            };

            if (mCurrentAudioStreamProvider() == null)
            {
                m_CurrentTreeNode = null;
                m_CurrentSubTreeNode = null;
                return;
            }

            FilePath = "";

            loadAndPlay();
        }
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

            resetWaveForm();

            FilePath = dlg.FileName;
            m_CurrentTreeNode = null;
            m_CurrentSubTreeNode = null;

            mCurrentAudioStreamProvider = () =>
            {
                if (m_PlayStream == null)
                {
                    if (m_PlayStreamMarkers != null)
                    {
                        m_PlayStreamMarkers.Clear();
                        m_PlayStreamMarkers = null;
                    }
                    if (!String.IsNullOrEmpty(FilePath))
                    {
                        if (!File.Exists(FilePath))
                        {
                            return null;
                        }
                        m_PlayStream = File.Open(FilePath, FileMode.Open);
                    }
                    if (m_PlayStream == null)
                    {
                        return null;
                    }

                    m_dataLength = m_PlayStream.Length;
                }
                return m_PlayStream;
            };

            if (mCurrentAudioStreamProvider() == null)
            {
                FilePath = "Could not get file stream !";
                return;
            }

            loadAndPlay();
        }
        private void loadAndPlay()
        {
            if (mCurrentAudioStreamProvider() == null)
            {
                return;
            }
            //else the stream is now open

            if (m_Player.State != AudioPlayerState.NotReady && m_Player.State != AudioPlayerState.Stopped)
            {
                m_Player.Stop();
            }

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

            if (m_pcmFormat == null)
            {
                m_PlayStream.Position = 0;
                m_PlayStream.Seek(0, SeekOrigin.Begin);

                if (FilePath.Length > 0)
                {
                    m_pcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_PlayStream);
                    m_StreamRiffHeaderEndPos = m_PlayStream.Position;
                }
                else
                {
                    m_pcmFormat = m_CurrentTreeNode.Presentation.MediaDataManager.DefaultPCMFormat.Copy();
                }
            }

            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            }

            loadWaveForm(); // will close the stream so that we can pass the stream onto the player 

            TimeDelta dur = m_pcmFormat.GetDuration(m_dataLength);
            m_Player.Play(mCurrentAudioStreamProvider,
                        dur,
                        m_pcmFormat);
        }

        private void OnAudioPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                m_PlayStream = null;
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
            m_PlaybackTimer = null;
        }
        private void stopPeakMeterTimer()
        {
            if (m_PeakMeterTimer != null && m_PeakMeterTimer.IsEnabled)
            {
                m_PeakMeterTimer.Stop();
            }
            m_PeakMeterTimer = null;
        }

        private void startPeakMeterTimer()
        {
            if (m_PeakMeterTimer == null)
            {
                m_PeakMeterTimer = new DispatcherTimer();
                m_PeakMeterTimer.Tick += OnPeakMeterTimerTick;
                m_PeakMeterTimer.Interval = TimeSpan.FromMilliseconds(60);
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

                double interval = convertByteToMilliseconds(m_bytesPerPixel);

                if (interval < 20.0)
                {
                    interval = 20;
                }
                m_PlaybackTimer.Interval = TimeSpan.FromMilliseconds(interval);
            }
            else if (m_PlaybackTimer.IsEnabled)
            {
                return;
            }

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
            return 1000.0 * bytes / ((double)m_pcmFormat.SampleRate * m_pcmFormat.NumberOfChannels * m_pcmFormat.BitDepth / 8.0);
        }
        public double convertMillisecondsToByte(double ms)
        {
            if (m_pcmFormat == null)
            {
                return 0;
            }
            return (ms * m_pcmFormat.SampleRate * m_pcmFormat.NumberOfChannels * m_pcmFormat.BitDepth / 8.0) / 1000.0;
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
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            }
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
            if (m_pcmFormat != null)
            {
                double time = m_pcmFormat.GetDuration(m_dataLength).TimeDeltaAsMillisecondDouble;
                updateWaveFormPlayHead(time);
            }
            if (FilePath.Length > 0 || m_CurrentTreeNode == null)
            {
                return;
            }
            TreeNode nextNode = m_CurrentTreeNode.GetNextSiblingWithManagedAudio();
            if (nextNode != null)
            {
                m_eventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
            }
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
                        sgc.LineTo(new Point(barWidth, availableHeight - 20), true, false);

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
            m_LastPlayHeadTime = time;

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

            if (m_PlayStreamMarkers == null)
            {
                return;
            }

            TreeNode subTreeNode = null;

            long sumData = 0;
            long sumDataPrev = 0;
            foreach (TreeNodeAndStreamDataLength markers in m_PlayStreamMarkers)
            {
                sumData += markers.m_LocalStreamDataLength;
                if (byteOffset <= sumData)
                {
                    subTreeNode = markers.m_TreeNode;
                    break;
                }
                sumDataPrev = sumData;
            }

            if (subTreeNode == null || subTreeNode == m_CurrentSubTreeNode)
            {
                return;
            }

            m_CurrentSubTreeNode = subTreeNode;
            //m_CurrentSubTreeNode_OffsetLeft = sumDataPrev;
            //m_CurrentSubTreeNode_OffsetRight = sumData;

            double pixelsLeft = sumDataPrev / m_bytesPerPixel;
            double pixelsRight = sumData / m_bytesPerPixel;

            StreamGeometry geometryRange = null;
            if (WaveFormTimeRangePath.Data == null)
            {
                geometryRange = new StreamGeometry();
            }
            else
            {
                geometryRange = (StreamGeometry)WaveFormTimeRangePath.Data;
            }

            using (StreamGeometryContext sgc = geometryRange.Open())
            {
                sgc.BeginFigure(new Point(pixelsLeft, WaveFormCanvas.ActualHeight), true, true);
                sgc.LineTo(new Point(pixelsRight, WaveFormCanvas.ActualHeight), true, false);
                sgc.LineTo(new Point(pixelsRight, WaveFormCanvas.ActualHeight - 5), true, false);
                sgc.LineTo(new Point(pixelsLeft, WaveFormCanvas.ActualHeight - 5), true, false);

                sgc.Close();
            }

            if (WaveFormTimeRangePath.Data == null)
            {
                WaveFormTimeRangePath.Data = geometryRange;
            }

            WaveFormTimeRangePath.InvalidateVisual();

            m_eventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(m_CurrentSubTreeNode);
        }

        private void updateWaveFormPlayHead()
        {
            if (m_pcmFormat == null)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                double time = m_LastPlayHeadTime;
                if (m_Player.State == AudioPlayerState.Playing)
                {
                    time = m_Player.CurrentTimePosition;
                }
                updateWaveFormPlayHead(time);
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(updateWaveFormPlayHead));
            }
        }

        private void resetWaveForm()
        {
            DrawingImage drawImg = new DrawingImage();
            StreamGeometry geometry = new StreamGeometry();
            StreamGeometryContext sgc = geometry.Open();

            sgc.BeginFigure(new Point(0, 0), true, true);
            sgc.LineTo(new Point(0, WaveFormCanvas.ActualHeight), true, false);
            sgc.LineTo(new Point(WaveFormCanvas.ActualWidth, WaveFormCanvas.ActualHeight), true, false);
            sgc.LineTo(new Point(WaveFormCanvas.ActualWidth, 0), true, false);
            sgc.Close();
            geometry.Freeze();
            GeometryDrawing geoDraw = new GeometryDrawing(Brushes.Black, new Pen(Brushes.Black, 1.0), geometry);
            geoDraw.Freeze();
            DrawingGroup drawGrp = new DrawingGroup();
            drawGrp.Children.Add(geoDraw);
            drawGrp.Freeze();
            drawImg.Drawing = drawGrp;
            drawImg.Freeze();
            WaveFormImage.Source = drawImg;

            m_LastPlayHeadTime = 0;

            WaveFormPlayHeadPath.Data = null;
            WaveFormPlayHeadPath.InvalidateVisual();

            WaveFormTimeRangePath.Data = null;
            WaveFormTimeRangePath.InvalidateVisual();
        }

        private void loadWaveForm()
        {
            double stepX = 3;
            bool bShowEnvelope = true;
            bool bFillEnvelope = true;
            bool bShowSamples = false;

            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            bool wasPlaying = (m_Player.State == AudioPlayerState.Playing);

            if (m_Player.State != AudioPlayerState.NotReady)
            {
                if (wasPlaying)
                {
                    m_Player.Pause();
                }
            }

            if (m_pcmFormat.BitDepth != 16)
            {
                if (!wasPlaying)
                {
                    m_PlayStream.Close();
                    m_PlayStream = null;
                }
                return;
            }

            if (mCurrentAudioStreamProvider() == null)
            {
                return;
            }
            // else: the stream is now open, thus why we have a try/finally wrapper below:


            if (m_pcmFormat.NumberOfChannels == 1)
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Collapsed;
            }
            else
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Visible;
            }

            StreamGeometry geometryCh1 = new StreamGeometry();
            StreamGeometryContext sgcCh1 = geometryCh1.Open();

            StreamGeometry geometryCh1_envelope = new StreamGeometry();
            StreamGeometryContext sgcCh1_envelope = geometryCh1_envelope.Open();

            StreamGeometry geometryCh2 = null;
            StreamGeometryContext sgcCh2 = null;

            StreamGeometry geometryCh2_envelope = null;
            StreamGeometryContext sgcCh2_envelope = null;

            if (m_pcmFormat.NumberOfChannels > 1)
            {
                geometryCh2 = new StreamGeometry();
                sgcCh2 = geometryCh2.Open();

                geometryCh2_envelope = new StreamGeometry();
                sgcCh2_envelope = geometryCh2_envelope.Open();
            }

            double height = WaveFormCanvas.ActualHeight;
            if (m_pcmFormat.NumberOfChannels > 1)
            {
                height /= 2;
            }

            double prevY1 = -1;
            double prevY2 = -1;
            double prevY1_ = -1;
            double prevY2_ = -1;

            m_bytesPerPixel = m_dataLength / WaveFormCanvas.ActualWidth;

            int byteDepth = m_pcmFormat.BitDepth / 8; //bytes per sample (data for one channel only)

            int samplesPerStep = (int)Math.Floor((m_bytesPerPixel * stepX) / byteDepth);
            samplesPerStep += (samplesPerStep % m_pcmFormat.NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;

            byte[] bytes = new byte[bytesPerStep];
            short[] samples = new short[samplesPerStep];

            List<Point> listTopPointsCh1 = null;
            List<Point> listTopPointsCh2 = null;
            List<Point> listBottomPointsCh1 = null;
            List<Point> listBottomPointsCh2 = null;

            if (bFillEnvelope)
            {
                listTopPointsCh1 = new List<Point>();
                listTopPointsCh2 = new List<Point>();
                listBottomPointsCh1 = new List<Point>();
                listBottomPointsCh2 = new List<Point>();
            }

            int read = 0;
            double x = 0;
            try
            {
                if (FilePath.Length > 0)
                {
                    m_PlayStream.Position = m_StreamRiffHeaderEndPos;
                    m_PlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
                }
                while ((read = m_PlayStream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    Buffer.BlockCopy(bytes, 0, samples, 0, Math.Min(read, samples.Length));

                    short min = short.MaxValue;
                    short max = short.MinValue;
                    for (int channel = 0; channel < m_pcmFormat.NumberOfChannels; channel++)
                    {
                        //int limit = (int)Math.Ceiling(read / (float)frameSize);
                        //int limit = (int)Math.Ceiling(samplesPerPixel / m_pcmFormat.NumberOfChannels);

                        int limit = samples.Length;

                        if (read < bytes.Length)
                        {
                            int nSamples = (int)Math.Floor((double)read / byteDepth);
                            nSamples = m_pcmFormat.NumberOfChannels * (int)Math.Floor((double)nSamples / m_pcmFormat.NumberOfChannels);
                            limit = nSamples;
                            limit = Math.Min(limit, samples.Length);
                        }

                        for (int i = channel; i < limit; i += m_pcmFormat.NumberOfChannels)
                        {
                            if (samples[i] < min)
                            {
                                min = samples[i];
                            }
                            if (samples[i] > max)
                            {
                                max = samples[i];
                            }
                        }

                        double y1 = Math.Min(height, height
                                    - ((min - short.MinValue) * height)
                                      / ushort.MaxValue);

                        double y2 = Math.Max(0, height
                                    - ((max - short.MinValue) * height)
                                      / ushort.MaxValue);

                        if (channel == 0)
                        {
                            sgcCh1.BeginFigure(new Point(x, y1), false, false);

                            if (bFillEnvelope)
                            {
                                listTopPointsCh1.Add(new Point(x, y1));
                            }

                            if (prevY1 == -1)
                            {
                                //sgcCh1_envelope.BeginFigure(new Point(x, y1), false, false);
                            }
                            else
                            {
                                if (!bFillEnvelope)
                                {
                                    sgcCh1_envelope.BeginFigure(new Point(x - stepX, prevY1), false, false);
                                    sgcCh1_envelope.LineTo(new Point(x, y1), true, false);
                                }
                            }
                            prevY1 = y1;
                        }
                        else
                        {
                            y1 += height;
                            sgcCh2.BeginFigure(new Point(x, y1), false, false);
                            
                            if (bFillEnvelope)
                            {
                                listTopPointsCh2.Add(new Point(x, y1));
                            }

                            if (prevY1_ == -1)
                            {
                                //sgcCh2_envelope.BeginFigure(new Point(x, y1), false, false);
                            }
                            else
                            {
                                prevY1_ += height;
                                if (!bFillEnvelope)
                                {
                                    sgcCh2_envelope.BeginFigure(new Point(x - stepX, prevY1_), false, false);
                                    sgcCh2_envelope.LineTo(new Point(x, y1), true, false);
                                }
                            }
                            prevY1_ = y1 - height;
                        }

                        if (channel == 0)
                        {
                            sgcCh1.LineTo(new Point(x, y2), true, false);

                            if (bFillEnvelope)
                            {
                                listBottomPointsCh1.Add(new Point(x, y2));
                            }

                            if (prevY2 == -1)
                            {
                                //sgcCh1_envelope.BeginFigure(new Point(x, y2), false, false);
                            }
                            else
                            {
                                if (!bFillEnvelope)
                                {
                                    sgcCh1_envelope.BeginFigure(new Point(x - stepX, prevY2), false, false);
                                    sgcCh1_envelope.LineTo(new Point(x, y2), true, false);
                                }
                            }
                            prevY2 = y2;
                        }
                        else
                        {
                            y2 += height;
                            sgcCh2.LineTo(new Point(x, y2), true, false);

                            if (bFillEnvelope)
                            {
                                listBottomPointsCh2.Add(new Point(x, y2));
                            }

                            if (prevY2_ == -1)
                            {
                                //sgcCh2_envelope.BeginFigure(new Point(x, y2), false, false);
                            }
                            else
                            {
                                prevY2_ += height;
                                if (!bFillEnvelope)
                                {
                                    sgcCh2_envelope.BeginFigure(new Point(x - stepX, prevY2_), false, false);
                                    sgcCh2_envelope.LineTo(new Point(x, y2), true, false);
                                }
                            }
                            prevY2_ = y2 - height;
                        }
                    }

                    x += (read / m_bytesPerPixel); //stepX;
                    if (x > WaveFormCanvas.ActualWidth)
                    {
                        break;
                    }
                }

                if (bFillEnvelope)
                {
                    listBottomPointsCh1.Reverse();
                    listTopPointsCh1.AddRange(listBottomPointsCh1);
                    listBottomPointsCh1.Clear();
                    int count = 0;
                    foreach (Point p in listTopPointsCh1)
                    {
                        if (count == 0)
                        {
                            sgcCh1_envelope.BeginFigure(p, true, true);
                        }
                        else
                        {
                            sgcCh1_envelope.LineTo(p, true, false);
                        }
                        count++;
                    }
                    if (m_pcmFormat.NumberOfChannels > 1)
                    {
                        listBottomPointsCh2.Reverse();
                        listTopPointsCh2.AddRange(listBottomPointsCh2);
                        listBottomPointsCh2.Clear();
                        count = 0;
                        foreach (Point p in listTopPointsCh2)
                        {
                            if (count == 0)
                            {
                                sgcCh2_envelope.BeginFigure(p, true, true);
                            }
                            else
                            {
                                sgcCh2_envelope.LineTo(p, true, false);
                            }
                            count++;
                        }
                    }
                }

                sgcCh1.Close();
                sgcCh1_envelope.Close();
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    sgcCh2.Close();
                    sgcCh2_envelope.Close();
                }

                DrawingImage drawImg = new DrawingImage();
                //
                geometryCh1.Freeze();
                GeometryDrawing geoDraw1 = new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.LimeGreen, 1.0), geometryCh1);
                geoDraw1.Freeze();
                //
                geometryCh1_envelope.Freeze();
                GeometryDrawing geoDraw1_envelope = new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.GreenYellow, 1.0), geometryCh1_envelope);
                geoDraw1_envelope.Freeze();
                //
                GeometryDrawing geoDraw2 = null;
                GeometryDrawing geoDraw2_envelope = null;
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    geometryCh2.Freeze();
                    geoDraw2 = new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.LimeGreen, 1.0), geometryCh2);
                    geoDraw2.Freeze();
                    geometryCh2_envelope.Freeze();
                    geoDraw2_envelope = new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.GreenYellow, 1.0), geometryCh2_envelope);
                    geoDraw2_envelope.Freeze();
                }
                //
                GeometryDrawing geoDrawMarkers = null;
                if (m_PlayStreamMarkers != null)
                {
                    StreamGeometry geometryMarkers = new StreamGeometry();
                    StreamGeometryContext sgcMarkers = geometryMarkers.Open();

                    long sumData = 0;
                    foreach (TreeNodeAndStreamDataLength markers in m_PlayStreamMarkers)
                    {
                        double pixels = (sumData + markers.m_LocalStreamDataLength) / m_bytesPerPixel;

                        sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
                        sgcMarkers.LineTo(new Point(pixels, WaveFormCanvas.ActualHeight), true, false);

                        sumData += markers.m_LocalStreamDataLength;
                    }
                    sgcMarkers.Close();

                    geometryMarkers.Freeze();
                    geoDrawMarkers = new GeometryDrawing(Brushes.BlueViolet,
                                                                         new Pen(Brushes.BlueViolet, 1.0),
                                                                         geometryMarkers);
                    geoDrawMarkers.Freeze();
                }
                //
                StreamGeometry geometryBack = new StreamGeometry();
                StreamGeometryContext sgcBack = geometryBack.Open();

                sgcBack.BeginFigure(new Point(0, 0), true, true);
                sgcBack.LineTo(new Point(0, WaveFormCanvas.ActualHeight), true, false);
                sgcBack.LineTo(new Point(WaveFormCanvas.ActualWidth, WaveFormCanvas.ActualHeight), true, false);
                sgcBack.LineTo(new Point(WaveFormCanvas.ActualWidth, 0), true, false);
                sgcBack.Close();
                geometryBack.Freeze();
                GeometryDrawing geoDrawBack = new GeometryDrawing(Brushes.Black, new Pen(Brushes.Black, 1.0), geometryBack);
                geoDrawBack.Freeze();
                //
                DrawingGroup drawGrp = new DrawingGroup();
                //
                drawGrp.Children.Add(geoDrawBack);
                if (bShowSamples)
                {
                    drawGrp.Children.Add(geoDraw1);
                }
                if (bShowEnvelope)
                {
                    drawGrp.Children.Add(geoDraw1_envelope);
                }
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    if (bShowSamples)
                    {
                        drawGrp.Children.Add(geoDraw2);
                    }
                    if (bShowEnvelope)
                    {
                        drawGrp.Children.Add(geoDraw2_envelope);
                    }
                }
                if (m_PlayStreamMarkers != null)
                {
                    drawGrp.Children.Add(geoDrawMarkers);
                }
                drawGrp.Freeze();
                drawImg.Drawing = drawGrp;
                drawImg.Freeze();
                WaveFormImage.Source = drawImg;

                if (m_WaveFormLoadingAdorner != null)
                {
                    m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;
                }
            }
            finally
            {
                // ensure the stream is closed before we resume the player
                m_PlayStream.Close();
                m_PlayStream = null;
            }

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
                WaveFormTimeRangePath.StrokeThickness = 1 * dpiFactor;
            }

            m_bytesPerPixel = m_dataLength / WaveFormCanvas.ActualWidth;

            m_CurrentSubTreeNode = null;
            updateWaveFormPlayHead();

            if (!e.WidthChanged || m_pcmFormat == null)
            {
                return;
            }

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


            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            }
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
                mCurrentAudioStreamProvider(); // ensure m_PlayStream is open

                m_Player.Play(mCurrentAudioStreamProvider,
                            m_pcmFormat.GetDuration(m_dataLength),
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

        private void OnPaneLoaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(WaveFormScroll);
            if (layer == null)
            {
                m_WaveFormLoadingAdorner = null;
                return;
            }
            m_WaveFormLoadingAdorner = new WaveFormLoadingAdorner(WaveFormScroll);
            layer.Add(m_WaveFormLoadingAdorner);
            m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;
        }

    }
}
