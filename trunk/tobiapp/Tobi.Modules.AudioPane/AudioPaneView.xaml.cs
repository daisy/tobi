using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa.core;
using urakawa.data;
using urakawa.media.data.audio;

namespace Tobi.Plugin.AudioPane
{
    [ValueConversion(typeof(Double), typeof(Double))]
    public class MillisecondsPerPixelToPixelWidthConverter : ValueConverterMarkupExtensionBase<MillisecondsPerPixelToPixelWidthConverter>
    {
        public static double defaultWidth = 3;

        public static double calc(double millisecondsPerPixel, AudioPaneViewModel viewModel)
        {
            if (!viewModel.State.Audio.HasContent)
            {
                return defaultWidth;
            }

            long bytesPerPixel = viewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes(
                                                                                (long)Math.Round(AudioLibPCMFormat.TIME_UNIT * millisecondsPerPixel));

            if (bytesPerPixel <= 0)
            {
#if DEBUG
                DebugFix.Assert(false);
#endif // DEBUG
                return defaultWidth;
            }

            double width = (double)viewModel.State.Audio.DataLength / (double)bytesPerPixel;

            if (double.IsNaN(width) || double.IsInfinity(width) || (long)Math.Ceiling(width) == 0)
            {
#if DEBUG
                DebugFix.Assert(false);
#endif // DEBUG
            }

            return width;
        }

        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return defaultWidth;
            if (values[0] == null || values[1] == null) return defaultWidth;
            if (!(values[0] is Double) || !(values[1] is AudioPaneViewModel))
                return defaultWidth;

            var millisecondsPerPixel = (Double)values[0];
            var viewModel = (AudioPaneViewModel)values[1];

