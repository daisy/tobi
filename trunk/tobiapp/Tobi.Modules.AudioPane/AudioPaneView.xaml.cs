using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.media.data.audio;

namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// Interaction logic for the AudioPaneView.xaml view.
    /// The backing ViewModel is injected in the constructor ("passive" view design pattern)
    /// </summary>
    public partial class AudioPaneView : IAudioPaneView
    {
        #region Construction

        public AudioPaneViewModel ViewModel { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public AudioPaneView(AudioPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.SetView(this);

            ViewModel.Logger.Log("AudioPaneView.ctor", Category.Debug, Priority.Medium);

            DataContext = ViewModel;

            InitializeComponent();

            WinFormHost.Child = ViewModel.GetWindowsFormsHookControl();

            ZoomSlider.SetValue(AutomationProperties.NameProperty, UserInterfaceStrings.Audio_ZoomSlider);
            //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);
        }

        public void InitGraphicalCommandBindings()
        {
            var mouseGest = new MouseGesture { MouseAction = MouseAction.LeftDoubleClick };

            var mouseBind = new MouseBinding { Gesture = mouseGest, Command = ViewModel.CommandSelectAll };

            WaveFormCanvas.InputBindings.Add(mouseBind);

            var mouseGest2 = new MouseGesture
            {
                MouseAction = MouseAction.LeftDoubleClick,
                Modifiers = ModifierKeys.Control
            };

            var mouseBind2 = new MouseBinding { Gesture = mouseGest2, Command = ViewModel.CommandClearSelection };

            WaveFormCanvas.InputBindings.Add(mouseBind2);
        }

        ~AudioPaneView()
        {
#if DEBUG
            ViewModel.Logger.Log("AudioPaneView garbage collected.", Category.Debug, Priority.Medium);
#endif
        }

        #endregion Construction

        #region Private Class Attributes

        private const double m_ArrowDepth = 6;

        private WaveFormLoadingAdorner m_WaveFormLoadingAdorner;
        private WaveFormTimeTicksAdorner m_WaveFormTimeTicksAdorner;

        private double m_WaveFormDragX = -1;
        private bool m_ControlKeyWasDownAtLastMouseMove = false;

        private readonly Cursor m_WaveFormDefaultCursor = Cursors.Arrow;
        private readonly Cursor m_WaveFormDragMoveCursor = Cursors.ScrollAll;
        private readonly Cursor m_WaveFormDragSelectCursor = Cursors.SizeWE;

        #endregion Private Class Attributes

        #region Event / Callbacks

        private void OnPeakMeterCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.UpdatePeakMeter();
        }

        private bool m_ZoomSliderDrag = false;

        private void OnZoomSliderDragStarted(object sender, DragStartedEventArgs e1)
        {
            ViewModel.Logger.Log("AudioPaneView.OnZoomSliderDragStarted", Category.Debug, Priority.Medium);
            m_ZoomSliderDrag = true;
        }

        private void OnZoomSliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            ViewModel.Logger.Log("AudioPaneView.OnZoomSliderDragCompleted", Category.Debug, Priority.Medium);
            m_ZoomSliderDrag = false;

            ViewModel.CommandRefresh.Execute();
        }

        private void OnWaveFormScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.InvalidateVisual();
            }
        }

        private void OnWaveFormCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double oldWidth = e.PreviousSize.Width;

            double width = WaveFormCanvas.ActualWidth;
            if (double.IsNaN(width) || width == 0)
            {
                width = WaveFormCanvas.Width;
            }

            if (m_TimeSelectionLeftX >= 0)
            {
                double ratio = width / oldWidth;
                m_TimeSelectionLeftX *= ratio;
                WaveFormTimeSelectionRect.Width *= ratio;
                WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
            }

            if (ViewModel.State.Audio.HasContent)
            {
                BytesPerPixel = ViewModel.State.Audio.DataLength/width;
            }
            else
            {
                BytesPerPixel = -1;
            }

            ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
            ViewModel.RefreshWaveFormChunkMarkers();

            if (!ViewModel.State.Audio.HasContent)
            {
                return;
            }

            if (m_WaveFormImageSourceDrawingImage != null && !(WaveFormImage.Source is DrawingImage))
            {
                ViewModel.Logger.Log("AudioPaneView.OnWaveFormCanvasSizeChanged:WaveFormImage.Source switch", Category.Debug, Priority.Medium);

                //RenderTargetBitmap source = (RenderTargetBitmap)WaveFormImage.Source;
                WaveFormImage.Source = null;
                WaveFormImage.Source = m_WaveFormImageSourceDrawingImage;
            }

            if (m_ZoomSliderDrag || ViewModel.ResizeDrag)
            {
                return;
            }

            if (oldWidth == width)
            {
                return;
            }

            ViewModel.CommandRefresh.Execute();
        }

        private void OnWaveFormMouseMove(object sender, MouseEventArgs e)
        {
            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.OnAdornerMouseMove(sender, e);
            }

            if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                m_ControlKeyWasDownAtLastMouseMove = isControlKeyDown();
                WaveFormCanvas.Cursor = isControlKeyDown() ? m_WaveFormDragMoveCursor : m_WaveFormDefaultCursor;
                return;
            }

            m_ControlKeyWasDownAtLastMouseMove = m_ControlKeyWasDownAtLastMouseMove || isControlKeyDown();

            Point p = e.GetPosition(WaveFormCanvas);

            if (m_ControlKeyWasDownAtLastMouseMove)
            {
                if (m_WaveFormDragX >= 0)
                {
                    double offset = p.X - m_WaveFormDragX;
                    //double ratio = WaveFormCanvas.ActualWidth / WaveFormScroll.ViewportWidth;
                    WaveFormScroll.ScrollToHorizontalOffset(WaveFormScroll.HorizontalOffset + offset);
                    m_WaveFormDragX = p.X;
                }
                WaveFormCanvas.Cursor = m_WaveFormDragMoveCursor;
                return;
            }

            if (p.X == m_TimeSelectionLeftX)
            {
                ViewModel.CommandClearSelection.Execute();
                m_TimeSelectionLeftX = p.X;

                WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;
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

            WaveFormCanvas.Cursor = m_WaveFormDragSelectCursor;
        }

        private void OnWaveFormMouseEnter(object sender, MouseEventArgs e)
        {
            m_ControlKeyWasDownAtLastMouseMove = isControlKeyDown();

            WaveFormCanvas.Cursor = m_ControlKeyWasDownAtLastMouseMove ? m_WaveFormDragMoveCursor : m_WaveFormDefaultCursor;
        }

        private void OnWaveFormMouseLeave(object sender, MouseEventArgs e)
        {
            WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;

            m_WaveFormDragX = -1;

            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.OnAdornerMouseLeave(sender, e);
            }

            if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (!m_ControlKeyWasDownAtLastMouseMove)
            {
                Point p = e.GetPosition(WaveFormCanvas);
                double x = Math.Min(p.X, WaveFormCanvas.ActualWidth);
                x = Math.Max(x, 0);
                selectionFinished(x);
            }
            m_ControlKeyWasDownAtLastMouseMove = false;
        }

        private void OnWaveFormMouseUp(object sender, MouseButtonEventArgs e)
        {
            WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;
            m_WaveFormDragX = -1;

            if (!m_ControlKeyWasDownAtLastMouseMove)
            {
                Point p = e.GetPosition(WaveFormCanvas);
                if (m_MouseClicks == 2)
                {
                    if (!ViewModel.State.Audio.HasContent || ViewModel.State.CurrentTreeNode == null)
                    {
                        ViewModel.CommandClearSelection.Execute();
                        ViewModel.CommandSelectAll.Execute();
                    }
                    else
                    {
                        ViewModel.CommandClearSelection.Execute();
                        ViewModel.SelectChunk(Convert.ToInt64(p.X*BytesPerPixel));
                    }
                }
                else if (m_MouseClicks == 3)
                {
                    ViewModel.CommandClearSelection.Execute();
                    ViewModel.CommandSelectAll.Execute();
                }
                else
                {
                    selectionFinished(p.X);
                }
            }

            m_ControlKeyWasDownAtLastMouseMove = false;
        }

        private uint m_MouseClicks = 0;

        private void OnWaveFormMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(WaveFormCanvas);

            ViewModel.AudioPlayer_Stop();

            if (e.ClickCount == 2)
            {
                m_MouseClicks = 2;
                return;
            }
            else if (e.ClickCount == 3)
            {
                m_MouseClicks = 3;
                return;
            }
            else
            {
                m_MouseClicks = 1;
            }

            m_ControlKeyWasDownAtLastMouseMove = m_ControlKeyWasDownAtLastMouseMove || isControlKeyDown();

            WaveFormCanvas.Cursor = m_ControlKeyWasDownAtLastMouseMove ? m_WaveFormDragMoveCursor : m_WaveFormDefaultCursor;

            if (m_ControlKeyWasDownAtLastMouseMove)
            {
                m_WaveFormDragX = p.X;
            }
            else
            {
                backupSelection();
                ViewModel.CommandClearSelection.Execute();
                m_TimeSelectionLeftX = p.X;
            }
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
            ViewModel.Logger.Log("AudioPaneView.OnPaneLoaded", Category.Debug, Priority.Medium);

            AdornerLayer layer = AdornerLayer.GetAdornerLayer(WaveFormScroll);
            if (layer == null)
            {
                m_WaveFormLoadingAdorner = null;
                m_WaveFormTimeTicksAdorner = null;
                return;
            }
            layer.ClipToBounds = true;

            m_WaveFormTimeTicksAdorner = new WaveFormTimeTicksAdorner(WaveFormScroll, this);
            layer.Add(m_WaveFormTimeTicksAdorner);
            m_WaveFormTimeTicksAdorner.Visibility = Visibility.Visible;

            m_WaveFormLoadingAdorner = new WaveFormLoadingAdorner(WaveFormScroll, this);
            layer.Add(m_WaveFormLoadingAdorner);
            m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;

            ViewModel.EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("No document.");
        }

        #endregion Event / Callbacks

        public double BytesPerPixel { get; set; }

        public string OpenFileDialog()
        {
            ViewModel.Logger.Log("AudioPaneView.OpenFileDialog", Category.Debug, Priority.Medium);

            var dlg = new OpenFileDialog
            {
                FileName = "audio",
                DefaultExt = ".wav",
                Filter = "WAV files (.wav)|*.wav;*.aiff",
                CheckFileExists = false,
                CheckPathExists = false,
                AddExtension = true,
                DereferenceLinks = true,
                Title = "Tobi: " + UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Audio_OpenFile)
            };

            var shellPresenter = ViewModel.Container.Resolve<IShellPresenter>();

            bool? result = false;

            shellPresenter.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

            if (result == false)
            {
                return null;
            }

            return dlg.FileName;
        }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void ResetWaveFormEmpty()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(ResetWaveFormEmpty));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(ResetWaveFormEmpty));
                return;
            }

            ViewModel.Logger.Log("AudioPaneView.ResetWaveFormEmpty", Category.Debug, Priority.Medium);

            double height = WaveFormCanvas.ActualHeight;
            if (double.IsNaN(height) || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            double width = WaveFormCanvas.ActualWidth;
            if (double.IsNaN(width) || width == 0)
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
        public void ResetAll()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(ResetAll));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(ResetAll));
                return;
            }

            ViewModel.Logger.Log("AudioPaneView.ResetAll", Category.Debug, Priority.Medium);

            ClearSelection();

            WaveFormPlayHeadPath.Data = null;
            WaveFormPlayHeadPath.InvalidateVisual();

            WaveFormTimeRangePath.Data = null;
            WaveFormTimeRangePath.InvalidateVisual();

            PeakMeterPathCh2.Data = null;
            PeakMeterPathCh2.InvalidateVisual();

            PeakMeterPathCh1.Data = null;
            PeakMeterPathCh1.InvalidateVisual();

            PeakMeterCanvasOpaqueMask.Visibility = Visibility.Visible;

            ResetWaveFormEmpty();

            m_WaveFormImageSourceDrawingImage = null;

            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.InvalidateVisual();
            }

            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.InvalidateVisual();
            }
        }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_WaveFormPlayHead()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(RefreshUI_WaveFormPlayHead_NoDispatcherCheck));
                Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(RefreshUI_WaveFormPlayHead_NoDispatcherCheck));
                return;
            }
            RefreshUI_WaveFormPlayHead_NoDispatcherCheck();
        }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        /// <param name="bytesLeft"></param>
        /// <param name="bytesRight"></param>
        public void RefreshUI_WaveFormChunkMarkers(long bytesLeft, long bytesRight)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => RefreshUI_WaveFormChunkMarkers(bytesLeft, bytesRight)));
                return;
            }
            ViewModel.Logger.Log("AudioPaneView.RefreshUI_WaveFormChunkMarkers", Category.Debug, Priority.Medium);

            double height = WaveFormCanvas.ActualHeight;
            if (double.IsNaN(height) || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            double pixelsLeft = bytesLeft / BytesPerPixel;
            double pixelsRight = bytesRight / BytesPerPixel;

            StreamGeometry geometryRange;
            if (WaveFormTimeRangePath.Data == null)
            {
                geometryRange = new StreamGeometry();
            }
            else
            {
                geometryRange = (StreamGeometry)WaveFormTimeRangePath.Data;
            }

            double thickNezz = m_ArrowDepth/2;

            using (StreamGeometryContext sgc = geometryRange.Open())
            {
                sgc.BeginFigure(new Point(pixelsLeft, height - thickNezz), true, false);
                sgc.LineTo(new Point(pixelsRight, height - thickNezz), false, false);
                sgc.LineTo(new Point(pixelsRight, height), false, false);
                sgc.LineTo(new Point(pixelsLeft, height), false, false);
                sgc.LineTo(new Point(pixelsLeft, 0), false, false);
                sgc.LineTo(new Point(pixelsRight, 0), false, false);
                sgc.LineTo(new Point(pixelsRight, thickNezz), false, false);
                sgc.LineTo(new Point(pixelsLeft, thickNezz), false, false);
                sgc.LineTo(new Point(pixelsLeft, 0), false, false);

                sgc.Close();
            }

            if (WaveFormTimeRangePath.Data == null)
            {
                WaveFormTimeRangePath.Data = geometryRange;
            }

            WaveFormTimeRangePath.InvalidateVisual();
        }

        private Point m_PointPeakMeter = new Point(0, 0);
        /// <summary>
        /// Refreshes the PeakMeterCanvas
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_PeakMeter()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_PeakMeter));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(RefreshUI_PeakMeter));
                return;
            }
            PCMFormatInfo pcmInfo = ViewModel.State.Audio.GetCurrentPcmFormat();

            double barWidth = PeakMeterCanvas.ActualWidth;
            if (pcmInfo.Data.NumberOfChannels > 1)
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

                m_PointPeakMeter.X = 0;
                m_PointPeakMeter.Y = 0;
                sgc.BeginFigure(m_PointPeakMeter, true, true);

                m_PointPeakMeter.X = barWidth;
                m_PointPeakMeter.Y = 0;
                sgc.LineTo(m_PointPeakMeter, false, false);

                m_PointPeakMeter.X = barWidth;
                m_PointPeakMeter.Y = availableHeight - pixels;
                sgc.LineTo(m_PointPeakMeter, false, false);

                m_PointPeakMeter.X = 0;
                m_PointPeakMeter.Y = availableHeight - pixels;
                sgc.LineTo(m_PointPeakMeter, false, false);

                sgc.Close();
            }

            StreamGeometry geometry2 = null;
            if (pcmInfo.Data.NumberOfChannels > 1)
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

                    m_PointPeakMeter.X = barWidth;
                    m_PointPeakMeter.Y = 0;
                    sgc.BeginFigure(m_PointPeakMeter, true, true);

                    m_PointPeakMeter.X = barWidth + barWidth;
                    m_PointPeakMeter.Y = 0;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    m_PointPeakMeter.X = barWidth + barWidth;
                    m_PointPeakMeter.Y = availableHeight - pixels;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    m_PointPeakMeter.X = barWidth;
                    m_PointPeakMeter.Y = availableHeight - pixels;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    m_PointPeakMeter.X = barWidth;
                    m_PointPeakMeter.Y = availableHeight - 1;
                    sgc.LineTo(m_PointPeakMeter, false, false);

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
            if (pcmInfo.Data.NumberOfChannels > 1)
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
                    //Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_PeakMeterBlackoutOn));
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(refreshUI_PeakMeterBlackoutOn));
                }
                else
                {
                    //Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_PeakMeterBlackoutOff));
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(refreshUI_PeakMeterBlackoutOff));
                }
            }
        }

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void TimeMessageRefresh()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Render, new ThreadStart(TimeMessageRefresh));
                Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(TimeMessageRefresh));
                return;
            }
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.InvalidateVisual();
            }
        }

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void TimeMessageHide()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(TimeMessageHide));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(TimeMessageHide));
                return;
            }
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.DisplayRecorderTime = false;
                m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void TimeMessageShow()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(TimeMessageShow));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(TimeMessageShow));
                return;
            }
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.DisplayRecorderTime = true;
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
                m_WaveFormLoadingAdorner.InvalidateVisual();
            }
        }

        /// <summary>
        /// Shows or hides the loading message in the adorner layer
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        /// <param name="visible"></param>
        public void ShowHideWaveFormLoadingMessage(bool visible)
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
                    //Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_LoadingMessageVisible));
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(refreshUI_LoadingMessageVisible));
                }
                else
                {
                    //Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(refreshUI_LoadingMessageHidden));
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(refreshUI_LoadingMessageHidden));
                }
            }
        }

        public void ZoomFitFull()
        {
            ViewModel.Logger.Log("AudioPaneView.OnZoomFitFull", Category.Debug, Priority.Medium);

            double widthToUse = WaveFormScroll.ViewportWidth;
            if (double.IsNaN(Double.NaN) || widthToUse == 0)
            {
                widthToUse = WaveFormScroll.ActualWidth;
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

        #region DispatcherTimers

        private DispatcherTimer m_PlaybackTimer;

        public void StopWaveFormTimer()
        {
            if (m_PlaybackTimer != null && m_PlaybackTimer.IsEnabled)
            {
                ViewModel.Logger.Log("m_PlaybackTimer.Stop()", Category.Debug, Priority.Medium);

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

                double interval = ViewModel.State.Audio.ConvertBytesToMilliseconds(Convert.ToInt64(BytesPerPixel));

                ViewModel.Logger.Log("WaveFormTimer REFRESH interval: " + interval, Category.Debug, Priority.Medium);

                if (interval < ViewModel.AudioPlayer_RefreshInterval)
                {
                    interval = ViewModel.AudioPlayer_RefreshInterval;
                }
                m_PlaybackTimer.Interval = TimeSpan.FromMilliseconds(interval);
            }
            else if (m_PlaybackTimer.IsEnabled)
            {
                return;
            }

            ViewModel.Logger.Log("m_PlaybackTimer.Start()", Category.Debug, Priority.Medium);

            m_PlaybackTimer.Start();
        }

        private void OnPlaybackTimerTick(object sender, EventArgs e)
        {
            ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
        }

        //private DispatcherTimer m_PeakMeterTimer;

        //public void StopPeakMeterTimer()
        //{
        //    if (m_PeakMeterTimer != null && m_PeakMeterTimer.IsEnabled)
        //    {
        //        ViewModel.Logger.Log("m_PeakMeterTimer.Stop()", Category.Debug, Priority.Medium);

        //        m_PeakMeterTimer.Stop();
        //    }
        //    m_PeakMeterTimer = null;
        //}

        //public void StartPeakMeterTimer()
        //{
        //    if (m_PeakMeterTimer == null)
        //    {
        //        m_PeakMeterTimer = new DispatcherTimer(DispatcherPriority.Input);
        //        m_PeakMeterTimer.Tick += OnPeakMeterTimerTick;
        //        m_PeakMeterTimer.Interval = TimeSpan.FromMilliseconds(60);
        //    }
        //    else if (m_PeakMeterTimer.IsEnabled)
        //    {
        //        return;
        //    }

        //    ViewModel.Logger.Log("m_PeakMeterTimer.Start()", Category.Debug, Priority.Medium);

        //    m_PeakMeterTimer.Start();
        //}

        //private void OnPeakMeterTimerTick(object sender, EventArgs e)
        //{
        //    ViewModel.UpdatePeakMeter();
        //}

        #endregion DispatcherTimers

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

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        private void refreshUI_LoadingMessageVisible()
        {
            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
                m_WaveFormLoadingAdorner.InvalidateVisual();
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

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        private void RefreshUI_WaveFormPlayHead_NoDispatcherCheck()
        {
            if (!ViewModel.State.Audio.HasContent)
            {
                if (m_TimeSelectionLeftX >= 0)
                {
                    scrollInView(m_TimeSelectionLeftX + 1);
                }

                return;
            }

            if (BytesPerPixel <= 0)
            {
                return;
            }

            //long bytes = ViewModel.PcmFormat.GetByteForTime(new Time(ViewModel.LastPlayHeadTime));
            long bytes = ViewModel.State.Audio.ConvertMillisecondsToBytes(ViewModel.LastPlayHeadTime);

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
            if (double.IsNaN(height) || height == 0)
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

            scrollInView(pixels);
        }

        // ReSharper restore InconsistentNaming

        private void scrollInView(double pixels)
        {
            double left = WaveFormScroll.HorizontalOffset;
            double right = left + WaveFormScroll.ViewportWidth;
            //bool b = WaveFormPlayHeadPath.IsVisible;

            if (m_TimeSelectionLeftX < 0)
            {
                if (pixels < left || pixels > right)
                {
                    double offset = pixels - 10;
                    if (offset < 0)
                    {
                        offset = 0;
                    }

                    WaveFormScroll.ScrollToHorizontalOffset(offset);
                }
            }
            else
            {
                double timeSelectionRightX = m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width;

                double minX = Math.Min(m_TimeSelectionLeftX, pixels);
                minX = Math.Min(timeSelectionRightX, minX);

                double maxX = Math.Max(m_TimeSelectionLeftX, pixels);
                maxX = Math.Max(timeSelectionRightX, maxX);

                double visibleWidth = (right - left);

                if ((maxX - minX) <= (visibleWidth - 20))
                {
                    if (minX < left)
                    {
                        double offset = minX - 10;
                        if (offset < 0)
                        {
                            offset = 0;
                        }
                        WaveFormScroll.ScrollToHorizontalOffset(offset);
                    }
                    else if (maxX > right)
                    {
                        double offset = maxX - visibleWidth + 10;
                        if (offset < 0)
                        {
                            offset = 0;
                        }
                        WaveFormScroll.ScrollToHorizontalOffset(offset);
                    }
                }
                else if (pixels >= timeSelectionRightX)
                {
                    double offset = pixels - visibleWidth + 10;
                    if (offset < 0)
                    {
                        offset = 0;
                    }
                    WaveFormScroll.ScrollToHorizontalOffset(offset);
                }
                else if (pixels <= timeSelectionRightX)
                {
                    double offset = pixels - 10;
                    if (offset < 0)
                    {
                        offset = 0;
                    }
                    WaveFormScroll.ScrollToHorizontalOffset(offset);
                }
                else if ((timeSelectionRightX - pixels) <= (visibleWidth - 10))
                {
                    double offset = pixels - 10;
                    if (offset < 0)
                    {
                        offset = 0;
                    }

                    WaveFormScroll.ScrollToHorizontalOffset(offset);
                }
                else if ((pixels - m_TimeSelectionLeftX) <= (visibleWidth - 10))
                {
                    double offset = m_TimeSelectionLeftX - 10;
                    if (offset < 0)
                    {
                        offset = 0;
                    }

                    WaveFormScroll.ScrollToHorizontalOffset(offset);
                }
                else
                {
                    double offset = pixels - 10;
                    if (offset < 0)
                    {
                        offset = 0;
                    }

                    WaveFormScroll.ScrollToHorizontalOffset(offset);
                }
            }
        }

        private bool isControlKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None;

            //System.Windows.Forms.Control.ModifierKeys == Keys.Control;
            // (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.None;
        }
        
        public void BringIntoFocus()
        {
            FocusHelper.Focus(this, FocusStart);
        }
        
        public void BringIntoFocusStatusBar()
        {
            FocusHelper.Focus(this, FocusStartStatusBar);
        }
    }
}
