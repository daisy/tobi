using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using urakawa.media.timing;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// Interaction logic for the AudioPaneView.xaml view.
    /// The backing ViewModel is injected in the constructor ("passive" view design pattern)
    /// </summary>
    public partial class AudioPaneView : IAudioPaneView
    {
        #region Construction

        protected AudioPaneViewModel ViewModel { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public AudioPaneView(AudioPaneViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            ViewModel.SetView(this);

            WinFormHost.Child = ViewModel.GetWindowsFormsHookControl();
        }

        #endregion Construction

        #region Private Class Attributes

        private const int m_ArrowDepth = 6;

        private WaveFormLoadingAdorner m_WaveFormLoadingAdorner;

        #endregion Private Class Attributes

        #region Event / Callbacks

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            ViewModel.AudioPlayer_TogglePlayPause();
        }
        private void OnPause(object sender, RoutedEventArgs e)
        {
            ViewModel.AudioPlayer_TogglePlayPause();
        }
        private void OnStop(object sender, RoutedEventArgs e)
        {
            ViewModel.AudioPlayer_Stop();
        }

        private void OnRecord(object sender, RoutedEventArgs e)
        {

        }
        private void OnSwitchPhrasePrevious(object sender, RoutedEventArgs e)
        {

        }
        private void OnSwitchPhraseNext(object sender, RoutedEventArgs e)
        {

        }
        private void OnGotoBegining(object sender, RoutedEventArgs e)
        {

        }
        private void OnGotoEnd(object sender, RoutedEventArgs e)
        {

        }
        private void OnStepBack(object sender, RoutedEventArgs e)
        {

        }

        private void OnStepForward(object sender, RoutedEventArgs e)
        {

        }
        private void OnRewind(object sender, RoutedEventArgs e)
        {

        }
        private void OnFastForward(object sender, RoutedEventArgs e)
        {

        }

        private void clearSelection()
        {
            m_TimeSelectionLeftX = -1;
            WaveFormTimeSelectionRect.Visibility = Visibility.Hidden;
            WaveFormTimeSelectionRect.Width = 0;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
        }

        private void OnClearSelection(object sender, RoutedEventArgs e)
        {
            clearSelection();
        }

        private void OnZoomSelection(object sender, RoutedEventArgs e)
        {
            if (m_TimeSelectionLeftX == -1)
            {
                return;
            }

            double widthToUse = WaveFormScroll.ActualWidth;
            if (widthToUse == Double.NaN || widthToUse == 0)
            {
                widthToUse = WaveFormScroll.Width;
            }
            double widthSelection = WaveFormTimeSelectionRect.Width;

            double ratio = widthToUse / widthSelection;

            double newSliderValue = ZoomSlider.Value * ratio;

            if (newSliderValue > 20000)
            {
                return; //safeguard...image too large
            }

            if (newSliderValue < ZoomSlider.Minimum)
            {
                ZoomSlider.Minimum = newSliderValue;
            }
            if (newSliderValue > ZoomSlider.Maximum)
            {
                ZoomSlider.Maximum = newSliderValue;
            }
            ZoomSlider.Value = newSliderValue;
        }
        private void OnZoomFitFull(object sender, RoutedEventArgs e)
        {
            double widthToUse = WaveFormScroll.ActualWidth;
            if (widthToUse == Double.NaN || widthToUse == 0)
            {
                widthToUse = WaveFormScroll.Width;
            }
            if (widthToUse < ZoomSlider.Minimum)
            {
                ZoomSlider.Minimum = widthToUse;
            }
            if (widthToUse > ZoomSlider.Maximum)
            {
                ZoomSlider.Maximum = widthToUse;
            }
            ZoomSlider.Value = widthToUse;
        }
        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            ViewModel.AudioPlayer_TogglePlayPause();

            var dlg = new OpenFileDialog
                          {
                              FileName = "audio",
                              DefaultExt = ".wav",
                              Filter = "WAV files (.wav)|*.wav;*.aiff"
                          };
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }

            ViewModel.AudioPlayer_Stop();

            ViewModel.AudioPlayer_LoadAndPlayFromFile(dlg.FileName);
        }

        private void OnPeakMeterCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.UpdatePeakMeter();
        }

        private void OnWaveFormCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            /*
            PresentationSource ps = PresentationSource.FromVisual(this);
            if (ps != null)
            {
                Matrix m = ps.CompositionTarget.TransformToDevice;
                double dpiFactor = 1 / m.M11;
                WaveFormPlayHeadPath.StrokeThickness = 1 * dpiFactor; // 1px
                WaveFormTimeRangePath.StrokeThickness = 1 * dpiFactor;
            }
             */

            double width = WaveFormCanvas.ActualWidth;
            if (width == Double.NaN || width == 0)
            {
                width = WaveFormCanvas.Width;
            }

            BytesPerPixel = ViewModel.AudioPlayer_GetDataLength() / width;

            ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
            if (ViewModel.AudioPlayer_GetPcmFormat() == null)
            {
                return;
            }
            StartWaveFormLoadTimer(500, false);
        }

        private double m_TimeSelectionLeftX = -1;
        private readonly Cursor m_WaveFormDefaultCursor = Cursors.ArrowCD;

        private void OnWaveFormMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;
                return;
            }

            Point p = e.GetPosition(WaveFormCanvas);

            if (p.X == m_TimeSelectionLeftX)
            {
                WaveFormTimeSelectionRect.Visibility = Visibility.Hidden;
                WaveFormTimeSelectionRect.Width = 0;

                return;
            }

            double right = p.X;
            double left = m_TimeSelectionLeftX;

            if (p.X < m_TimeSelectionLeftX)
            {
                right = m_TimeSelectionLeftX;
                left = p.X;
            }

            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = right - left;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, left);

            WaveFormCanvas.Cursor = Cursors.SizeWE;
        }

        private void OnWaveFormMouseUp(object sender, MouseButtonEventArgs e)
        {
            WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;

            Point p = e.GetPosition(WaveFormCanvas);

            if (p.X == m_TimeSelectionLeftX)
            {
                m_TimeSelectionLeftX = -1;
                WaveFormTimeSelectionRect.Visibility = Visibility.Hidden;
                WaveFormTimeSelectionRect.Width = 0;
                WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);

                ViewModel.AudioPlayer_PlayFrom(p.X * BytesPerPixel);

                return;
            }

            double right = p.X;

            if (p.X < m_TimeSelectionLeftX)
            {
                right = m_TimeSelectionLeftX;
                m_TimeSelectionLeftX = p.X;
            }

            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = right - m_TimeSelectionLeftX;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);

            ViewModel.AudioPlayer_PlayFromTo(m_TimeSelectionLeftX * BytesPerPixel, right * BytesPerPixel);
        }

        private void OnWaveFormMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(WaveFormCanvas);

            m_TimeSelectionLeftX = p.X;

            WaveFormTimeSelectionRect.Visibility = Visibility.Hidden;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
            WaveFormTimeSelectionRect.Width = 0;

            WaveFormCanvas.Cursor = Cursors.SizeWE;
        }

        private void OnResetPeakOverloadCountCh1(object sender, MouseButtonEventArgs e)
        {
            ViewModel.PeakOverloadCountCh1 = 0;
        }

        private void OnResetPeakOverloadCountCh2(object sender, MouseButtonEventArgs e)
        {
            ViewModel.PeakOverloadCountCh2 = 0;
        }

        /// <summary>
        /// Init the adorner layer by adding the "Loading" message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        #endregion Event / Callbacks

        public double BytesPerPixel { get; set; }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_WaveFormBackground()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormBackground));
                return;
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

            var drawImg = new DrawingImage();
            var geometry = new StreamGeometry();
            using (StreamGeometryContext sgc = geometry.Open())
            {
                sgc.BeginFigure(new Point(0, 0), true, true);
                sgc.LineTo(new Point(0, height), true, false);
                sgc.LineTo(new Point(width, height), true, false);
                sgc.LineTo(new Point(width, 0), true, false);
                sgc.Close();
            }

            geometry.Freeze();

            Brush brushColorBack = new SolidColorBrush(ViewModel.ColorWaveBackground);
            var geoDraw = new GeometryDrawing(brushColorBack, new Pen(brushColorBack, 1.0), geometry);
            geoDraw.Freeze();
            var drawGrp = new DrawingGroup();
            drawGrp.Children.Add(geoDraw);
            drawGrp.Freeze();
            drawImg.Drawing = drawGrp;
            drawImg.Freeze();
            WaveFormImage.Source = drawImg;
        }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_AllReset()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_AllReset));
                return;
            }

            WaveFormTimeSelectionRect.Visibility = Visibility.Hidden;

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

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_WaveFormPlayHead()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(RefreshUI_WaveFormPlayHead));
                return;
            }

            long bytes = ViewModel.AudioPlayer_GetPcmFormat().GetByteForTime(new Time(ViewModel.LastPlayHeadTime));
            double pixels = bytes / BytesPerPixel;

            StreamGeometry geometry;
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

            using (StreamGeometryContext sgc = geometry.Open())
            {
                sgc.BeginFigure(new Point(pixels, height - m_ArrowDepth), true, false);
                sgc.LineTo(new Point(pixels + m_ArrowDepth, height), true, false);
                sgc.LineTo(new Point(pixels - m_ArrowDepth, height), true, false);
                sgc.LineTo(new Point(pixels, height - m_ArrowDepth), true, false);
                sgc.LineTo(new Point(pixels, m_ArrowDepth), true, false);
                sgc.LineTo(new Point(pixels - m_ArrowDepth, 0), true, false);
                sgc.LineTo(new Point(pixels + m_ArrowDepth, 0), true, false);
                sgc.LineTo(new Point(pixels, m_ArrowDepth), true, false);

                sgc.Close();
            }

            if (WaveFormPlayHeadPath.Data == null)
            {
                WaveFormPlayHeadPath.Data = geometry;
            }

            WaveFormPlayHeadPath.InvalidateVisual();

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
        }