            return calc(millisecondsPerPixel, viewModel);
        }
    }

    ///<summary>
    /// Single shared instance (singleton) of the audio view
    ///</summary>
    [Export(typeof(IAudioPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class AudioPaneView : IAudioPaneView, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        private readonly IEventAggregator m_EventAggregator;

        public readonly IShellView m_ShellView;
        private readonly AudioPaneViewModel m_ViewModel;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="eventAggregator">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="viewModel">normally obtained from the Unity container, it's a Tobi built-in type</param>
        ///<param name="shellView">normally obtained from the Unity container, it's a Tobi built-in type</param>
        [ImportingConstructor]
        public AudioPaneView(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(AudioPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            AudioPaneViewModel viewModel,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {

#if DEBUG
            DebugFix.Assert(BitConverter.IsLittleEndian);
#endif // DEBUG

            m_Logger = logger;
            m_EventAggregator = eventAggregator;

            m_ViewModel = viewModel;
            m_ViewModel.SetView(this);

            m_ShellView = shellView;

            m_Logger.Log(@"AudioPaneView.ctor", Category.Debug, Priority.Medium);

            DataContext = m_ViewModel;

            InitializeComponent();

            WinFormHost.Child = m_ViewModel.GetWindowsFormsHookControl();

            ZoomSlider.SetValue(AutomationProperties.NameProperty, Tobi_Plugin_AudioPane_Lang.Audio_ZoomSlider);
            //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);
        }

        private void OnToolbarToggleVisible(object sender, MouseButtonEventArgs e)
        {
            Settings.Default.Audio_ButtonBarVisible = !Settings.Default.Audio_ButtonBarVisible;
        }

        private void OnToolbarToggleVisibleKeyboard(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) // || e.Key == Key.Space)
            {
                Settings.Default.Audio_ButtonBarVisible = !Settings.Default.Audio_ButtonBarVisible;
                FocusHelper.FocusBeginInvoke(Settings.Default.Audio_ButtonBarVisible ? FocusExpanded : FocusCollapsed);
            }
        }
        #region Construction


        public void InitGraphicalCommandBindings()
        {
            //var mouseGest = new MouseGesture { MouseAction = MouseAction.LeftDoubleClick };
            //var mouseBind = new MouseBinding { Gesture = mouseGest, Command = m_ViewModel.CommandSelectAll };
            //WaveFormCanvas.InputBindings.Add(mouseBind);


            //var mouseGest2 = new MouseGesture
            //{
            //    MouseAction = MouseAction.LeftDoubleClick,
            //    Modifiers = ModifierKeys.Control
            //};
            //var mouseBind2 = new MouseBinding { Gesture = mouseGest2, Command = m_ViewModel.CommandClearSelection };
            //WaveFormCanvas.InputBindings.Add(mouseBind2);
        }

        ~AudioPaneView()
        {
#if DEBUG
            m_Logger.Log("AudioPaneView garbage collected.", Category.Debug, Priority.Medium);
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
            m_ViewModel.UpdatePeakMeter();
        }

        private bool m_ZoomSliderDrag = false;

        private void OnZoomSliderDragStarted(object sender, DragStartedEventArgs e1)
        {
            if (m_ViewModel.IsWaveFormLoading) return;

            //m_Logger.Log("AudioPaneView.OnZoomSliderDragStarted", Category.Debug, Priority.Medium);
            m_ZoomSliderDrag = true;
        }

        private void OnZoomSliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (m_ViewModel.IsWaveFormLoading) return;

            //m_Logger.Log("AudioPaneView.OnZoomSliderDragCompleted", Category.Debug, Priority.Medium);
            m_ZoomSliderDrag = false;

            m_ViewModel.CommandRefresh.Execute();
        }

        public void InvalidateWaveFormOverlay()
        {
            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.InvalidateVisualz();
            }
        }

        //private bool m_scrollRefreshNoTimer = false;
        //private bool m_scrollRefreshSkip = false;
        private DispatcherTimer m_scrollRefreshIntervalTimer = null;
        private void OnWaveFormScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            InvalidateWaveFormOverlay();

            //if (m_scrollRefreshSkip)
            //{
            //    m_scrollRefreshSkip = false;
            //    return;
            //}

            //if (m_scrollRefreshNoTimer)
            //{
            //    m_ViewModel.AudioPlayer_LoadWaveForm(true);
            //    m_scrollRefreshNoTimer = false;
            //    return;
            //}

            if (m_scrollRefreshIntervalTimer == null)
            {
                m_scrollRefreshIntervalTimer = new DispatcherTimer(DispatcherPriority.Normal);
                m_scrollRefreshIntervalTimer.Interval = TimeSpan.FromMilliseconds(Settings.Default.AudioWaveForm_LoadDelay);
                m_scrollRefreshIntervalTimer.Tick += (oo, ee) =>
                                                         {
                                                             m_scrollRefreshIntervalTimer.Stop();
                                                             //m_scrollRefreshIntervalTimer = null;
                                                             if (!m_ZoomSliderDrag && !m_ViewModel.IsWaveFormLoading)
                                                             {
                                                                 m_ViewModel.AudioPlayer_LoadWaveForm(true);
                                                             }
                                                         };
                m_scrollRefreshIntervalTimer.Start();
            }
            else if (m_scrollRefreshIntervalTimer.IsEnabled)
            {
                //restart
                m_scrollRefreshIntervalTimer.Stop();
                m_scrollRefreshIntervalTimer.Start();
            }
            else
            {
                m_scrollRefreshIntervalTimer.Start();
            }
        }

#if DEBUG
        private double getWaveFormWidth()
        {
            double width1 = WaveFormCanvas.ActualWidth;
            double width2 = WaveFormCanvas.Width;

            if (double.IsNaN(width1) || (long)Math.Round(width1) == 0)
            {
                if (double.IsNaN(width2) || (long)Math.Round(width2) == 0)
                {
                    //throw new Exception("NO VALID WAVEFORM WIDTH!!");
                    //#if DEBUG
                    //                            Debugger.Break();
                    //#endif //DEBUG
                    return MillisecondsPerPixelToPixelWidthConverter.defaultWidth;
                }
                else return width2;
            }
            else return width1;
        }
#endif //DEBUG

        private void OnZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //m_ForceCanvasWidthUpdate = true;
            //#if DEBUG
            //            m_Logger.Log("refreshCanvasWidth (before canvas width change)", Category.Debug, Priority.Medium);
            //#endif
            //MultiBindingExpression mb1 = BindingOperations.GetMultiBindingExpression(WaveFormCanvas,
            //                                                                         FrameworkElement.WidthProperty);
            //DebugFix.Assert(mb1 != null);
            //if (mb1 != null)
            //{
            //    mb1.UpdateTarget();
            //    //mb1.UpdateSource();
            //}
            //else
            //{
            //    BindingExpression b1 = WaveFormCanvas.GetBindingExpression(FrameworkElement.WidthProperty);
            //    if (b1 != null)
            //    {
            //        b1.UpdateTarget();
            //        //b1.UpdateSource();
            //    }
            //}

            //#if DEBUG
            //            m_Logger.Log("refreshCanvasWidth (after canvas width change)", Category.Debug, Priority.Medium);
            //#endif


            //bool forceCanvasWidthUpdate = m_ForceCanvasWidthUpdate || !m_ZoomSliderDrag; // m_ViewModel.ResizeDrag;
            //m_ForceCanvasWidthUpdate = false;

            //#if DEBUG
            //            m_Logger.Log("OnWaveFormCanvasSizeChanged", Category.Debug, Priority.Medium);
            //#endif


            //            double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);

            //#if DEBUG
            //            double widthReal = getWaveFormWidth();
            //            bool match = (long)Math.Round(width * 100) == (long)Math.Round(widthReal * 100);

            //            //DebugFix.Assert(match);

            //            if (!match)
            //            {
            //                m_Logger.Log("OnWaveFormCanvasSizeChanged (WIDTH mismatch): " + widthReal + " != " + width, Category.Debug,
            //                         Priority.Medium);
            //            }
            //#endif //DEBUG

            //if (!match)
            //{
            //    return;
            //}

            //double oldWidth = e.PreviousSize.Width;

            double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);
            WaveFormCanvas.Width = width;

            //#if DEBUG
            //                m_Logger.Log("OnWaveFormCanvasSizeChanged (calling CANCEL Waveform load)", Category.Debug,
            //                             Priority.Medium);
            //#endif

            CancelWaveFormLoad(true);

            //if (m_TimeSelectionLeftX >= 0)
            //{
            //    double ratio = width / oldWidth;
            //    m_TimeSelectionLeftX *= ratio;
            //    WaveFormTimeSelectionRect.Width *= ratio;
            //    WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
            //}

            if (m_ViewModel.State.Audio.HasContent)
            {
                BytesPerPixel = m_ViewModel.State.Audio.DataLength / width;
            }
            else
            {
                BytesPerPixel = -1;
            }

            if (m_ViewModel.State.Audio.HasContent)
            {
                if (m_ViewModel.IsSelectionSet)
                {
                    SetSelectionBytes(m_ViewModel.State.Selection.SelectionBeginBytePosition, m_ViewModel.State.Selection.SelectionEndBytePosition);
                }
            }
            else
            {
                ClearSelection();
            }

            m_ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
            m_ViewModel.RefreshWaveFormChunkMarkers();

            if (!m_ViewModel.State.Audio.HasContent)
            {
                return;
            }


            updateWaveformTileImagesWidthAndPosition();
            updateWaveformTileImagesVectorFill();

            if (m_ZoomSliderDrag || m_ViewModel.ResizeDrag)
            {
                return;
            }

            //if ((long)Math.Round(width * 100) == (long)Math.Round(oldWidth * 100))
            //{
            //    return;
            //}

            //#if DEBUG
            //                m_Logger.Log("OnWaveFormCanvasSizeChanged (calling CommandRefresh)", Category.Debug, Priority.Medium);
            //#endif

            m_ViewModel.CommandRefresh.Execute();
        }

        private void OnWaveFormCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                //if (m_ViewModel.State.Audio.HasContent)
                //{
                //    if (m_ViewModel.IsSelectionSet)
                //    {
                //        SetSelectionBytes(m_ViewModel.State.Selection.SelectionBeginBytePosition, m_ViewModel.State.Selection.SelectionEndBytePosition);
                //    }
                //}
                //else
                //{
                //    ClearSelection();
                //}

                //m_ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
                RefreshUI_WaveFormPlayHead(true);

                m_ViewModel.RefreshWaveFormChunkMarkers();

                updateWaveformTileImagesVectorFill();

                var zoom = (m_ShellView != null
                                ? m_ShellView.MagnificationLevel
                                : (Double)FindResource("MagnificationLevel"));

                LightLinkedList<ImageAndDrawing>.Item current_ = m_WaveformTileImages.m_First;
                while (current_ != null)
                {
                    ImageAndDrawing imgAndDraw = current_.m_data;

                    imgAndDraw.m_image.Height = WaveFormCanvas.ActualHeight * zoom;

                    current_ = current_.m_nextItem;
                }
            }
        }

        private bool useVectorResize()
        {
            if (Settings.Default.AudioWaveForm_UseVectorAtResize)
            {
                //double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);

                LightLinkedList<ImageAndDrawing>.Item current = m_WaveformTileImages.m_First;
                if (current != null)
                {
                    ImageAndDrawing imgAndDraw = current.m_data;
                    return imgAndDraw.m_originalCanvasW <= Settings.Default.AudioWaveForm_VectorWidthThreshold;
                }
            }
            return false;
        }

        private void OnWaveFormMouseMove(object sender, MouseEventArgs e)
        {
            if (m_ViewModel.IsWaveFormLoading) return;

            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.OnAdornerMouseMove(sender, e);

                if (m_WaveFormTimeTicksAdorner.m_markerLeftMouseGrab != null
                    || m_WaveFormTimeTicksAdorner.m_markerRightMouseGrab != null)
                {
                    //if (e.LeftButton == MouseButtonState.Pressed)
                    //{
                    //    WaveFormCanvas.Cursor = Cursors.SizeWE;
                    //}
                    //else
                    //{
                    //    WaveFormCanvas.Cursor = Cursors.ScrollWE;
                    //}

                    //m_ControlKeyWasDownAtLastMouseMove = isControlKeyDown();
                    //return;
                }
                else
                {
                    //WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;
                }
            }

            if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                m_ControlKeyWasDownAtLastMouseMove = isControlKeyDown();
                WaveFormCanvas.Cursor = m_ControlKeyWasDownAtLastMouseMove ? m_WaveFormDragMoveCursor : m_WaveFormDefaultCursor;
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
                m_ViewModel.CommandClearSelection.Execute();
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
            if (m_ViewModel.IsWaveFormLoading) return;

            m_ControlKeyWasDownAtLastMouseMove = isControlKeyDown();

            WaveFormCanvas.Cursor = m_ControlKeyWasDownAtLastMouseMove ? m_WaveFormDragMoveCursor : m_WaveFormDefaultCursor;
        }

        private void OnWaveFormMouseLeave(object sender, MouseEventArgs e)
        {
            if (m_ViewModel.IsWaveFormLoading) return;

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

                double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);
