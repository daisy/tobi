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
using NAudio.Utils;
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

            startWaveFormLoadTimer(10, true);
        }

        private void OnAudioPlayerStateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.OldState == AudioPlayerState.Playing
                && (m_Player.State == AudioPlayerState.Paused
                    || m_Player.State == AudioPlayerState.Stopped))
            {
                updatePeakMeter();
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
                updatePeakMeter();
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
                m_PeakMeterTimer = new DispatcherTimer(DispatcherPriority.Input);
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
                m_PlaybackTimer = new DispatcherTimer(DispatcherPriority.Send);
                m_PlaybackTimer.Tick += OnPlaybackTimerTick;

                double interval = convertByteToMilliseconds(m_bytesPerPixel);

                if (interval < 60.0)
                {
                    interval = 60;
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
            loadWaveForm(m_forcePlayAfterWaveFormLoaded);
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

            updatePeakMeter();

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
            if (Dispatcher.CheckAccess())
            {
                if (m_Player.State != AudioPlayerState.Playing)
                {
                    PeakMeterCanvasOpaqueMask.Visibility = Visibility.Visible;
                    return;
                }
                if (m_pcmFormat == null)
                {
                    return;
                }
                PeakMeterCanvasOpaqueMask.Visibility = Visibility.Hidden;

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
                    sgc.LineTo(new Point(barWidth, 0), false, false);
                    sgc.LineTo(new Point(barWidth, availableHeight - pixels), false, false);
                    sgc.LineTo(new Point(0, availableHeight - pixels), false, false);

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
                        sgc.LineTo(new Point(barWidth + barWidth, 0), false, false);
                        sgc.LineTo(new Point(barWidth + barWidth, availableHeight - pixels), false, false);
                        sgc.LineTo(new Point(barWidth, availableHeight - pixels), false, false);
                        sgc.LineTo(new Point(barWidth, availableHeight - 1), false, false);

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

            double height = WaveFormCanvas.ActualHeight;
            if (height == Double.NaN || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            int arrowDepth = 6;

            using (StreamGeometryContext sgc = geometry.Open())
            {
                sgc.BeginFigure(new Point(pixels, height - arrowDepth), true, false);
                sgc.LineTo(new Point(pixels + arrowDepth, height), true, false);
                sgc.LineTo(new Point(pixels - arrowDepth, height), true, false);
                sgc.LineTo(new Point(pixels, height - arrowDepth), true, false);
                sgc.LineTo(new Point(pixels, arrowDepth), true, false);
                sgc.LineTo(new Point(pixels - arrowDepth, 0), true, false);
                sgc.LineTo(new Point(pixels + arrowDepth, 0), true, false);
                sgc.LineTo(new Point(pixels, arrowDepth), true, false);

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
                sgc.BeginFigure(new Point(pixelsLeft, height - arrowDepth), true, false);
                sgc.LineTo(new Point(pixelsRight, height - arrowDepth), false, false);
                sgc.LineTo(new Point(pixelsRight, height), false, false);
                sgc.LineTo(new Point(pixelsLeft, height), false, false);
                sgc.LineTo(new Point(pixelsLeft, 0), false, false);
                sgc.LineTo(new Point(pixelsRight, 0), false, false);
                sgc.LineTo(new Point(pixelsRight, arrowDepth), false, false);
                sgc.LineTo(new Point(pixelsLeft, arrowDepth), false, false);
                sgc.LineTo(new Point(pixelsLeft, 0), false, false);

                sgc.Close();
            }

            if (WaveFormTimeRangePath.Data == null)
            {
                WaveFormTimeRangePath.Data = geometryRange;
            }

            WaveFormTimeRangePath.InvalidateVisual();


            if (subTreeNode == null || (subTreeNode == m_CurrentSubTreeNode && subTreeNode != m_CurrentTreeNode))
            {
                return;
            }
            if (m_CurrentSubTreeNode != m_CurrentTreeNode)
            {
                m_eventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(m_CurrentSubTreeNode);
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
                double time = m_LastPlayHeadTime;
                if (m_Player.State == AudioPlayerState.Playing)
                {
                    time = m_Player.CurrentTimePosition;
                }
                updateWaveFormPlayHead(time);
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(updateWaveFormPlayHead));
            }
        }

        private void resetWaveFormBackground()
        {
            double height = WaveFormCanvas.ActualHeight;
            if (height == Double.NaN || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            double width = WaveFormCanvas.ActualWidth;
            if (width == Double.NaN || width == 0)
            {
                width = WaveFormCanvas.Width;
            }

            DrawingImage drawImg = new DrawingImage();
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext sgc = geometry.Open())
            {
                sgc.BeginFigure(new Point(0, 0), true, true);
                sgc.LineTo(new Point(0, height), true, false);
                sgc.LineTo(new Point(width, height), true, false);
                sgc.LineTo(new Point(width, 0), true, false);
                sgc.Close();
            }

            geometry.Freeze();

            Brush brushColorBack = new SolidColorBrush(ColorWaveBackground);
            GeometryDrawing geoDraw = new GeometryDrawing(brushColorBack, new Pen(brushColorBack, 1.0), geometry);
            geoDraw.Freeze();
            DrawingGroup drawGrp = new DrawingGroup();
            drawGrp.Children.Add(geoDraw);
            drawGrp.Freeze();
            drawImg.Drawing = drawGrp;
            drawImg.Freeze();
            WaveFormImage.Source = drawImg;
        }

        private void resetWaveForm()
        {
            resetWaveFormBackground();

            m_LastPlayHeadTime = 0;

            WaveFormPlayHeadPath.Data = null;
            WaveFormPlayHeadPath.InvalidateVisual();

            WaveFormTimeRangePath.Data = null;
            WaveFormTimeRangePath.InvalidateVisual();

            PeakMeterPathCh2.Data = null;
            PeakMeterPathCh2.InvalidateVisual();

            PeakMeterPathCh1.Data = null;
            PeakMeterPathCh1.InvalidateVisual();

            PeakMeterCanvasOpaqueMask.Visibility = Visibility.Visible;
        }
        private List<Double> m_DecibelResolutions = null;
        public List<Double> DecibelResolutions
        {
            get
            {
                if (m_DecibelResolutions == null)
                {
                    m_DecibelResolutions = new List<double>();
                    m_DecibelResolutions.Add(4.0);
                    m_DecibelResolutions.Add(3.5);
                    m_DecibelResolutions.Add(3.0);
                    m_DecibelResolutions.Add(2.5);
                    m_DecibelResolutions.Add(2.0);
                    m_DecibelResolutions.Add(1.5);
                    m_DecibelResolutions.Add(1.0);
                    m_DecibelResolutions.Add(0.9);
                    m_DecibelResolutions.Add(0.8);
                    m_DecibelResolutions.Add(0.7);
                    m_DecibelResolutions.Add(0.6);
                    m_DecibelResolutions.Add(0.5);
                    m_DecibelResolutions.Add(0.4);
                    m_DecibelResolutions.Add(0.3);
                    m_DecibelResolutions.Add(0.2);
                    m_DecibelResolutions.Add(0.1);
                    m_DecibelResolutions.Add(0.09);
                    m_DecibelResolutions.Add(0.08);
                    m_DecibelResolutions.Add(0.07);
                    m_DecibelResolutions.Add(0.06);
                    m_DecibelResolutions.Add(0.05);
                    m_DecibelResolutions.Add(0.04);
                    m_DecibelResolutions.Add(0.03);
                    m_DecibelResolutions.Add(0.02);
                    m_DecibelResolutions.Add(0.01);
                    m_DecibelResolutions.Add(0.0);
                }
                return m_DecibelResolutions;
            }
        }
        private double m_DecibelResolution = 1;
        public double DecibelResolution
        {
            get
            {
                return m_DecibelResolution;
            }
            set
            {
                if (m_DecibelResolution == value) return;
                m_DecibelResolution = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("DecibelResolution");
            }
        }

        private List<Double> m_WaveStepXs = null;
        public List<Double> WaveStepXs
        {
            get
            {
                if (m_WaveStepXs == null)
                {
                    m_WaveStepXs = new List<double>();
                    m_WaveStepXs.Add(1.0);
                    m_WaveStepXs.Add(2.0);
                    m_WaveStepXs.Add(3.0);
                    m_WaveStepXs.Add(4.0);
                    m_WaveStepXs.Add(5.0);
                    m_WaveStepXs.Add(6.0);
                    m_WaveStepXs.Add(7.0);
                    m_WaveStepXs.Add(8.0);
                    m_WaveStepXs.Add(9.0);
                    m_WaveStepXs.Add(10.0);
                }
                return m_WaveStepXs;
            }
        }
        private double m_WaveStepX = 2;
        public double WaveStepX
        {
            get
            {
                return m_WaveStepX;
            }
            set
            {
                if (m_WaveStepX == value) return;
                m_WaveStepX = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("WaveStepX");
            }
        }

        private bool m_IsUseDecibelsAdjust = true;
        public bool IsUseDecibelsAdjust
        {
            get
            {
                return m_IsUseDecibelsAdjust;
            }
            set
            {
                if (m_IsUseDecibelsAdjust == value) return;
                m_IsUseDecibelsAdjust = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsUseDecibelsAdjust");
            }
        }
        private bool m_IsUseDecibels = false;
        public bool IsUseDecibels
        {
            get
            {
                return m_IsUseDecibels;
            }
            set
            {
                if (m_IsUseDecibels == value) return;
                m_IsUseDecibels = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsUseDecibels");
            }
        }
        private bool m_IsUseDecibelsNoAverage = false;
        public bool IsUseDecibelsNoAverage
        {
            get
            {
                return m_IsUseDecibelsNoAverage;
            }
            set
            {
                if (m_IsUseDecibelsNoAverage == value) return;
                m_IsUseDecibelsNoAverage = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsUseDecibelsNoAverage");
            }
        }
        /*
        private bool m_IsUseDecibelsIntensity = false;
        public bool IsUseDecibelsIntensity
        {
            get
            {
                return m_IsUseDecibelsIntensity;
            }
            set
            {
                if (m_IsUseDecibelsIntensity == value) return;
                m_IsUseDecibelsIntensity = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsUseDecibelsIntensity");
            }
        }*/

        private bool m_IsBackgroundVisible = true;
        public bool IsBackgroundVisible
        {
            get
            {
                return m_IsBackgroundVisible;
            }
            set
            {
                if (m_IsBackgroundVisible == value) return;
                m_IsBackgroundVisible = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsBackgroundVisible");
            }
        }

        /*
        private bool m_IsAdjustOffsetFix = false;
        public bool IsAdjustOffsetFix
        {
            get
            {
                return m_IsAdjustOffsetFix;
            }
            set
            {
                if (m_IsAdjustOffsetFix == value) return;
                m_IsAdjustOffsetFix = value;
                resetWaveFormBackground();
                loadWaveForm();
                OnPropertyChanged("IsAdjustOffsetFix");
            }
        }*/

        private bool m_IsWaveFillVisible = false;
        public bool IsWaveFillVisible
        {
            get
            {
                return m_IsWaveFillVisible;
            }
            set
            {
                if (m_IsWaveFillVisible == value) return;
                m_IsWaveFillVisible = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsWaveFillVisible");
            }
        }

        private bool m_IsEnvelopeVisible = true;
        public bool IsEnvelopeVisible
        {
            get
            {
                return m_IsEnvelopeVisible;
            }
            set
            {
                if (m_IsEnvelopeVisible == value) return;
                m_IsEnvelopeVisible = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsEnvelopeVisible");
            }
        }
        private bool m_IsEnvelopeFilled = true;
        public bool IsEnvelopeFilled
        {
            get
            {
                return m_IsEnvelopeFilled;
            }
            set
            {
                if (m_IsEnvelopeFilled == value) return;
                m_IsEnvelopeFilled = value;
                //resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("IsEnvelopeFilled");
            }
        }

        private Color m_ColorPlayhead = Colors.Red;
        public Color ColorPlayhead
        {
            get
            {
                return m_ColorPlayhead;
            }
            set
            {
                if (m_ColorPlayhead == value) return;
                m_ColorPlayhead = value;
                Brush brush = new SolidColorBrush(m_ColorPlayhead);
                WaveFormPlayHeadPath.Stroke = brush;
                //updateWaveFormPlayHead();
                OnPropertyChanged("ColorPlayhead");
            }
        }
        private Color m_ColorPlayheadFill = Colors.Gold;
        public Color ColorPlayheadFill
        {
            get
            {
                return m_ColorPlayheadFill;
            }
            set
            {
                if (m_ColorPlayheadFill == value) return;
                m_ColorPlayheadFill = value;
                Brush brush = new SolidColorBrush(m_ColorPlayheadFill);
                WaveFormPlayHeadPath.Fill = brush;
                //updateWaveFormPlayHead();
                OnPropertyChanged("ColorPlayheadFill");
            }
        }
        private Color m_ColorWaveBackground = Colors.Black;
        public Color ColorWaveBackground
        {
            get
            {
                return m_ColorWaveBackground;
            }
            set
            {
                if (m_ColorWaveBackground == value) return;
                m_ColorWaveBackground = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("ColorWaveBackground");
            }
        }
        private Color m_ColorMarkers = Colors.Maroon;
        public Color ColorMarkers
        {
            get
            {
                return m_ColorMarkers;
            }
            set
            {
                if (m_ColorMarkers == value) return;
                m_ColorMarkers = value;
                Brush brush = new SolidColorBrush(m_ColorMarkers);
                WaveFormTimeRangePath.Fill = brush;
                WaveFormTimeRangePath.Stroke = brush;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("ColorMarkers");
            }
        }
        private Color m_ColorWaveBars = Colors.Lime;
        public Color ColorWaveBars
        {
            get
            {
                return m_ColorWaveBars;
            }
            set
            {
                if (m_ColorWaveBars == value) return;
                m_ColorWaveBars = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("ColorWaveBars");
            }
        }
        private Color m_ColorEnvelopeFill = Colors.ForestGreen;
        public Color ColorEnvelopeFill
        {
            get
            {
                return m_ColorEnvelopeFill;
            }
            set
            {
                if (m_ColorEnvelopeFill == value) return;
                m_ColorEnvelopeFill = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("ColorEnvelopeFill");
            }
        }
        private Color m_ColorEnvelopeOutline = Colors.LawnGreen;
        public Color ColorEnvelopeOutline
        {
            get
            {
                return m_ColorEnvelopeOutline;
            }
            set
            {
                if (m_ColorEnvelopeOutline == value) return;
                m_ColorEnvelopeOutline = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                OnPropertyChanged("ColorEnvelopeOutline");
            }
        }
        private void loadWaveForm(bool play)
        {
            if (m_pcmFormat == null)
            {
                return;
            }
            Brush brush1 = new SolidColorBrush(m_ColorPlayhead);
            WaveFormPlayHeadPath.Stroke = brush1;
            Brush brush2 = new SolidColorBrush(m_ColorPlayheadFill);
            WaveFormPlayHeadPath.Fill = brush2;
            Brush brush3 = new SolidColorBrush(m_ColorMarkers);
            WaveFormTimeRangePath.Fill = brush3;
            WaveFormTimeRangePath.Stroke = brush3;

            double stepX = WaveStepX;

            bool bUseDecibels = IsUseDecibels;
            bool bUseDecibelsNoAverage = IsUseDecibelsNoAverage;
            bool bUseDecibelsAdjust = IsUseDecibelsAdjust;
            //bool bUseDecibelsIntensity = IsUseDecibelsIntensity;

            bool bShowBackground = IsBackgroundVisible;
            bool bShowEnvelope = IsEnvelopeVisible;
            bool bFillEnvelope = IsEnvelopeFilled;
            bool bShowSamples = IsWaveFillVisible;
            //bool bAdjustOffsetFix = IsAdjustOffsetFix;

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

            double prevY1 = -1;
            double prevY2 = -1;
            double prevY1_ = -1;
            double prevY2_ = -1;

            double height = WaveFormCanvas.ActualHeight;
            if (height == Double.NaN || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            double width = WaveFormCanvas.ActualWidth;
            if (width == Double.NaN || width == 0)
            {
                width = WaveFormCanvas.Width;
            }

            m_bytesPerPixel = m_dataLength / width;

            int byteDepth = m_pcmFormat.BitDepth / 8; //bytes per sample (data for one channel only)

            int samplesPerStep = (int)Math.Floor((m_bytesPerPixel * stepX) / byteDepth);
            samplesPerStep += (samplesPerStep % m_pcmFormat.NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;

            byte[] bytes = new byte[bytesPerStep]; // Int 8 unsigned
            short[] samples = new short[samplesPerStep]; // Int 16 signed

            List<Point> listTopPointsCh1 = new List<Point>();
            List<Point> listTopPointsCh2 = new List<Point>();
            List<Point> listBottomPointsCh1 = new List<Point>();
            List<Point> listBottomPointsCh2 = new List<Point>();

            int read = 0;
            double x = 0.5;
            bool bJoinInterSamples = false;

            int tolerance = 5;
            try
            {
                if (FilePath.Length > 0)
                {
                    m_PlayStream.Position = m_StreamRiffHeaderEndPos;
                    m_PlayStream.Seek(m_StreamRiffHeaderEndPos, SeekOrigin.Begin);
                }
                double dBMinReached = double.PositiveInfinity;
                double dBMaxReached = double.NegativeInfinity;
                double dBMinHardCoded = 0.0;
                double dBMin_pixelsPerDb = 0.0;
                double decibelDrawDelta = (bUseDecibelsNoAverage ? 0 : 2);

                    //Amplitude ratio (or Sound Pressure Level):
                    //decibels = 20 * log10(ratio);

                    //Power ratio (or Sound Intensity Level):
                    //decibels = 10 * log10(ratio);

                    //10 * log(ratio^2) is exactly the same as 20 * log(ratio).

                    bool bUseDecibelsIntensity = false; // feature removed: no visible changes
                    double logFactor = (bUseDecibelsIntensity ? 10 : 20);

                    double reference = short.MaxValue; // Int 16 signed 32767 (0 dB reference value)
                    double adjustFactor = DecibelResolution;
                    if (adjustFactor != 0)
                    {
                        reference *= adjustFactor;
                        //0.707 adjustment to more realistic noise floor value, to avoid clipping (otherwise, use MinValue = -45 or -60 directly)
                    }

                    double dbMinValue = logFactor * Math.Log10(1.0 / reference); //-90.3 dB
                    //double val = reference*Math.Pow(10, MinValue/20); // val == 1.0, just checking

                    System.Diagnostics.Debug.Print(dbMinValue + "");

                    dBMinHardCoded = dbMinValue;

                    double dbMaxValue = (bUseDecibelsNoAverage ? -dbMinValue : 0);
                

                while ((read = m_PlayStream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    // converts Int 8 unsigned to Int 16 signed
                    Buffer.BlockCopy(bytes, 0, samples, 0, Math.Min(read, samples.Length));

                    for (int channel = 0; channel < m_pcmFormat.NumberOfChannels; channel++)
                    {
                        int limit = samples.Length;

                        if (read < bytes.Length)
                        {
                            int nSamples = (int)Math.Floor((double)read / byteDepth);
                            nSamples = m_pcmFormat.NumberOfChannels *
                                       (int)Math.Floor((double)nSamples / m_pcmFormat.NumberOfChannels);
                            limit = nSamples;
                            limit = Math.Min(limit, samples.Length);
                        }

                        double total = 0;
                        int n = 0;

                        double min = short.MaxValue; // Int 16 signed 32767
                        double max = short.MinValue; // Int 16 signed -32768

                        for (int i = channel; i < limit; i += m_pcmFormat.NumberOfChannels)
                        {
                            n++;

                            short sample = samples[i];
                            if (sample == short.MinValue)
                            {
                                total += short.MaxValue+1;
                            }
                            else
                            {
                                total += Math.Abs(sample);
                            }
                            

                            if (samples[i] < min)
                            {
                                min = samples[i];
                            }
                            if (samples[i] > max)
                            {
                                max = samples[i];
                            }
                        }

                        double avg = total / n;

                        double hh = height;
                        if (m_pcmFormat.NumberOfChannels > 1)
                        {
                            hh /= 2;
                        }

                        double y1 = 0.0;
                        double y2 = 0.0;

                        if (bUseDecibels)
                        {

                            if (!bUseDecibelsNoAverage)
                            {
                                min = avg;
                                max = avg;
                            }

                            bool minIsNegative = min < 0;
                            double minAbs = Math.Abs(min);
                            if (minAbs == 0)
                            {
                                min = (bUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                            }
                            else
                            {
                                min = logFactor * Math.Log10(minAbs / reference);
                                dBMinReached = Math.Min(dBMinReached, min);
                                if (bUseDecibelsNoAverage && !minIsNegative)
                                {
                                    min = -min;
                                }
                            }

                            bool maxIsNegative = max < 0;
                            double maxAbs = Math.Abs(max);
                            if (maxAbs == 0)
                            {
                                max = (bUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                            }
                            else
                            {
                                max = logFactor * Math.Log10(maxAbs / reference);
                                dBMaxReached = Math.Max(dBMaxReached, max);
                                if (bUseDecibelsNoAverage && !maxIsNegative)
                                {
                                    max = -max;
                                }
                            }

                            double totalDbRange = dbMaxValue - dbMinValue;
                            double pixPerDbUnit = hh / totalDbRange;
                            dBMin_pixelsPerDb = pixPerDbUnit;
                            if (bUseDecibelsNoAverage)
                            {
                                min = dbMinValue - min;
                            }
                            y1 = pixPerDbUnit * (min - dbMinValue) + decibelDrawDelta;
                            if (!bUseDecibelsNoAverage)
                            {
                                y1 = hh - y1;
                            }
                            if (bUseDecibelsNoAverage)
                            {
                                max = dbMaxValue - max;
                            }
                            y2 = pixPerDbUnit * (max - dbMinValue) - decibelDrawDelta;
                            if (!bUseDecibelsNoAverage)
                            {
                                y2 = hh - y2;
                            }
                        }
                        else
                        {
                            double MaxValue = short.MaxValue; // Int 16 signed 32767
                            double MinValue = short.MinValue; // Int 16 signed -32768

                            double pixPerUnit = hh /
                                                (MaxValue - MinValue); // == ushort.MaxValue => Int 16 unsigned 65535

                            y1 = pixPerUnit * (min - MinValue);
                            y1 = hh - y1;
                            y2 = pixPerUnit * (max - MinValue);
                            y2 = hh - y2;
                        }

                        if (!(bUseDecibels && bUseDecibelsAdjust))
                        {
                            if (y1 > hh - tolerance)
                            {
                                y1 = hh - tolerance;
                            }
                            if (y1 < 0 + tolerance)
                            {
                                y1 = 0 + tolerance;
                            }

                            if (y2 > hh - tolerance)
                            {
                                y2 = hh - tolerance;
                            }
                            if (y2 < 0 + tolerance)
                            {
                                y2 = 0 + tolerance;
                            }
                        }

                        if (channel == 0)
                        {
                            listTopPointsCh1.Add(new Point(x, y1));

                            if (prevY1 == -1)
                            {
                                sgcCh1.BeginFigure(new Point(x, y1), false, false);
                            }
                            else
                            {
                                sgcCh1.LineTo(new Point(x, y1), bJoinInterSamples, false);
                            }
                            prevY1 = y1;
                        }
                        else
                        {
                            y1 += hh;

                            listTopPointsCh2.Add(new Point(x, y1));

                            if (prevY1_ == -1)
                            {
                                sgcCh2.BeginFigure(new Point(x, y1), false, false);
                            }
                            else
                            {
                                sgcCh2.LineTo(new Point(x, y1), bJoinInterSamples, false);

                                prevY1_ += hh;
                            }
                            prevY1_ = y1 - hh;
                        }

                        if (channel == 0)
                        {
                            sgcCh1.LineTo(new Point(x, y2), true, false);

                            listBottomPointsCh1.Add(new Point(x, y2));

                            if (prevY2 == -1)
                            {
                                //sgcCh1_envelope.BeginFigure(new Point(x, y2), false, false);
                            }
                            else
                            {
                                //
                            }
                            prevY2 = y2;
                        }
                        else
                        {
                            y2 += hh;
                            sgcCh2.LineTo(new Point(x, y2), true, false);

                            listBottomPointsCh2.Add(new Point(x, y2));

                            if (prevY2_ == -1)
                            {
                                //sgcCh2_envelope.BeginFigure(new Point(x, y2), false, false);
                            }
                            else
                            {
                                prevY2_ += hh;
                                //
                            }
                            prevY2_ = y2 - hh;
                        }
                    }

                    x += (read / m_bytesPerPixel); //stepX;
                    if (x > width)
                    {
                        break;
                    }
                }

                int bottomIndexStartCh1 = listTopPointsCh1.Count;
                int bottomIndexStartCh2 = listTopPointsCh2.Count;

                if (!bUseDecibels || (bUseDecibels && bUseDecibelsNoAverage))
                {
                    listBottomPointsCh1.Reverse();
                    listTopPointsCh1.AddRange(listBottomPointsCh1);
                    listBottomPointsCh1.Clear();

                    if (m_pcmFormat.NumberOfChannels > 1)
                    {
                        listBottomPointsCh2.Reverse();
                        listTopPointsCh2.AddRange(listBottomPointsCh2);
                        listBottomPointsCh2.Clear();
                    }
                }

                if (bUseDecibels && bUseDecibelsAdjust &&
                    (dBMinHardCoded != dBMinReached ||
                    (IsUseDecibelsNoAverage && (-dBMinHardCoded) != dBMaxReached)))
                {
                    List<Point> listNewCh1 = new List<Point>(listTopPointsCh1.Count);
                    List<Point> listNewCh2 = new List<Point>(listTopPointsCh2.Count);

                    double hh = height;
                    if (m_pcmFormat.NumberOfChannels > 1)
                    {
                        hh /= 2;
                    }

                    double range = ((IsUseDecibelsNoAverage ? -dBMinHardCoded : 0) - dBMinHardCoded);
                    double pixPerDbUnit = hh / range;

                    int index = -1;

                    Point p2 = new Point();
                    foreach (Point p in listTopPointsCh1)
                    {
                        index++;

                        p2.X = p.X;
                        p2.Y = p.Y;

                        /*
                         if (bUseDecibelsNoAverage)
                         * 
                            YY = pixPerDbUnit * (MaxValue - DB - MinValue) - decibelDrawDelta [+HH]
                         * 
                         * 
                           DB = (-YY - decibelDrawDelta)/pixPerDbUnit + MaxValue - MinValue
                           
                         */


                        /*if (!bUseDecibelsNoAverage)
                         * 
                            YY = hh - (pixPerDbUnit * (DB - MinValue) - decibelDrawDelta) [+HH]
                         * 
                         * 
                            DB = ( hh + decibelDrawDelta- YY)/pixPerDbUnit + MinValue
                            
                         */


                        double newRange = ((IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                        double pixPerDbUnit_new = hh / newRange;

                        double dB = 0.0;
                        if (IsUseDecibelsNoAverage)
                        {
                            if (index >= bottomIndexStartCh1)
                            {
                                dB = (-p.Y - decibelDrawDelta) / pixPerDbUnit - dBMinHardCoded - dBMinHardCoded;
                                p2.Y = pixPerDbUnit_new * (dBMaxReached - dB - dBMinReached) - decibelDrawDelta;
                            }
                            else
                            {
                                dB = (-p.Y - decibelDrawDelta) / pixPerDbUnit + dBMinHardCoded - dBMinHardCoded;
                                p2.Y = pixPerDbUnit_new * (dBMinReached - dB - dBMinReached) - decibelDrawDelta;
                            }
                            //p2.Y = hh - p2.Y;
                        }
                        else
                        {
                            dB = (hh + decibelDrawDelta - p.Y) / pixPerDbUnit + dBMinHardCoded;
                            p2.Y = hh - (pixPerDbUnit_new * (dB - dBMinReached) - decibelDrawDelta);
                        }

                        listNewCh1.Add(p2);
                    }
                    listTopPointsCh1.Clear();
                    listTopPointsCh1.AddRange(listNewCh1);
                    listNewCh1.Clear();

                    if (m_pcmFormat.NumberOfChannels > 1)
                    {
                        index = -1;

                        foreach (Point p in listTopPointsCh2)
                        {
                            index++;

                            p2.X = p.X;
                            p2.Y = p.Y;

                            double newRange = ((IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                            double pixPerDbUnit_new = hh / newRange;

                            double dB = 0.0;
                            if (IsUseDecibelsNoAverage)
                            {
                                if (index >= bottomIndexStartCh1)
                                {
                                    dB = (hh + -p.Y - decibelDrawDelta) / pixPerDbUnit - dBMinHardCoded - dBMinHardCoded;
                                    p2.Y = hh + pixPerDbUnit_new * (dBMaxReached - dB - dBMinReached) - decibelDrawDelta;
                                }
                                else
                                {
                                    dB = (hh + -p.Y - decibelDrawDelta) / pixPerDbUnit + dBMinHardCoded - dBMinHardCoded;
                                    p2.Y = hh + pixPerDbUnit_new * (dBMinReached - dB - dBMinReached) - decibelDrawDelta;
                                }
                                //p2.Y = hh - p2.Y;
                            }
                            else
                            {
                                dB = (hh + hh + decibelDrawDelta - p.Y) / pixPerDbUnit + dBMinHardCoded;
                                p2.Y = hh + hh - (pixPerDbUnit_new * (dB - dBMinReached) - decibelDrawDelta);
                            }

                            listNewCh2.Add(p2);
                        }
                        listTopPointsCh2.Clear();
                        listTopPointsCh2.AddRange(listNewCh2);
                        listNewCh2.Clear();
                    }
                }

                int count = 0;
                Point pp = new Point();
                foreach (Point p in listTopPointsCh1)
                {
                    pp.X = p.X;
                    pp.Y = p.Y;

                    if (pp.X > width)
                    {
                        pp.X = width;
                    }
                    if (pp.X < 0)
                    {
                        pp.X = 0;
                    }
                    if (pp.Y > height - tolerance)
                    {
                        pp.Y = height - tolerance;
                    }
                    if (pp.Y < 0 + tolerance)
                    {
                        pp.Y = 0 + tolerance;
                    }
                    if (count == 0)
                    {
                        sgcCh1_envelope.BeginFigure(pp, bFillEnvelope && (!bUseDecibels || bUseDecibelsNoAverage), false);
                    }
                    else
                    {
                        sgcCh1_envelope.LineTo(pp, true, false);
                    }
                    count++;
                }
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    count = 0;

                    foreach (Point p in listTopPointsCh2)
                    {
                        pp.X = p.X;
                        pp.Y = p.Y;

                        if (pp.X > width)
                        {
                            pp.X = width;
                        }
                        if (pp.X < 0)
                        {
                            pp.X = 0;
                        }
                        if (pp.Y > height - tolerance)
                        {
                            pp.Y = height - tolerance;
                        }
                        if (pp.Y < 0 + tolerance)
                        {
                            pp.Y = 0 + tolerance;
                        }
                        if (count == 0)
                        {
                            sgcCh2_envelope.BeginFigure(pp, bFillEnvelope && (!bUseDecibels || bUseDecibelsNoAverage), false);
                        }
                        else
                        {
                            sgcCh2_envelope.LineTo(pp, true, false);
                        }
                        count++;
                    }
                }

                sgcCh1.Close();
                sgcCh1_envelope.Close();
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    sgcCh2.Close();
                    sgcCh2_envelope.Close();
                }

                Brush brushColorBars = new SolidColorBrush(ColorWaveBars);
                Brush brushColorEnvelopeOutline = new SolidColorBrush(ColorEnvelopeOutline);
                Brush brushColorEnvelopeFill = new SolidColorBrush(ColorEnvelopeFill);

                //
                geometryCh1.Freeze();
                GeometryDrawing geoDraw1 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh1);
                geoDraw1.Freeze();
                //
                geometryCh1_envelope.Freeze();
                GeometryDrawing geoDraw1_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh1_envelope);
                geoDraw1_envelope.Freeze();
                //
                GeometryDrawing geoDraw2 = null;
                GeometryDrawing geoDraw2_envelope = null;
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    geometryCh2.Freeze();
                    geoDraw2 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh2);
                    geoDraw2.Freeze();
                    geometryCh2_envelope.Freeze();
                    geoDraw2_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh2_envelope);
                    geoDraw2_envelope.Freeze();
                }
                //

                Brush brushColorMarkers = new SolidColorBrush(ColorMarkers);
                GeometryDrawing geoDrawMarkers = null;
                if (m_PlayStreamMarkers != null)
                {
                    StreamGeometry geometryMarkers = new StreamGeometry();
                    using (StreamGeometryContext sgcMarkers = geometryMarkers.Open())
                    {
                        sgcMarkers.BeginFigure(new Point(0.5, 0), false, false);
                        sgcMarkers.LineTo(new Point(0.5, height), true, false);

                        long sumData = 0;
                        foreach (TreeNodeAndStreamDataLength markers in m_PlayStreamMarkers)
                        {
                            double pixels = (sumData + markers.m_LocalStreamDataLength) / m_bytesPerPixel;

                            sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
                            sgcMarkers.LineTo(new Point(pixels, height), true, false);

                            sumData += markers.m_LocalStreamDataLength;
                        }
                        sgcMarkers.Close();
                    }

                    geometryMarkers.Freeze();
                    geoDrawMarkers = new GeometryDrawing(brushColorMarkers,
                                                                         new Pen(brushColorMarkers, 1.0),
                                                                         geometryMarkers);
                    geoDrawMarkers.Freeze();
                }
                //
                StreamGeometry geometryBack = new StreamGeometry();
                using (StreamGeometryContext sgcBack = geometryBack.Open())
                {
                    sgcBack.BeginFigure(new Point(0, 0), true, true);
                    sgcBack.LineTo(new Point(0, height), false, false);
                    sgcBack.LineTo(new Point(width, height), false, false);
                    sgcBack.LineTo(new Point(width, 0), false, false);
                    sgcBack.Close();
                }
                geometryBack.Freeze();
                Brush brushColorBack = new SolidColorBrush(ColorWaveBackground);
                GeometryDrawing geoDrawBack = new GeometryDrawing(brushColorBack, null, geometryBack); //new Pen(brushColorBack, 1.0)
                geoDrawBack.Freeze();
                //
                DrawingGroup drawGrp = new DrawingGroup();
                //

                if (bShowBackground)
                {
                    drawGrp.Children.Add(geoDrawBack);
                    if (drawGrp.Bounds.Top != 0 || drawGrp.Bounds.Left != 0)
                    {
                        int debug = 1;
                    }
                    if (drawGrp.Bounds.Width != width || drawGrp.Bounds.Height != height)
                    {
                        int debug = 1;
                    }
                }
                if (bShowEnvelope)
                {
                    if (bFillEnvelope)
                    {
                        drawGrp.Children.Add(geoDraw1_envelope);
                        if (bShowSamples)
                        {
                            drawGrp.Children.Add(geoDraw1);
                        }
                    }
                    else
                    {
                        if (bShowSamples)
                        {
                            drawGrp.Children.Add(geoDraw1);
                        }
                        drawGrp.Children.Add(geoDraw1_envelope);
                    }

                    if (drawGrp.Bounds.Top != 0 || drawGrp.Bounds.Left != 0)
                    {
                        int debug = 1;
                    }
                    if (drawGrp.Bounds.Width != width || drawGrp.Bounds.Height != height)
                    {
                        int debug = 1;
                    }
                }
                else if (bShowSamples)
                {
                    drawGrp.Children.Add(geoDraw1);


                    if (drawGrp.Bounds.Top != 0 || drawGrp.Bounds.Left != 0)
                    {
                        int debug = 1;
                    }
                    if (drawGrp.Bounds.Width != width || drawGrp.Bounds.Height != height)
                    {
                        int debug = 1;
                    }
                }
                if (m_pcmFormat.NumberOfChannels > 1)
                {
                    if (bShowEnvelope)
                    {
                        if (bFillEnvelope)
                        {
                            drawGrp.Children.Add(geoDraw2_envelope);
                            if (bShowSamples)
                            {
                                drawGrp.Children.Add(geoDraw2);
                            }
                        }
                        else
                        {
                            if (bShowSamples)
                            {
                                drawGrp.Children.Add(geoDraw2);
                            }
                            drawGrp.Children.Add(geoDraw2_envelope);
                        }
                    }
                    else if (bShowSamples)
                    {
                        drawGrp.Children.Add(geoDraw2);
                    }
                }
                if (m_PlayStreamMarkers != null)
                {
                    drawGrp.Children.Add(geoDrawMarkers);
                }

                if (drawGrp.Bounds.Top != 0 || drawGrp.Bounds.Left != 0)
                {
                    int debug = 1;
                }
                if (drawGrp.Bounds.Width != width || drawGrp.Bounds.Height != height)
                {
                    int debug = 1;
                }
                double m_offsetFixX = 0;
                m_offsetFixX = drawGrp.Bounds.Width - width;

                double m_offsetFixY = 0;
                m_offsetFixY = drawGrp.Bounds.Height - height;

                /*
                if (bAdjustOffsetFix && (m_offsetFixX != 0 || m_offsetFixY != 0))
                {
                    TransformGroup trGrp = new TransformGroup();
                    //trGrp.Children.Add(new TranslateTransform(-drawGrp.Bounds.Left, -drawGrp.Bounds.Top));
                    trGrp.Children.Add(new ScaleTransform(width / drawGrp.Bounds.Width, height / drawGrp.Bounds.Height));
                    drawGrp.Transform = trGrp;
                }*/

                drawGrp.Freeze();


                DrawingImage drawImg = new DrawingImage(drawGrp);

                if (drawImg.Height > height)
                {
                    //drawImg.Height = WaveFormCanvas.ActualHeight;
                    int debug = 1;
                }

                if (drawImg.Width > width)
                {
                    //drawImg.Width = WaveFormCanvas.ActualWidth;
                    int debug = 1;
                }
                drawImg.Freeze();

                RenderOptions.SetBitmapScalingMode(WaveFormImage, BitmapScalingMode.LowQuality);
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

            if (play)
            {
                TimeDelta dur = m_pcmFormat.GetDuration(m_dataLength);
                m_Player.Play(mCurrentAudioStreamProvider,
                              dur,
                              m_pcmFormat);
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

            double height = WaveFormCanvas.ActualHeight;
            if (height == Double.NaN || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            double width = WaveFormCanvas.ActualWidth;
            if (width == Double.NaN || width == 0)
            {
                width = WaveFormCanvas.Width;
            }

            m_bytesPerPixel = m_dataLength / width;

            m_CurrentSubTreeNode = null;
            updateWaveFormPlayHead();

            if (m_pcmFormat == null) //!e.WidthChanged || 
            {
                return;
            }
            startWaveFormLoadTimer(500, false);
        }

        private bool m_forcePlayAfterWaveFormLoaded = false;

        private void startWaveFormLoadTimer(double delay, bool play)
        {
            if (m_pcmFormat == null)
            {
                return;
            }

            m_forcePlayAfterWaveFormLoaded = play;

            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            }

            if (m_WaveFormLoadTimer == null)
            {
                m_WaveFormLoadTimer = new DispatcherTimer(DispatcherPriority.Background);
                m_WaveFormLoadTimer.Tick += OnWaveFormLoadTimerTick;
                if (delay == 0)
                {
                    m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(0);//??
                }
                else
                {
                    m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(delay);
                }
            }
            else if (m_WaveFormLoadTimer.IsEnabled)
            {
                m_WaveFormLoadTimer.Stop();
            }

            m_WaveFormLoadTimer.Start();
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
                m_Player.Stop();
            }

            if (m_Player.State == AudioPlayerState.Stopped)
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