// ReSharper disable RedundantDefaultFieldInitializer
        private long m_WaveFormChunkMarkersLeftBytes = 0;
        private long m_WaveFormChunkMarkersRightBytes = 0;
// ReSharper restore RedundantDefaultFieldInitializer
        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        // ReSharper disable InconsistentNaming
        private void RefreshUI_WaveFormChunkMarkers()
        // ReSharper restore InconsistentNaming
        {
            double height = WaveFormCanvas.ActualHeight;
            if (height == Double.NaN || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            double pixelsLeft = m_WaveFormChunkMarkersLeftBytes / BytesPerPixel;
            double pixelsRight = m_WaveFormChunkMarkersRightBytes / BytesPerPixel;

            StreamGeometry geometryRange;
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
                sgc.BeginFigure(new Point(pixelsLeft, height - m_ArrowDepth), true, false);
                sgc.LineTo(new Point(pixelsRight, height - m_ArrowDepth), false, false);
                sgc.LineTo(new Point(pixelsRight, height), false, false);
                sgc.LineTo(new Point(pixelsLeft, height), false, false);
                sgc.LineTo(new Point(pixelsLeft, 0), false, false);
                sgc.LineTo(new Point(pixelsRight, 0), false, false);
                sgc.LineTo(new Point(pixelsRight, m_ArrowDepth), false, false);
                sgc.LineTo(new Point(pixelsLeft, m_ArrowDepth), false, false);
                sgc.LineTo(new Point(pixelsLeft, 0), false, false);

                sgc.Close();
            }

            if (WaveFormTimeRangePath.Data == null)
            {
                WaveFormTimeRangePath.Data = geometryRange;
            }

            WaveFormTimeRangePath.InvalidateVisual();
        }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        /// <param name="bytesLeft"></param>
        /// <param name="bytesRight"></param>
        public void RefreshUI_WaveFormChunkMarkers(long bytesLeft, long bytesRight)
        {
            m_WaveFormChunkMarkersLeftBytes = bytesLeft;
            m_WaveFormChunkMarkersRightBytes = bytesRight;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                return;
            }

            RefreshUI_WaveFormChunkMarkers();
        }

        /// <summary>
        /// Refreshes the PeakMeterCanvas
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_PeakMeter()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_PeakMeter));
                return;
            }

            double barWidth = PeakMeterCanvas.ActualWidth;
            if (ViewModel.AudioPlayer_GetPcmFormat().NumberOfChannels > 1)
            {
                barWidth = barWidth / 2;
            }
            double availableHeight = PeakMeterCanvas.ActualHeight;

            StreamGeometry geometry1;
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
                double pixels = ViewModel.PeakMeterBarDataCh1.DbToPixels(availableHeight);

                sgc.BeginFigure(new Point(0, 0), true, true);
                sgc.LineTo(new Point(barWidth, 0), false, false);
                sgc.LineTo(new Point(barWidth, availableHeight - pixels), false, false);
                sgc.LineTo(new Point(0, availableHeight - pixels), false, false);

                sgc.Close();
            }

            StreamGeometry geometry2 = null;
            if (ViewModel.AudioPlayer_GetPcmFormat().NumberOfChannels > 1)
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
                    double pixels = ViewModel.PeakMeterBarDataCh2.DbToPixels(availableHeight);

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
            if (ViewModel.AudioPlayer_GetPcmFormat().NumberOfChannels > 1)
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

            //PeakMeterCanvas.InvalidateVisual();

        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        private void refreshUI_PeakMeterBlackoutOn()
        {
            PeakMeterCanvasOpaqueMask.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        private void refreshUI_PeakMeterBlackoutOff()
        {
            PeakMeterCanvasOpaqueMask.Visibility = Visibility.Hidden;

            if (PeakMeterPathCh1.Data != null)
            {
                ((StreamGeometry)PeakMeterPathCh1.Data).Clear();
            }
            if (PeakMeterPathCh2.Data != null)
            {
                ((StreamGeometry)PeakMeterPathCh2.Data).Clear();
            }
        }
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        /// <param name="black"></param>
        public void RefreshUI_PeakMeterBlackout(bool black)
        {
            if (Dispatcher.CheckAccess())
            {
                if (black)
                {
                    refreshUI_PeakMeterBlackoutOn();
                }
                else
                {
                    refreshUI_PeakMeterBlackoutOff();
                }
            }
            else
            {
                if (black)
                {
                    Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_PeakMeterBlackoutOn));
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_PeakMeterBlackoutOff));
                }
            }
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        private void refreshUI_LoadingMessageVisible()
        {
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        private void refreshUI_LoadingMessageHidden()
        {
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;
            }
        }
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Shows or hides the loading message in the adorner layer
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        /// <param name="visible"></param>
        public void RefreshUI_LoadingMessage(bool visible)
        {
            if (Dispatcher.CheckAccess())
            {
                if (visible)
                {
                    refreshUI_LoadingMessageVisible();
                }
                else
                {
                    refreshUI_LoadingMessageHidden();
                }
            }
            else
            {
                if (visible)
                {
                    Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_LoadingMessageVisible));
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_LoadingMessageHidden));
                }
            }
        }

        #region DispatcherTimers

        private DispatcherTimer m_PlaybackTimer;

        public void StopWaveFormTimer()
        {
            if (m_PlaybackTimer != null && m_PlaybackTimer.IsEnabled)
            {
                m_PlaybackTimer.Stop();
            }
            m_PlaybackTimer = null;
        }

        public void StartWaveFormTimer()
        {
            if (m_PlaybackTimer == null)
            {
                m_PlaybackTimer = new DispatcherTimer(DispatcherPriority.Send);
                m_PlaybackTimer.Tick += OnPlaybackTimerTick;

// ReSharper disable RedundantAssignment
                double interval = 60;
// ReSharper restore RedundantAssignment

                interval = ViewModel.AudioPlayer_ConvertByteToMilliseconds(BytesPerPixel);

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

        private void OnPlaybackTimerTick(object sender, EventArgs e)
        {
            ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
        }

        private DispatcherTimer m_PeakMeterTimer;

        public void StopPeakMeterTimer()
        {
            if (m_PeakMeterTimer != null && m_PeakMeterTimer.IsEnabled)
            {
                m_PeakMeterTimer.Stop();
            }
            m_PeakMeterTimer = null;
        }

        public void StartPeakMeterTimer()
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

        private void OnPeakMeterTimerTick(object sender, EventArgs e)
        {
            ViewModel.UpdatePeakMeter();
        }


        private DispatcherTimer m_WaveFormLoadTimer;

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_ForcePlayAfterWaveFormLoaded = false;
        // ReSharper restore RedundantDefaultFieldInitializer

        public void StartWaveFormLoadTimer(double delay, bool play)
        {
            if (ViewModel.AudioPlayer_GetPcmFormat() == null)
            {
                return;
            }

            m_ForcePlayAfterWaveFormLoaded = play;

            RefreshUI_LoadingMessage(true);

            if (m_WaveFormLoadTimer == null)
            {
                m_WaveFormLoadTimer = new DispatcherTimer(DispatcherPriority.Background);
                m_WaveFormLoadTimer.Tick += OnWaveFormLoadTimerTick;
                // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                if (delay == 0)
                // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                {
                    m_WaveFormLoadTimer.Interval = TimeSpan.FromMilliseconds(0);//TODO: does this work ?? (immediate dispatch)
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

        private void OnWaveFormLoadTimerTick(object sender, EventArgs e)
        {
            RefreshUI_LoadingMessage(true);
            m_WaveFormLoadTimer.Stop();
            ViewModel.AudioPlayer_LoadWaveForm(m_ForcePlayAfterWaveFormLoaded);
        }
        #endregion DispatcherTimers
    }
}