#if DEBUG
                double widthReal = getWaveFormWidth();
                DebugFix.Assert((long)Math.Round(width * 100) == (long)Math.Round(widthReal * 100));
#endif //DEBUG

                double x = Math.Min(p.X, width);
                x = Math.Max(x, 0);
                selectionFinished(x);
            }
            m_ControlKeyWasDownAtLastMouseMove = false;
        }

        private void OnWaveFormMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                WaveFormCanvas.ContextMenu.PlacementTarget = WaveFormCanvas;
                WaveFormCanvas.ContextMenu.Placement = PlacementMode.Bottom;
                var p = e.GetPosition(WaveFormCanvas);
                WaveFormCanvas.ContextMenu.PlacementRectangle = new Rect(p.X, p.Y, 2, 2);
                WaveFormCanvas.ContextMenu.IsOpen = true;
                return;
            }

            if (e.ChangedButton != MouseButton.Left) return;

            if (m_ViewModel.IsWaveFormLoading) return;

            WaveFormCanvas.Cursor = m_WaveFormDefaultCursor;
            m_WaveFormDragX = -1;

            if (!m_ControlKeyWasDownAtLastMouseMove)
            {
                Point p = e.GetPosition(WaveFormCanvas);

                if (m_MouseClicks == 2)
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_ViewModel.m_UrakawaSession.GetTreeNodeSelection();
                    if (!m_ViewModel.State.Audio.HasContent || treeNodeSelection.Item1 == null)
                    {
                        m_ViewModel.CommandClearSelection.Execute();
                        m_ViewModel.CommandSelectAll.Execute();
                    }
                    else
                    {
                        m_ViewModel.CommandClearSelection.Execute();
                        m_ViewModel.SelectChunk(
                            m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                            (long)Math.Round(p.X * BytesPerPixel)));
                    }
                }
                else if (m_MouseClicks == 3)
                {
                    m_ViewModel.CommandClearSelection.Execute();
                    m_ViewModel.CommandSelectAll.Execute();
                }
                else
                {
                    if (isShiftKeyDown() && m_ViewModel.State.Audio.HasContent)
                    {
                        m_TimeSelectionLeftX = m_ViewModel.PlayBytePosition / BytesPerPixel;
                        selectionFinished(p.X);
                    }
                    else
                    {
                        if (m_TimeSelectionLeftX >= 0)
                        {
                            selectionFinished(p.X);
                        }
                    }
                }
            }

            m_ControlKeyWasDownAtLastMouseMove = false;
        }

        private uint m_MouseClicks = 0;

        private void OnWaveFormMouseDown(object sender, MouseButtonEventArgs e)
        {
            BringIntoFocus();

            if (e.LeftButton != MouseButtonState.Pressed
                || e.ChangedButton != MouseButton.Left) return;

            if (m_ViewModel.IsWaveFormLoading) return;

            Point p = e.GetPosition(WaveFormCanvas);

            m_ViewModel.CommandPause.Execute();

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
                m_ViewModel.CommandClearSelection.Execute();

                m_TimeSelectionLeftX = p.X;
            }
        }

        private void OnResetPeakOverloadCountCh1(object sender, MouseButtonEventArgs e)
        {
            m_ViewModel.PeakOverloadCountCh1 = 0;
        }

        private void OnResetPeakOverloadCountCh2(object sender, MouseButtonEventArgs e)
        {
            m_ViewModel.PeakOverloadCountCh2 = 0;
        }

        /// <summary>
        /// Init the adorner layer by adding the "Loading" message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaneLoaded(object sender, RoutedEventArgs e)
        {
            m_Logger.Log("AudioPaneView.OnPaneLoaded", Category.Debug, Priority.Medium);

            AdornerLayer layer = AdornerLayer.GetAdornerLayer(WaveFormScroll);
            if (layer == null)
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                m_WaveFormLoadingAdorner = null;
                m_WaveFormTimeTicksAdorner = null;
                return;
            }
            layer.ClipToBounds = true;

            m_WaveFormTimeTicksAdorner = new WaveFormTimeTicksAdorner(WaveFormScroll, this, m_ViewModel);
            layer.Add(m_WaveFormTimeTicksAdorner);
            m_WaveFormTimeTicksAdorner.Visibility = Visibility.Visible;

            m_WaveFormLoadingAdorner = new WaveFormLoadingAdorner(WaveFormScroll, this, m_ViewModel);
            layer.Add(m_WaveFormLoadingAdorner);
            m_WaveFormLoadingAdorner.Visibility = Visibility.Hidden;

            if (m_EventAggregator != null)
            {
                m_EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("-");// TODO Localize 
            }
        }

        #endregion Event / Callbacks

        public double BytesPerPixel { get; set; }

        public string[] OpenFileDialog()
        {
            m_Logger.Log("AudioPaneView.OpenFileDialog", Category.Debug, Priority.Medium);

            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                FileName = "",
                DefaultExt = DataProviderFactory.AUDIO_WAV_EXTENSION,
                Filter = @"WAV, MP3, MP4 (*" + DataProviderFactory.AUDIO_WAV_EXTENSION + ", *" + DataProviderFactory.AUDIO_MP3_EXTENSION + ", *" + DataProviderFactory.AUDIO_MP4_EXTENSION + ")|*" + DataProviderFactory.AUDIO_WAV_EXTENSION + ";*" + DataProviderFactory.AUDIO_MP3_EXTENSION + ";*" + DataProviderFactory.AUDIO_MP4_EXTENSION,
                CheckFileExists = false,
                CheckPathExists = false,
                AddExtension = true,
                DereferenceLinks = true,
                Title = "Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_AudioPane_Lang.CmdAudioOpenFile_ShortDesc)
            };

            bool? result = false;

            m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

            if (result == false)
            {
                return null;
            }

            return dlg.FileNames;
        }

#if DRAW_EMPTY_IMAGE
        private DrawingImage m_ResetWaveFormImageDrawing;
        private DrawingImage getResetWaveFormImageDrawing()
        {
            double height = WaveFormCanvas.ActualHeight;
            double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);

            if (m_ResetWaveFormImageDrawing == null
                || (long)Math.Round(m_ResetWaveFormImageDrawing.Width * 100) != (long)Math.Round(width * 100)
                || (long)Math.Round(m_ResetWaveFormImageDrawing.Height * 100) != (long)Math.Round(height * 100)
            )
            {
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

                Brush brushColorBack = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Back); //m_ViewModel.ColorWaveBackground);
                var geoDraw = new GeometryDrawing(brushColorBack, new Pen(brushColorBack, 1.0), geometry);
                geoDraw.Freeze();
                var drawGrp = new DrawingGroup();
                drawGrp.Children.Add(geoDraw);
                drawGrp.Freeze();
                drawImg.Drawing = drawGrp;
                drawImg.Freeze();

                m_ResetWaveFormImageDrawing = drawImg;
            }

            return m_ResetWaveFormImageDrawing;
        }
#endif //DRAW_EMPTY_IMAGE

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void ResetWaveFormEmpty()
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(ResetWaveFormEmpty));
                return;
            }

            //m_Logger.Log("AudioPaneView.ResetWaveFormEmpty", Category.Debug, Priority.Medium);

            var zoom = (m_ShellView != null
                            ? m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

#if DRAW_EMPTY_IMAGE
            WaveFormImage.Source = getResetWaveFormImageDrawing();
#else
            //WaveFormImage.Source = null;
            emptyWaveformTiles(zoom);
#endif //DRAW_EMPTY_IMAGE

            double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);
            WaveFormCanvas.Width = width;
        }

        public void ResetWaveFormChunkMarkers()
        {
            WaveFormTimeRangePath.Data = null;
            WaveFormTimeRangePath.InvalidateVisual();
        }

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void ResetAll()
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(ResetAll));
                return;
            }

            //m_Logger.Log("AudioPaneView.ResetAll", Category.Debug, Priority.Medium);

            ClearSelection();

            Canvas.SetLeft(WaveFormPlayHeadPath, -100);
            //WaveFormPlayHeadPath.Data = null;
            WaveFormPlayHeadPath.InvalidateVisual();

            ResetWaveFormChunkMarkers();

            PeakMeterPathCh2.Data = null;
            PeakMeterPathCh2.InvalidateVisual();

            PeakMeterPathCh1.Data = null;
            PeakMeterPathCh1.InvalidateVisual();

            ResetPeakLines();

            PeakMeterCanvasOpaqueMask.Visibility = Visibility.Visible;

            ResetWaveFormEmpty();

            if (m_WaveFormTimeTicksAdorner != null)
            {
                m_WaveFormTimeTicksAdorner.ResetBrushes();
                m_WaveFormTimeTicksAdorner.InvalidateVisual();
            }

            if (m_WaveFormLoadingAdorner != null)
            {
                m_WaveFormLoadingAdorner.ResetBrushes();
                m_WaveFormLoadingAdorner.InvalidateVisual();
            }
        }

        private const double DB_DROP_PEAK_LINE = 2.5;
        private double m_PeakMeterLinePeakFixedCh1_Y, m_PeakMeterLinePeakFixedCh2_Y;
        private double m_PeakMeterLinePeakDropCh1_Y, m_PeakMeterLinePeakDropCh2_Y;
        public void ResetPeakLines()
        {
            m_PeakMeterLinePeakFixedCh1_Y = -1000;
            PeakMeterLinePeakFixedCh1.Y1 = m_PeakMeterLinePeakFixedCh1_Y;
            PeakMeterLinePeakFixedCh1.Y2 = PeakMeterLinePeakFixedCh1.Y1;
            PeakMeterLinePeakFixedCh1.Visibility = Visibility.Hidden;

            m_PeakMeterLinePeakFixedCh2_Y = -1000;
            PeakMeterLinePeakFixedCh2.Y1 = m_PeakMeterLinePeakFixedCh2_Y;
            PeakMeterLinePeakFixedCh2.Y2 = PeakMeterLinePeakFixedCh2.Y1;
            PeakMeterLinePeakFixedCh2.Visibility = Visibility.Hidden;

            m_PeakMeterLinePeakDropCh1_Y = -1000;
            PeakMeterLinePeakDropCh1.Y1 = m_PeakMeterLinePeakDropCh1_Y;
            PeakMeterLinePeakDropCh1.Y2 = PeakMeterLinePeakDropCh1.Y1;
            PeakMeterLinePeakDropCh1.Visibility = Visibility.Hidden;

            m_PeakMeterLinePeakDropCh2_Y = -1000;
            PeakMeterLinePeakDropCh2.Y1 = m_PeakMeterLinePeakDropCh2_Y;
            PeakMeterLinePeakDropCh2.Y2 = PeakMeterLinePeakDropCh2.Y1;
            PeakMeterLinePeakDropCh2.Visibility = Visibility.Hidden;
        }

        private double m_PlaybackHeadHeight = 0;

        /// <summary>
        /// (ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_WaveFormPlayHead(bool scrollSelection)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<bool>)(RefreshUI_WaveFormPlayHead), scrollSelection);
                return;
            }
            if (!m_ViewModel.State.Audio.HasContent)
            {
                if (m_TimeSelectionLeftX >= 0)
                {
                    scrollInView(m_TimeSelectionLeftX + 1, true);
                }

                return;
            }

            if (BytesPerPixel <= 0)
            {
                return;
            }

            double pixels = m_ViewModel.PlayBytePosition / BytesPerPixel;

            double height = WaveFormCanvas.ActualHeight;
            //if (double.IsNaN(height) || height == 0)
            //{
            //    height = WaveFormCanvas.Height;
            //}

            //double left = Canvas.GetLeft(WaveFormPlayHeadPath);

            if (WaveFormPlayHeadPath.Data == null
                || (int)Math.Round(10 * m_PlaybackHeadHeight) != (int)Math.Round(10 * height))
            {
#if DEBUG
                m_Logger.Log("=====> WaveFormPlayHeadPath.Data Geometry refresh", Category.Debug, Priority.Medium);
#endif //DEBUG

                m_PlaybackHeadHeight = height;

                int pixelz = 0;

                StreamGeometry geometry = new StreamGeometry();

                using (StreamGeometryContext sgc = geometry.Open())
                {
                    if (m_ViewModel.PlayBytePosition < 0)
                    {
                        sgc.BeginFigure(new Point(pixelz, height), true, false);
                        sgc.LineTo(new Point(pixelz, 0), true, false);
                    }
                    else
                    {
                        sgc.BeginFigure(new Point(pixelz, height - m_ArrowDepth), true, false);
                        sgc.LineTo(new Point(pixelz + m_ArrowDepth, height), true, false);
                        sgc.LineTo(new Point(pixelz - m_ArrowDepth, height), true, false);
                        sgc.LineTo(new Point(pixelz, height - m_ArrowDepth), true, false);
                        sgc.LineTo(new Point(pixelz, m_ArrowDepth), true, false);
                        sgc.LineTo(new Point(pixelz - m_ArrowDepth, 0), true, false);
                        sgc.LineTo(new Point(pixelz + m_ArrowDepth, 0), true, false);
                        sgc.LineTo(new Point(pixelz, m_ArrowDepth), true, false);
                    }

                    sgc.Close();
                }

                geometry.Freeze();
                WaveFormPlayHeadPath.Data = geometry;
            }
            else
            {
                //geometry = (StreamGeometry)WaveFormPlayHeadPath.Data;
            }

            Canvas.SetLeft(WaveFormPlayHeadPath, pixels);
            //WaveFormPlayHeadPath.SetValue(Canvas.LeftProperty, pixels);

            WaveFormPlayHeadPath.InvalidateVisual();

            scrollInView(pixels, scrollSelection);
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
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<long, long>)RefreshUI_WaveFormChunkMarkers, bytesLeft, bytesRight);
                return;
            }
            //m_Logger.Log("AudioPaneView.RefreshUI_WaveFormChunkMarkers", Category.Debug, Priority.Medium);

            double height = WaveFormCanvas.ActualHeight;
            //if (double.IsNaN(height) || height == 0)
            //{
            //    height = WaveFormCanvas.Height;
            //}

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

            double thickNezz = m_ArrowDepth / 2;

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
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(RefreshUI_PeakMeter));
                return;
            }
            PCMFormatInfo pcmInfo = m_ViewModel.State.Audio.GetCurrentPcmFormat();
            if (pcmInfo == null) return;

            double barWidth = PeakMeterCanvas.ActualWidth;
            if (pcmInfo.Data.NumberOfChannels > 1)
            {
                barWidth = barWidth / 2;
            }
            else
            {
                if (PeakMeterLinePeakFixedCh2.Visibility != Visibility.Hidden)
                {
                    PeakMeterLinePeakFixedCh2.Visibility = Visibility.Hidden;
                }
                if (PeakMeterLinePeakDropCh2.Visibility != Visibility.Hidden)
                {
                    PeakMeterLinePeakDropCh2.Visibility = Visibility.Hidden;
                }
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
                double pixels = m_ViewModel.PeakMeterBarDataCh1.DbToPixels(availableHeight);

                m_PointPeakMeter.X = 0;
                m_PointPeakMeter.Y = 0;
                sgc.BeginFigure(m_PointPeakMeter, true, true);

                m_PointPeakMeter.X = barWidth;
                m_PointPeakMeter.Y = 0;
                sgc.LineTo(m_PointPeakMeter, false, false);

                double diff = availableHeight - pixels;

                m_PointPeakMeter.X = barWidth;
                m_PointPeakMeter.Y = diff;
                sgc.LineTo(m_PointPeakMeter, false, false);

                m_PointPeakMeter.X = 0;
                m_PointPeakMeter.Y = diff;
                sgc.LineTo(m_PointPeakMeter, false, false);

                sgc.Close();

                if (m_ViewModel.PeakMeterBarDataCh1.ValueDb > m_PeakMeterLinePeakFixedCh1_Y)
                {
                    m_PeakMeterLinePeakFixedCh1_Y = m_ViewModel.PeakMeterBarDataCh1.ValueDb;
                }

                if (PeakMeterLinePeakFixedCh1.Visibility != Visibility.Visible)
                {
                    PeakMeterLinePeakFixedCh1.Visibility = Visibility.Visible;
                }

                double pixels_ = m_ViewModel.PeakMeterBarDataCh1.DbToPixels(availableHeight, m_PeakMeterLinePeakFixedCh1_Y);
                double diff_ = availableHeight - pixels_;

                PeakMeterLinePeakFixedCh1.Y1 = diff_;
                PeakMeterLinePeakFixedCh1.Y2 = PeakMeterLinePeakFixedCh1.Y1;

                PeakMeterLinePeakFixedCh1.X1 = 0;
                PeakMeterLinePeakFixedCh1.X2 = barWidth;

                PeakMeterLinePeakFixedCh1.InvalidateVisual();

                m_PeakMeterLinePeakDropCh1_Y -= DB_DROP_PEAK_LINE;
                if (m_PeakMeterLinePeakDropCh1_Y < m_ViewModel.PeakMeterBarDataCh1.MinimumDb)
                {
                    m_PeakMeterLinePeakDropCh1_Y = m_ViewModel.PeakMeterBarDataCh1.MinimumDb;
                }

                if (m_ViewModel.PeakMeterBarDataCh1.ValueDb > m_PeakMeterLinePeakDropCh1_Y)
                {
                    m_PeakMeterLinePeakDropCh1_Y = m_ViewModel.PeakMeterBarDataCh1.ValueDb;
                }

                if (PeakMeterLinePeakDropCh1.Visibility != Visibility.Visible)
                {
                    PeakMeterLinePeakDropCh1.Visibility = Visibility.Visible;
                }

                pixels_ = m_ViewModel.PeakMeterBarDataCh1.DbToPixels(availableHeight, m_PeakMeterLinePeakDropCh1_Y);
                diff_ = availableHeight - pixels_;

                PeakMeterLinePeakDropCh1.Y1 = diff_;
                PeakMeterLinePeakDropCh1.Y2 = PeakMeterLinePeakDropCh1.Y1;

                PeakMeterLinePeakDropCh1.X1 = 0;
                PeakMeterLinePeakDropCh1.X2 = barWidth;

                PeakMeterLinePeakDropCh1.InvalidateVisual();
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
                    double pixels = m_ViewModel.PeakMeterBarDataCh2.DbToPixels(availableHeight);

                    m_PointPeakMeter.X = barWidth;
                    m_PointPeakMeter.Y = 0;
                    sgc.BeginFigure(m_PointPeakMeter, true, true);

                    m_PointPeakMeter.X = barWidth + barWidth;
                    m_PointPeakMeter.Y = 0;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    double diff = availableHeight - pixels;

                    m_PointPeakMeter.X = barWidth + barWidth;
                    m_PointPeakMeter.Y = diff;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    m_PointPeakMeter.X = barWidth;
                    m_PointPeakMeter.Y = diff;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    m_PointPeakMeter.X = barWidth;
                    m_PointPeakMeter.Y = availableHeight - 1;
                    sgc.LineTo(m_PointPeakMeter, false, false);

                    sgc.Close();

                    if (m_ViewModel.PeakMeterBarDataCh2.ValueDb > m_PeakMeterLinePeakFixedCh2_Y)
                    {
                        m_PeakMeterLinePeakFixedCh2_Y = m_ViewModel.PeakMeterBarDataCh2.ValueDb;
                    }

                    if (PeakMeterLinePeakFixedCh2.Visibility != Visibility.Visible)
                    {
                        PeakMeterLinePeakFixedCh2.Visibility = Visibility.Visible;
                    }

                    double pixels_ = m_ViewModel.PeakMeterBarDataCh2.DbToPixels(availableHeight, m_PeakMeterLinePeakFixedCh2_Y);
                    double diff_ = availableHeight - pixels_;

                    PeakMeterLinePeakFixedCh2.Y1 = diff_;
                    PeakMeterLinePeakFixedCh2.Y2 = PeakMeterLinePeakFixedCh2.Y1;

                    PeakMeterLinePeakFixedCh2.X1 = barWidth;
                    PeakMeterLinePeakFixedCh2.X2 = barWidth + barWidth;

                    PeakMeterLinePeakFixedCh2.InvalidateVisual();

                    m_PeakMeterLinePeakDropCh2_Y -= DB_DROP_PEAK_LINE;
                    if (m_PeakMeterLinePeakDropCh2_Y < m_ViewModel.PeakMeterBarDataCh2.MinimumDb)
                    {
                        m_PeakMeterLinePeakDropCh2_Y = m_ViewModel.PeakMeterBarDataCh2.MinimumDb;
                    }

                    if (m_ViewModel.PeakMeterBarDataCh2.ValueDb > m_PeakMeterLinePeakDropCh2_Y)
                    {
                        m_PeakMeterLinePeakDropCh2_Y = m_ViewModel.PeakMeterBarDataCh2.ValueDb;
                    }

                    if (PeakMeterLinePeakDropCh2.Visibility != Visibility.Visible)
                    {
                        PeakMeterLinePeakDropCh2.Visibility = Visibility.Visible;
                    }

                    pixels_ = m_ViewModel.PeakMeterBarDataCh2.DbToPixels(availableHeight, m_PeakMeterLinePeakDropCh2_Y);
                    diff_ = availableHeight - pixels_;

                    PeakMeterLinePeakDropCh2.Y1 = diff_;
                    PeakMeterLinePeakDropCh2.Y2 = PeakMeterLinePeakDropCh2.Y1;

                    PeakMeterLinePeakDropCh2.X1 = barWidth;
                    PeakMeterLinePeakDropCh2.X2 = barWidth + barWidth;

                    PeakMeterLinePeakDropCh2.InvalidateVisual();
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

        private void ResetPeakLabels()
        {
            PCMFormatInfo format = m_ViewModel.State.Audio.GetCurrentPcmFormat();
            if (format == null || format.Data.NumberOfChannels == 1)
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Collapsed;
                PeakOverloadLabelCh1.SetValue(Grid.ColumnSpanProperty, 2);
            }
            else
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Visible;
                PeakOverloadLabelCh1.SetValue(Grid.ColumnSpanProperty, 1);
            }
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
#if DEBUG
                Debugger.Break();
#endif
                if (black)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(refreshUI_PeakMeterBlackoutOn));
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(refreshUI_PeakMeterBlackoutOff));
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
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(TimeMessageRefresh));
                return;
            }
            if (m_WaveFormLoadingAdorner != null)
            {
                if (m_WaveFormLoadingAdorner.Visibility != Visibility.Visible)
                    m_WaveFormLoadingAdorner.Visibility = Visibility.Visible;
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
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(TimeMessageHide));
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
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(TimeMessageShow));
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
#if DEBUG
                Debugger.Break();
#endif
                if (visible)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(refreshUI_LoadingMessageVisible));
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(refreshUI_LoadingMessageHidden));
                }
            }
        }

        public void ZoomFitFull()
        {
            m_Logger.Log("AudioPaneView.OnZoomFitFull", Category.Debug, Priority.Medium);

            double widthToUse = WaveFormScroll.ViewportWidth;

            if (!m_ViewModel.State.Audio.HasContent)
            {
                // resets to MillisecondsPerPixelToPixelWidthConverter.defaultWidth

                //m_ForceCanvasWidthUpdate = false;
                ZoomSlider.Value += 1;
                return;
            }

            long bytesPerPixel = (long)Math.Round(m_ViewModel.State.Audio.DataLength / (double)widthToUse);
            bytesPerPixel =
                m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(bytesPerPixel);

            double millisecondsPerPixel = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(bytesPerPixel) / (double)AudioLibPCMFormat.TIME_UNIT;

            if (millisecondsPerPixel < ZoomSlider.Minimum)
            {
                ZoomSlider.Minimum = millisecondsPerPixel;
            }
            if (millisecondsPerPixel > ZoomSlider.Maximum)
            {
                ZoomSlider.Maximum = millisecondsPerPixel;
            }

            //m_ForceCanvasWidthUpdate = false;
            ZoomSlider.Value = millisecondsPerPixel;
        }

        #region DispatcherTimers

        private DispatcherTimer m_PlaybackTimer;

        public void StopWaveFormTimer()
        {
            if (m_PlaybackTimer != null && m_PlaybackTimer.IsEnabled)
            {
                //m_Logger.Log("m_PlaybackTimer.Stop()", Category.Debug, Priority.Medium);

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

                double intervalMilliseconds = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                    m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                    (long)Math.Round(BytesPerPixel))) / (double)AudioLibPCMFormat.TIME_UNIT;

                //m_Logger.Log("WaveFormTimer REFRESH interval: " + interval, Category.Debug, Priority.Medium);

                if (intervalMilliseconds < m_ViewModel.AudioPlayer_RefreshInterval)
                {
                    intervalMilliseconds = m_ViewModel.AudioPlayer_RefreshInterval;
                }
                m_PlaybackTimer.Interval = TimeSpan.FromMilliseconds(intervalMilliseconds);
            }
            else if (m_PlaybackTimer.IsEnabled)
            {
                return;
            }

            //m_Logger.Log("m_PlaybackTimer.Start()", Category.Debug, Priority.Medium);

            m_PlaybackTimer.Start();
        }

        private void OnPlaybackTimerTick(object sender, EventArgs e)
        {
            m_ViewModel.AudioPlayer_UpdateWaveFormPlayHead();
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
            ResetPeakLines();
            ResetPeakLabels();
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
                m_WaveFormLoadingAdorner.ResetBrushes();
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

        // ReSharper restore InconsistentNaming

        private void scrollInView(double pixels, bool allowSelection)
        {
            double left = WaveFormScroll.HorizontalOffset;
            double right = left + WaveFormScroll.ViewportWidth;
            //bool b = WaveFormPlayHeadPath.IsVisible;

            // DISABLED BECAUSE OF ON-THE-FLY WAVEFORM LOADING (TOO MANY SCROLL REQUESTS => NO REFRESH)
            if (m_TimeSelectionLeftX < 0 || !allowSelection)
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

        private bool isShiftKeyDown()
        {
            return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None;

            //Keyboard.IsKeyDown(Key.LeftShift)
            //System.Windows.Forms.Control.ModifierKeys == Keys.Control;
            // (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.None;
        }

        private bool isControlKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Control
                //| ModifierKeys.Shift
                    )
                    ) != ModifierKeys.None;

            //Keyboard.IsKeyDown(Key.LeftShift)
            //System.Windows.Forms.Control.ModifierKeys == Keys.Control;
            // (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.None;
        }

        public void BringIntoFocus()
        {
            if (FocusCollapsed.IsVisible)
            {
                FocusHelper.FocusBeginInvoke(FocusCollapsed);
            }
            else
            {
                FocusHelper.FocusBeginInvoke(FocusExpanded);
            }
        }

        public void BringIntoFocusStatusBar()
        {
            FocusHelper.FocusBeginInvoke(FocusStartStatusBar);
        }


        private void OnMouseClickCheckBox(object sender, RoutedEventArgs e)
        {
            BringIntoFocus();
        }

        private void OnMouseUp_RecordPlayPreview(object sender, MouseButtonEventArgs e)
        {
            //Settings.Default.Audio_EnablePlayPreviewBeforeRecord = !Settings.Default.Audio_EnablePlayPreviewBeforeRecord;
            m_ViewModel.CommandTogglePlayPreviewBeforeRecord.Execute();
        }

        private void OnKeyUp_RecordPlayPreview(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Return)
            {
                OnMouseUp_RecordPlayPreview(null, null);
            }
        }


        private void OnMouseUp_AutoPlay(object sender, MouseButtonEventArgs e)
        {
            //m_ViewModel.IsAutoPlay = !m_ViewModel.IsAutoPlay;
            m_ViewModel.CommandAutoPlay.Execute();
        }

        private void OnKeyUp_AutoPlay(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Return)
            {
                OnMouseUp_AutoPlay(null, null);
            }
        }


        private void OnMouseUp_TotalSession(object sender, MouseButtonEventArgs e)
        {
            //m_ViewModel.TotalSessionAudioDurationInLocalUnits = 0;
            m_ViewModel.CommandResetSessionCounter.Execute();
        }

        private void OnKeyUp_TotalSession(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Return)
            {
                OnMouseUp_TotalSession(null, null);
            }
        }
    }
}
