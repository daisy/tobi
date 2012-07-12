using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa.core;
using urakawa.data;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    public enum WaveFormRenderMethod : byte
    {
        Vector,
        RenderTargetBitmap,
        WriteableBitmap,
        BitmapSource
    }

    public enum WaveFormDrawStyle : byte
    {
        Border,
        BorderFill,
        BorderFillBars,
        BorderBars,
        Bars
    }

    public class ImageAndDrawing
    {
        public Image m_image;
        public DrawingImage m_drawingImage;

        public double m_originalX;
        public double m_originalW;
        public double m_originalCanvasW;

        public bool m_imageSourceLoaded;
    }

    public partial class AudioPaneView
    {
        private LightLinkedList<ImageAndDrawing> m_WaveformTileImages = new LightLinkedList<ImageAndDrawing>();

        private LightLinkedList<Image> m_WaveformImagePool = new LightLinkedList<Image>();
        private void OnWaveFormCanvasLoaded(object sender, RoutedEventArgs e)
        {
            var zoom = (m_ShellView != null
                            ? m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

            int count = (int)Math.Round(1600 / Settings.Default.AudioWaveForm_TileWidth);
            int poolSize = Math.Max(1, count);
            for (int i = 0; i < poolSize; i++)
            {
                //Image image = createWaveformTileImage(-100, 10);
                var image = new Image();
                resetWaveformPooledImage(image, zoom);

                m_WaveformImagePool.Add(image);

                WaveFormCanvas.Children.Add(image);
            }
        }

        private void resetWaveformPooledImage(Image image, double zoom)
        {
            initEmptyImageTile(image, -100, 10, zoom);
            image.Visibility = Visibility.Collapsed; // TODO: Hidden  ?
        }

        private bool waveformImagePoolContains(Image image)
        {
            LightLinkedList<Image>.Item current = m_WaveformImagePool.m_First;
            while (current != null)
            {
                Image img = current.m_data;
                if (img == image)
                {
                    return true;
                }
                current = current.m_nextItem;
            }

            return false;
        }

        private void emptyWaveformTiles(double zoom)
        {
            //#if DEBUG
            //            m_Logger.Log("emptyWaveformTiles", Category.Debug, Priority.Medium);
            //#endif

            LightLinkedList<ImageAndDrawing>.Item current = m_WaveformTileImages.m_First;
            while (current != null)
            {
                ImageAndDrawing imgAndDraw = current.m_data;

                disposeImageSource(imgAndDraw.m_image);

                imgAndDraw.m_imageSourceLoaded = false;

                if (waveformImagePoolContains(imgAndDraw.m_image))
                {
                    resetWaveformPooledImage(imgAndDraw.m_image, zoom);
                }
                else
                {
                    WaveFormCanvas.Children.Remove(imgAndDraw.m_image);
                }

                imgAndDraw.m_image = null;
                imgAndDraw.m_drawingImage = null;

                current = current.m_nextItem;
            }

            m_WaveformTileImages.Clear();
        }

        public void disposeImageSource(Image image)
        {
            if (image.Source != null)
            {
                if (image.Source is RenderTargetBitmap)
                {
                    RenderTargetBitmap bitmap = (RenderTargetBitmap)image.Source;
                    // TODO bitmap. DESTROY ?
                }
                else if (image.Source is WriteableBitmap)
                {
                    WriteableBitmap bitmap = (WriteableBitmap)image.Source;
                    // TODO bitmap. DESTROY ?
                }
                else if (image.Source is BitmapSource)
                {
                    BitmapSource bitmap = (BitmapSource)image.Source;
                    // TODO bitmap. DESTROY ?
                }
            }

            image.Source = null;
        }

        private ScaleTransform m_cachedScaleTransform = new ScaleTransform(1, 1);

        private void initEmptyImageTile(Image image, double x, double w, double zoom)
        {
            long zoomNormalized = (long)Math.Round(zoom * 1000);
            if (zoomNormalized == 1000)
            {
                image.LayoutTransform = null;
            }
            else
            {
                long scaleNormalized = (long)Math.Round(m_cachedScaleTransform.ScaleX * 1000);
                if (scaleNormalized != zoomNormalized)
                {
                    double inverseZoom = 1 / zoom;
                    m_cachedScaleTransform.ScaleX = inverseZoom;
                    m_cachedScaleTransform.ScaleY = inverseZoom;
                }
                image.LayoutTransform = m_cachedScaleTransform;
            }

            image.SetValue(Canvas.LeftProperty, x);

            //image.SetValue(FrameworkElement.WidthProperty, w);
            image.Width = w * zoom;

            image.SetValue(Canvas.TopProperty, 0.0);
            //Canvas.SetTop(image, 0);

            image.SetValue(Panel.ZIndexProperty, 0);
            //Panel.SetZIndex(image, 0);

            //image.SetValue(Image.StretchProperty, Stretch.Fill);
            image.Stretch = Stretch.None;

            image.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
            //RenderOptions.SetEdgeMode(image, EdgeMode.Unspecified);

#if NET40
            image.SetValue(RenderOptions.ClearTypeHintProperty, ClearTypeHint.Auto);
            //RenderOptions.SetClearTypeHint(image, ClearTypeHint.Auto);

            image.CacheMode = null;
#endif //NET40

            image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.LowQuality);
            //RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality);

            image.SetValue(RenderOptions.CachingHintProperty, CachingHint.Cache);
            //RenderOptions.SetCachingHint(image, CachingHint.Cache);

            //image.SetValue(RenderOptions.CacheInvalidationThresholdMinimumProperty, 1);
            //image.SetValue(RenderOptions.CacheInvalidationThresholdMaximumProperty, 1);

            image.SnapsToDevicePixels = true;
            //image.SetValue(UIElement.SnapsToDevicePixelsProperty, false);


            image.Height = WaveFormCanvas.ActualHeight * zoom;
            //var binding = new Binding();
            //binding.Mode = BindingMode.OneWay;
            //binding.ElementName = WaveFormCanvas.Name;
            //string pathName = FrameworkElement.ActualHeightProperty.ToString();
            //binding.Path = new PropertyPath(pathName); //"ActualHeight"
            ////PropertyChangedNotifyBase.GetMemberName(()=>)
            //image.SetBinding(FrameworkElement.HeightProperty, binding);

            image.Visibility = Visibility.Visible;
        }

        private Image createWaveformTileImage(double x, double w, double zoom)
        {
            //#if DEBUG
            //            m_Logger.Log("createWaveformTileImage: " + x + " / " + w, Category.Debug, Priority.Medium);
            //#endif

            var image = new Image();
            initEmptyImageTile(image, x, w, zoom);
            return image;
        }

        private void createWaveformTileImages(double canvasWidth, double zoom)
        {
            emptyWaveformTiles(zoom);

            //double canvasWidth = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);

            double waveformTileWidth = Settings.Default.AudioWaveForm_TileWidth;

            double nTiles = canvasWidth / waveformTileWidth;

            //#if DEBUG
            //            m_Logger.Log("createWaveformTileImages: " + nTiles + " => " + canvasWidth + " (" + m_WaveformTileWidth + ")", Category.Debug, Priority.Medium);
            //#endif

            if (nTiles <= 1.0)
            {
                var imgAndDraw = new ImageAndDrawing();
                imgAndDraw.m_drawingImage = null;
                imgAndDraw.m_originalCanvasW = canvasWidth;
                imgAndDraw.m_originalW = canvasWidth;
                imgAndDraw.m_originalX = 0;

                m_WaveformTileImages.Add(imgAndDraw);

                imgAndDraw.m_image = m_WaveformImagePool.m_First.m_data;
                initEmptyImageTile(imgAndDraw.m_image, imgAndDraw.m_originalX, imgAndDraw.m_originalW, zoom);

                //imgAndDraw.m_image = createWaveformTileImage(imgAndDraw.m_originalX, imgAndDraw.m_originalW);
                //WaveFormCanvas.Children.Add(imgAndDraw.m_image);

                return;
            }

            double wRemainder = canvasWidth;

            LightLinkedList<Image>.Item pooledImage = m_WaveformImagePool.m_First;
            while (wRemainder > 0)
            {
                var imgAndDraw = new ImageAndDrawing();
                imgAndDraw.m_drawingImage = null;
                imgAndDraw.m_originalCanvasW = canvasWidth;
                imgAndDraw.m_originalW = Math.Min(waveformTileWidth, wRemainder);
                imgAndDraw.m_originalX = canvasWidth - wRemainder;

                m_WaveformTileImages.Add(imgAndDraw);

                if (pooledImage != null)
                {
                    imgAndDraw.m_image = pooledImage.m_data;
                    pooledImage = pooledImage.m_nextItem;
                    initEmptyImageTile(imgAndDraw.m_image, imgAndDraw.m_originalX, imgAndDraw.m_originalW, zoom);
                }
                else
                {
                    imgAndDraw.m_image = createWaveformTileImage(imgAndDraw.m_originalX, imgAndDraw.m_originalW, zoom);
                    WaveFormCanvas.Children.Add(imgAndDraw.m_image);
                }

                wRemainder -= imgAndDraw.m_originalW;
            }
        }

        private void updateWaveformTileImagesVectorFill()
        {
            LightLinkedList<ImageAndDrawing>.Item current = m_WaveformTileImages.m_First;
            while (current != null)
            {
                ImageAndDrawing imgAndDraw = current.m_data;

                //imgAndDraw.m_image.SetValue(Image.StretchProperty, Stretch.Fill);
                imgAndDraw.m_image.Stretch = Stretch.Fill;

                if (useVectorResize())
                {
#if NET40
                    //imgAndDraw.m_image.CacheMode = null;
#endif //NET40

                    if (imgAndDraw.m_drawingImage != null
                        &&
                        (
                        !(imgAndDraw.m_image.Source is DrawingImage)
                        ||
                        imgAndDraw.m_image.Source != imgAndDraw.m_drawingImage
                        )
                        )
                    {
                        //m_Logger.Log("AudioPaneView.OnWaveFormCanvasSizeChanged:WaveFormImage.Source switch", Category.Debug,
                        //             Priority.Medium);

                        disposeImageSource(imgAndDraw.m_image);

                        imgAndDraw.m_image.Source = imgAndDraw.m_drawingImage;
                    }
                }

                current = current.m_nextItem;
            }
        }

        private void updateWaveformTileImagesWidthAndPosition()
        {
            var zoom = (m_ShellView != null
                            ? m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

            double canvasWidth = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);

            LightLinkedList<ImageAndDrawing>.Item current = m_WaveformTileImages.m_First;
            while (current != null)
            {
                ImageAndDrawing imgAndDraw = current.m_data;

                double ratio = canvasWidth / imgAndDraw.m_originalCanvasW;

                double w = imgAndDraw.m_originalW * ratio;
                double x = imgAndDraw.m_originalX * ratio;

                imgAndDraw.m_image.Width = w * zoom;
                imgAndDraw.m_image.SetValue(Canvas.LeftProperty, x);

                //#if DEBUG
                //                m_Logger.Log("updateWaveformTileImagesWidthAndPosition: " + x + " / " + w, Category.Debug, Priority.Medium);
                //#endif
                current = current.m_nextItem;
            }
        }

        private bool m_CancelInterruptDrawingToo;
        private bool m_CancelRequested;

        private Thread m_LoadThread;

        //private static ManualResetEvent m_ManualResetEvent = new ManualResetEvent(true); // CANCEL does not wait by default (the event is signaled)
        //private BackgroundWorker m_BackgroundLoader;
        //private ReaderWriterLockSlim m_CancelLock = new ReaderWriterLockSlim();
        //private object m_CancelLock = new object();
        //private DispatcherFrame m_CancelDispatcherFrame;

        private bool m_LoadThreadIsAlive = false;

        public void CancelWaveFormLoad(bool interruptDrawingToo)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<bool>)CancelWaveFormLoad, interruptDrawingToo);
                return;
            }

            if (m_LoadThread == null) return;

            if (!m_LoadThread.IsAlive && !m_LoadThreadIsAlive)
            {
                m_LoadThread = null;
                m_CancelRequested = false;

                int index_ = 0;
                while (m_ViewModel.IsWaveFormLoading)
                {
                    Console.WriteLine(@"..............m_ViewModel.IsWaveFormLoading: " + index_++);

                    m_ShellView.PumpDispatcherFrames(DispatcherPriority.Normal);

                    //if (index_ > 50)
                    //{
                    //    Console.WriteLine(@"..............BREAK m_ViewModel.IsWaveFormLoading: " + index_++);
                    //    break;
                    //}
                }

                CommandManager.InvalidateRequerySuggested();
                return;
            }

            m_CancelInterruptDrawingToo = interruptDrawingToo;
            m_CancelRequested = true;

            m_ShellView.PumpDispatcherFrames(DispatcherPriority.Normal);
            //Thread.Sleep(20);

            int index = 0;
            while (m_LoadThread.IsAlive || m_LoadThreadIsAlive)
            {
                Console.WriteLine(@"..............(1) m_LoadThread.IsAlive || m_LoadThreadIsAlive: " + index++);
                Thread.Sleep(20);

                if (m_LoadThread.Join(100))
                {
                    //Console.WriteLine(@"..............CANCEL m_LoadThread.Join(100) OK: " + index++);
                    break;
                }

                m_ShellView.PumpDispatcherFrames(DispatcherPriority.Normal);

                if (index++ == 3)
                {
                    Console.WriteLine(@"..............CANCEL m_LoadThread.Join(100) ABORT !: " + index++);
                    m_LoadThread.Abort();
                    break;
                }
            }

            index = 0;
            while (m_LoadThread.IsAlive || m_LoadThreadIsAlive)
            {
                Console.WriteLine(@"..............(2) m_LoadThread.IsAlive || m_LoadThreadIsAlive: " + index++);
                m_ShellView.PumpDispatcherFrames(DispatcherPriority.Normal);
                //Thread.Sleep(20);

                //if (index > 50)
                //{
                //    Console.WriteLine(@"..............BREAK (2) m_LoadThread.IsAlive || m_LoadThreadIsAlive: " + index++);
                //    break;
                //}
            }

            m_LoadThread = null;
            m_CancelRequested = false;

            index = 0;
            while (m_ViewModel.IsWaveFormLoading)
            {
                Console.WriteLine(@"..............m_ViewModel.IsWaveFormLoading: " + index++);

                m_ShellView.PumpDispatcherFrames(DispatcherPriority.Normal);
                //Thread.Sleep(20);
                //if (index > 50)
                //{
                //    Console.WriteLine(@"..............BREAK m_ViewModel.IsWaveFormLoading: " + index++);
                //    break;
                //}
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void OnWaveFormCancelButtonClick(object sender, RoutedEventArgs e)
        {
            CancelWaveFormLoad(false);
        }


        private double m_ProgressVisibleOffset = 0;

        //private bool m_ForceCanvasWidthUpdate = false;


        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_LoadWaveForm(bool wasPlaying, bool onlyUpdateTiles)
        {
            var zoom = (m_ShellView != null
                            ? m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

            double widthReal = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);
            //#if DEBUG
            //            double width_ = getWaveFormWidth();
            //            DebugFix.Assert((long)Math.Round(width_ * 100) == (long)Math.Round(widthReal * 100));
            //#endif //DEBUG

            if (!onlyUpdateTiles)
            {
                //m_scrollRefreshSkip = true;
                //RefreshCanvasWidth();

                BytesPerPixel = m_ViewModel.State.Audio.DataLength / widthReal;

                WaveFormCanvas.Width = widthReal;

                createWaveformTileImages(widthReal, zoom);

                ResetPeakLabels();
            }


            double heightReal = WaveFormCanvas.ActualHeight;
            //if (double.IsNaN(heightReal) || (long)Math.Round(heightReal) == 0)
            //{
            //    heightReal = WaveFormCanvas.Height;
            //}

            if (Settings.Default.AudioWaveForm_DisableDraw)
            {
                //#if DEBUG
                //                m_Logger.Log("RefreshUI_LoadWaveForm (skip waveform drawing)", Category.Debug, Priority.Medium);
                //#endif

                m_ViewModel.IsWaveFormLoading = false;
                //m_BackgroundLoader = null;

                CommandManager.InvalidateRequerySuggested();

                if (false && !onlyUpdateTiles)
                {
                    ShowHideWaveFormLoadingMessage(false);
                }
                if (!onlyUpdateTiles)
                {
                    m_ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying);
                }

                return;
            }

            //// The subsequent scroll event will load the tiles
            //// UPDATE: well, not when the previous image was exactly the same size!! :)
            //if (!onlyUpdateTiles)
            //{
            //    m_scrollRefreshNoTimer = true;
            //    return;
            //}

            //
            double widthMagnified = widthReal * zoom;
            double heightMagnified = heightReal * zoom;

            double bytesPerPixel_Magnified = m_ViewModel.State.Audio.DataLength / widthMagnified;

            int byteDepth = m_ViewModel.State.Audio.PcmFormat.Data.BitDepth / 8;
            //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((bytesPerPixel_Magnified
                * Settings.Default.AudioWaveForm_Resolution //m_ViewModel.WaveStepX
                ) / byteDepth);
            samplesPerStep += (samplesPerStep % m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;


            double visibleWidth = WaveFormScroll.ViewportWidth * zoom;
            int nStepsScrollVisibleWidth = (int)Math.Floor((visibleWidth * bytesPerPixel_Magnified) / bytesPerStep);
            long nBytesScrollVisibleWidth = Math.Max(0, nStepsScrollVisibleWidth * bytesPerStep);

            double hoffset = WaveFormScroll.HorizontalOffset * zoom;
            int nStepsScrollOffset = (int)Math.Floor((hoffset * bytesPerPixel_Magnified) / bytesPerStep);
            long nBytesScrollOffset = Math.Max(0, nStepsScrollOffset * bytesPerStep);

            const bool onlyLoadVisibleScroll = true;

            var estimatedCapacity = (int)((onlyLoadVisibleScroll ? visibleWidth : widthMagnified) / (bytesPerStep / bytesPerPixel_Magnified)) + 1;

            //estimatedCapacity * m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 101) //501
            //if (true)
            //{
            bool vertical = WaveFormProgress.Orientation == Orientation.Vertical;
            double sizeProgress = (vertical ? WaveFormScroll.Height : WaveFormScroll.Width);
            if (double.IsNaN(sizeProgress) || sizeProgress <= 0)
            {
                sizeProgress = (vertical ? WaveFormScroll.ActualHeight : WaveFormScroll.ActualWidth);
            }

            //WaveFormProgress.SmallChange = 100;

            double numberOfVisibleXIncrements = sizeProgress / 20;
            // progressbar update will be triggered every xx pixels, which will minimize the Dispatcher access while reading the audio bytes and therefore increase performance.
            double progressStep = estimatedCapacity / numberOfVisibleXIncrements;

            //WaveFormProgress.LargeChange = progressStep;
            m_ProgressVisibleOffset = Math.Floor(progressStep);

            if (false && !onlyUpdateTiles)
            {
                WaveFormProgress.IsIndeterminate = false;
                WaveFormProgress.Value = 0;
                WaveFormProgress.Minimum = 0;
                WaveFormProgress.Maximum = estimatedCapacity;
            }

            if (m_LoadThread != null)
            {
                //Console.WriteLine(@"CancelWaveFormLoad !!!!!!!");
                CancelWaveFormLoad(true);
            }

            m_LoadThreadIsAlive = false;

            DebugFix.Assert(m_LoadThread == null);

            bool bypassThread =
                Math.Min(m_ViewModel.State.Audio.DataLength, nBytesScrollVisibleWidth - nBytesScrollOffset)
                <
                Settings.Default.AudioWaveForm_ThreadBytesThreshold;
            if (bypassThread)
            {
                m_ViewModel.IsWaveFormLoading = true;
                //threadDelegate.Invoke();
                loadWaveFormWrapper(widthMagnified, heightMagnified, wasPlaying, bytesPerPixel_Magnified, zoom, onlyLoadVisibleScroll, nBytesScrollOffset, nBytesScrollVisibleWidth, onlyUpdateTiles);
                return;
            }

            ThreadStart threadDelegate = delegate()
            {
                loadWaveFormWrapper(widthMagnified, heightMagnified, wasPlaying, bytesPerPixel_Magnified, zoom, onlyLoadVisibleScroll, nBytesScrollOffset, nBytesScrollVisibleWidth, onlyUpdateTiles);
            };

            m_LoadThread = new Thread(threadDelegate)
                               {
                                   Name = "Waveform Refresh Thread",
                                   Priority = ThreadPriority.Normal,
                                   IsBackground = true
                               };
            m_LoadThread.Start();

            int count = 0;
            while (!m_LoadThread.IsAlive && !m_LoadThreadIsAlive)
            {
                Console.WriteLine(@"------------ !m_LoadThread.IsAlive && !m_LoadThreadIsAlive: " + count++);
                Thread.Sleep(20);

                if (count > 10)
                {
                    Console.WriteLine(@"------------ BREAK !m_LoadThread.IsAlive && !m_LoadThreadIsAlive: " + count++);
                    m_ViewModel.IsWaveFormLoading = false;
                    return;
                }
                //count++;
                //if (count > 3)
                //{
                //    //Console.WriteLine(@" LOOP COUNT not m_LoadThread.IsAlive");
                //    count = 0;
                //    if (!isAlive)
                //    {
                //        //Console.WriteLine(@" LOOP FORCE OUT m_LoadThread.IsAlive");
                //        break;
                //    }
                //}
            }
            //Console.WriteLine(@"OK m_LoadThread.IsAlive");

            m_ViewModel.IsWaveFormLoading = true;
        }

        private void loadWaveFormWrapper(double widthMagnified, double heightMagnified, bool wasPlaying, double bytesPerPixel_Magnified, double zoom, bool onlyLoadVisibleScroll, long nBytesScrollOffset, long nBytesScrollVisibleWidth, bool onlyUpdateTiles)
        {
            m_LoadThreadIsAlive = true;
            try
            {
                //Console.WriteLine(@"BEFORE loadWaveForm");

                loadWaveForm(widthMagnified, heightMagnified, wasPlaying, bytesPerPixel_Magnified, zoom, onlyLoadVisibleScroll, nBytesScrollOffset, nBytesScrollVisibleWidth, onlyUpdateTiles);

                //Console.WriteLine(@"AFTER loadWaveForm");
            }
            catch (ThreadAbortException ex)
            {
                // cancelled brutally...let's get out of here !
                //Console.WriteLine("CATCH ThreadAbortException");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("CATCH All");
#if DEBUG
                Debugger.Break();
#endif
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        ExceptionHandler.Handle(ex, false, m_ShellView)));
                }
                else
                {
                    ExceptionHandler.Handle(ex, false, m_ShellView);
                }
            }
            finally
            {
                //Console.WriteLine(@">>>> SEND BEFORE 1");

                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(() =>
                    {
                        //Console.WriteLine(@">>>> SEND IsWaveFormLoading");
                        m_ViewModel.IsWaveFormLoading = false;
                        m_LoadThreadIsAlive = false;
                    }));
                }
                else
                {
                    m_ViewModel.IsWaveFormLoading = false;
                    m_LoadThreadIsAlive = false;
                }
                //Console.WriteLine(@">>>> SEND BEFORE 2");

                if (!onlyUpdateTiles
                    //|| m_scrollRefreshNoTimer
                    )
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                               (Action)
                                               (() =>
                                                m_ViewModel.
                                                    AudioPlayer_PlayAfterWaveFormLoaded(
                                                        wasPlaying)));
                    }
                    else
                    {
                        m_ViewModel.
                            AudioPlayer_PlayAfterWaveFormLoaded(
                                wasPlaying);
                    }
                }
                //Console.WriteLine(@">>>> SEND BEFORE 3");
                if (false && !onlyUpdateTiles)
                {

                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            WaveFormProgress
                                .
                                IsIndeterminate
                                =
                                true;
                            TimeMessageHide
                                ();
                            ShowHideWaveFormLoadingMessage
                                (false);
                            m_ViewModel
                                .
                                m_TimeStringOther
                                =
                                String
                                    .
                                    Empty;
                        }));
                    }
                    else
                    {
                        WaveFormProgress
                                                                                                   .
                                                                                                   IsIndeterminate
                                                                                                   =
                                                                                                   true;
                        TimeMessageHide
                            ();
                        ShowHideWaveFormLoadingMessage
                            (false);
                        m_ViewModel
                            .
                            m_TimeStringOther
                            =
                            String
                                .
                                Empty;
                    }
                }

                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        CommandManager
                            .
                            InvalidateRequerySuggested
                            ();
                    }));
                }
                else
                {
                    CommandManager
                                                                                              .
                                                                                              InvalidateRequerySuggested
                                                                                              ();
                }

                //Console.WriteLine(@">>>> SEND AFTER 3");
            }
            m_LoadThreadIsAlive = false;
        }

        private void loadWaveForm(double widthMagnified, double heightMagnified, bool wasPlaying, double bytesPerPixel_Magnified, double zoom,
            bool onlyLoadVisibleScroll, long nBytesScrollOffset, long nBytesScrollVisibleWidth, bool onlyUpdateTiles)
        {
            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            int byteDepth = m_ViewModel.State.Audio.PcmFormat.Data.BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((bytesPerPixel_Magnified
                * Settings.Default.AudioWaveForm_Resolution //m_ViewModel.WaveStepX
                ) / byteDepth);
            samplesPerStep += (samplesPerStep % m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);

            if (samplesPerStep <= 0) return;

            int bytesPerStep = samplesPerStep * byteDepth;


            //#if DEBUG
            //            m_Logger.Log("loadWaveForm - byte buffer size:" + bytesPerStep, Category.Debug, Priority.Medium);
            //#endif


            var bytes = new byte[bytesPerStep]; // Int 8 unsigned


            const bool bJoinInterSamples = false;

            if (m_CancelRequested) return;

            Stopwatch stopWatch = null;

            LightLinkedList<ImageAndDrawing>.Item imageTileFirst = m_WaveformTileImages.m_First;
            LightLinkedList<ImageAndDrawing>.Item imageTileLast = null;
            if (onlyLoadVisibleScroll)
            {
                imageTileFirst = null;
                LightLinkedList<ImageAndDrawing>.Item current = m_WaveformTileImages.m_First;
                while (current != null)
                {
                    ImageAndDrawing imgAndDraw = current.m_data;
                    if (nBytesScrollOffset >= imgAndDraw.m_originalX * BytesPerPixel
                        &&
                        nBytesScrollOffset <
                        (imgAndDraw.m_originalX + imgAndDraw.m_originalW) * BytesPerPixel
                        )
                    {
                        imageTileFirst = current;
                        break;
                    }
                    current = current.m_nextItem;
                }

                if (imageTileFirst == null)
                {
                    return;
                }

                current = imageTileFirst;
                while (current != null)
                {
                    ImageAndDrawing imgAndDraw = current.m_data;
                    if (!imgAndDraw.m_imageSourceLoaded
                        &&
                        imgAndDraw.m_originalX * BytesPerPixel < (nBytesScrollOffset + nBytesScrollVisibleWidth)
                        )
                    {
                        imageTileFirst = current;
                        break;
                    }
                    current = current.m_nextItem;
                }
                if (imageTileFirst.m_data.m_imageSourceLoaded)
                {
                    return;
                }

                bool atLeastOneNeedsLoading = false;
                current = imageTileFirst;
                while (current != null)
                {
                    ImageAndDrawing imgAndDraw = current.m_data;
                    if (!imgAndDraw.m_imageSourceLoaded)
                    {
                        atLeastOneNeedsLoading = true;
                    }
                    long rightByte = nBytesScrollOffset + nBytesScrollVisibleWidth;

                    if (Math.Floor(imgAndDraw.m_originalX * BytesPerPixel) >= rightByte)
                    {
                        imageTileLast = current.m_previousItem;
                        break;
                    }

                    if (current.m_nextItem == null ||
                        ((imgAndDraw.m_originalX + imgAndDraw.m_originalW) * BytesPerPixel) >= rightByte
                        )
                    {
                        imageTileLast = current;
                        break;
                    }

                    current = current.m_nextItem;
                }

                if (!atLeastOneNeedsLoading)
                {
                    return;
                }

                current = imageTileLast;
                while (current != null)
                {
                    if (current == imageTileFirst)
                    {
                        break;
                    }
                    ImageAndDrawing imgAndDraw = current.m_data;
                    if (!imgAndDraw.m_imageSourceLoaded)
                    {
                        break;
                    }
                    current = current.m_previousItem;
                }
                imageTileLast = current;
            }

            long totalRead = onlyLoadVisibleScroll ?
                m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                (long)Math.Round(imageTileFirst.m_data.m_originalX * BytesPerPixel)
                )
                : 0;

            double x = 0; // initial pixel offset
            x += (totalRead / bytesPerPixel_Magnified); //ViewModel.WaveStepX;

            double lastXdrawn = x;

            const int tolerance = 5;
            try
            {
                Stream audioStream = m_ViewModel.AudioPlayer_GetWaveformAudioStream();
                //audioStream = m_ViewModel.State.Audio.PlayStream

                if (m_CancelRequested) return;

                audioStream.Position = totalRead;
                audioStream.Seek(totalRead, SeekOrigin.Begin);

                /*if (!string.IsNullOrEmpty(ViewModel.FilePath))
                {
                    ViewModel.AudioPlayer_ResetPlayStreamPosition();
                }*/
                double dBMinReached = double.PositiveInfinity;
                double dBMaxReached = double.NegativeInfinity;
                double decibelDrawDelta = (m_ViewModel.IsUseDecibelsNoAverage ? 0 : 2);

                //Amplitude ratio (or Sound Pressure Level):
                //decibels = 20 * log10(ratio);

                //Power ratio (or Sound Intensity Level):
                //decibels = 10 * log10(ratio);

                //10 * log(ratio^2) is exactly the same as 20 * log(ratio).

                const bool bUseDecibelsIntensity = false; // feature removed: no visible changes
#pragma warning disable 162
                const double logFactor = (bUseDecibelsIntensity ? 10 : 20);
#pragma warning restore 162

                double reference = short.MaxValue; // Int 16 signed 32767 (0 dB reference value)
                double adjustFactor = m_ViewModel.DecibelResolution;
                if (adjustFactor != 0)
                {
                    reference *= adjustFactor;
                    //0.707 adjustment to more realistic noise floor value, to avoid clipping (otherwise, use MinValue = -45 or -60 directly)
                }

                double dbMinValue = logFactor * Math.Log10(1.0 / reference); //-90.3 dB
                //double val = reference*Math.Pow(10, MinValue/20); // val == 1.0, just checking

                //System.Diagnostics.Debug.Print(dbMinValue + "");

                double dBMinHardCoded = dbMinValue;

                double dbMaxValue = (m_ViewModel.IsUseDecibelsNoAverage ? -dbMinValue : 0);


                //bool isLittleEndian = BitConverter.IsLittleEndian;

                double sumProgress = 0;

                if (m_CancelRequested) return;

                stopWatch = Stopwatch.StartNew();
                stopWatch.Start();

#if ENABLE_INCREMENTAL_WAVEFORM_DRAWING
                bool drawnOnce = false;  
#endif //ENABLE_INCREMENTAL_WAVEFORM_DRAWING

                #region LOOP

                int read;

                // ONLY used for Settings.Default.AudioWaveForm_IsBordered
                // ---
                List<Point> listTopPointsCh1 = null;
                List<Point> listBottomPointsCh1 = null;

                List<Point> listTopPointsCh2 = null;
                List<Point> listBottomPointsCh2 = null;

                // ---

                //// ONLY used for Settings.Default.AudioWaveForm_IsStroked
                //// --
                //StreamGeometry geometryCh1 = null;
                //StreamGeometryContext sgcCh1 = null;

                //StreamGeometry geometryCh2 = null;
                //StreamGeometryContext sgcCh2 = null;

                //bool firstY1 = true;
                //bool firstY1_ = true;
                //// --

                LightLinkedList<ImageAndDrawing>.Item currentImageTile = null;

                Tuple<TreeNode, TreeNode> treeNodeSelection = m_ViewModel.m_UrakawaSession.GetTreeNodeSelection();

                while ((read = audioStream.Read(bytes, 0, bytesPerStep)) >= 0)
                {
                    totalRead += read;

                    double xNotMagnified = lastXdrawn / zoom;

                    double currentRightXLimit = -1;
                    if (currentImageTile != null)
                    {
                        currentRightXLimit = currentImageTile.m_data.m_originalX + currentImageTile.m_data.m_originalW;
                    }

                    if (currentImageTile == null
                        || xNotMagnified >= currentRightXLimit
                        || read == 0)
                    {
                        //draw current

                        if (currentImageTile != null && !currentImageTile.m_data.m_imageSourceLoaded)
                        {
                            if (m_CancelRequested && m_CancelInterruptDrawingToo) return;

                            //adjust tile size

                            double overflow = xNotMagnified - currentRightXLimit;
                            if (overflow > 0)
                            {
                                currentImageTile.m_data.m_originalW += overflow;

                                if (currentImageTile.m_nextItem != null)
                                {
                                    currentImageTile.m_nextItem.m_data.m_originalW -= overflow;
                                    currentImageTile.m_nextItem.m_data.m_originalX += overflow;

                                    if (currentImageTile.m_nextItem.m_data.m_originalW <= 0)
                                    {
#if DEBUG
                                        Debugger.Break();
#endif //DEBUG
                                        currentImageTile.m_nextItem.m_data.m_originalW = 1;
                                    }
                                }

                                //#if DEBUG
                                //                                m_Logger.Log("loadWaveFor, overflow (1):" + overflow, Category.Debug, Priority.Medium);
                                //#endif

                                Action deleg = () =>
                                                   {
                                                       currentImageTile.m_data.m_image.Width += overflow;

                                                       if (currentImageTile.m_nextItem != null)
                                                       {
                                                           double newW = currentImageTile.m_nextItem.m_data.m_image.Width - overflow;
                                                           if (newW <= 0)
                                                           {
#if DEBUG
                                                               Debugger.Break();
#endif //DEBUG
                                                               newW = 1;
                                                           }
                                                           currentImageTile.m_nextItem.m_data.m_image.Width = newW;

                                                           double left =
                                                               (double)
                                                               currentImageTile.m_nextItem.m_data.m_image.GetValue(
                                                                   Canvas.LeftProperty);
                                                           currentImageTile.m_nextItem.m_data.m_image.SetValue(
                                                               Canvas.LeftProperty, left + overflow);
                                                       }

                                                       //#if DEBUG
                                                       //                                    m_Logger.Log("loadWaveFor, overflow (2):" + overflow, Category.Debug, Priority.Medium);
                                                       //#endif
                                                   };

                                if (!Dispatcher.CheckAccess())
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, deleg);
                                }
                                else
                                {
                                    deleg.Invoke();
                                }
                            }

                            double wZoomed = currentImageTile.m_data.m_originalW * zoom;

                            drawWaveForm(
                                currentImageTile.m_data,
                                //audioStream,
                                true,
                                //sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                                listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                                heightMagnified, wZoomed, //wZoomed > 0 ? wZoomed : widthMagnified, // widthMagnified,
                                dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance,
                                bytesPerPixel_Magnified, zoom, treeNodeSelection);
                        }

                        if (
                            //currentImageTile != imageTileLast &&
                            //x > widthMagnified ||
                            (onlyLoadVisibleScroll && imageTileLast != null
                            // && totalRead > (nBytesScrollOffset + nBytesScrollVisibleWidth)
                             &&
                             (totalRead - read) >
                             (imageTileLast.m_data.m_originalX + imageTileLast.m_data.m_originalW) * BytesPerPixel
                            //m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                            ))
                        {
                            break;
                        }

                        if (read == 0)
                        {
                            break;
                        }

                        // go to next image tile

                        if (currentImageTile == null)
                        {
                            currentImageTile = imageTileFirst;
                        }
                        else
                        {
                            currentImageTile = currentImageTile.m_nextItem;

                            if (currentImageTile == null)
                            {
                                break;
                            }
                        }

                        //reset points geomertries

                        if (!currentImageTile.m_data.m_imageSourceLoaded
                            //&& (Settings.Default.AudioWaveForm_IsBordered || Settings.Default.AudioWaveForm_IsStroked)
                            )
                        //m_ViewModel.IsEnvelopeVisible)
                        {
                            Point p = default(Point);

                            Point listBottomPointsCh1_LAST = p;
                            Point listTopPointsCh1_LAST = p;

                            Point listBottomPointsCh2_LAST = p;
                            Point listTopPointsCh2_LAST = p;

                            if (currentImageTile != imageTileFirst)
                            {
                                listBottomPointsCh1_LAST = listBottomPointsCh1[listBottomPointsCh1.Count - 1];
                                listBottomPointsCh1_LAST.X = 0;
                                listTopPointsCh1_LAST = listTopPointsCh1[listTopPointsCh1.Count - 1];
                                listTopPointsCh1_LAST.X = 0;

                                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                                {
                                    listBottomPointsCh2_LAST = listBottomPointsCh2[listBottomPointsCh2.Count - 1];
                                    listBottomPointsCh2_LAST.X = 0;
                                    listTopPointsCh2_LAST = listTopPointsCh2[listTopPointsCh2.Count - 1];
                                    listTopPointsCh2_LAST.X = 0;
                                }
                            }

                            var estimatedCapacity = (int)((
                                //onlyLoadVisibleScroll ?
                                //visibleWidth * zoom :
                                                           currentImageTile.m_data.m_originalW * zoom //widthMagnified
                                                           ) / (bytesPerStep / bytesPerPixel_Magnified)) + 2;

                            listTopPointsCh1 = new List<Point>(estimatedCapacity);
                            listBottomPointsCh1 = new List<Point>(estimatedCapacity);

                            if (currentImageTile != imageTileFirst)
                            {
                                listTopPointsCh1.Add(listTopPointsCh1_LAST);
                                listBottomPointsCh1.Add(listBottomPointsCh1_LAST);
                            }

                            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                            {
                                listTopPointsCh2 = new List<Point>(estimatedCapacity);
                                listBottomPointsCh2 = new List<Point>(estimatedCapacity);

                                if (currentImageTile != imageTileFirst)
                                {
                                    listTopPointsCh2.Add(listTopPointsCh2_LAST);
                                    listBottomPointsCh2.Add(listBottomPointsCh2_LAST);
                                }
                            }
                            //else
                            //{
                            //    listTopPointsCh2 = new List<Point>(1);
                            //    listBottomPointsCh2 = new List<Point>(1);
                            //}
                        }

                        //if (!currentImageTile.m_data.m_imageSourceLoaded && Settings.Default.AudioWaveForm_IsStroked)
                        //{
                        //    firstY1 = true;
                        //    firstY1_ = true;

                        //    geometryCh1 = new StreamGeometry();
                        //    sgcCh1 = geometryCh1.Open();

                        //    if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                        //    {
                        //        geometryCh2 = new StreamGeometry();
                        //        sgcCh2 = geometryCh2.Open();
                        //    }
                        //}
                    }


                    if (m_CancelRequested) break;

                    if (!currentImageTile.m_data.m_imageSourceLoaded)
                    {
                        for (int channel = 0; channel < m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels; channel++)
                        {
                            int limit = samplesPerStep;

                            if (read < bytesPerStep)
                            {
                                var nSamples = (int)Math.Floor((double)read / byteDepth);

                                nSamples = m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels *
                                           (int)
                                           Math.Floor((double)nSamples /
                                                      m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);
                                limit = nSamples;
                                limit = Math.Min(limit, samplesPerStep);
                            }

                            long total = 0;
                            int nSamplesRead = 0;

                            long min_ = short.MaxValue; // Int 16 signed 32767
                            long max_ = short.MinValue; // Int 16 signed -32768

                            for (int i = channel; i < limit; i += m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels)
                            {
                                nSamplesRead++;


                                int index = i << 1;
                                if (index >= bytes.Length)
                                {
#if DEBUG
                                    Debugger.Break();
#endif
                                    // DEBUG
                                    break;
                                }

                                byte byte1 = bytes[index];
                                byte byte2 = bytes[index + 1];
                                var sample =
                                    BitConverter.IsLittleEndian
                                    ? (short)(byte1 | (byte2 << 8))
                                    : (short)((byte1 << 8) | byte2);




                                if (sample == short.MinValue)
                                {
                                    total += short.MaxValue + 1;
                                }
                                else
                                {
                                    total += Math.Abs(sample);
                                }

                                if (sample < min_)
                                {
                                    min_ = sample;
                                }
                                if (sample > max_)
                                {
                                    max_ = sample;
                                }
                            }

                            double hh = heightMagnified;
                            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                            {
                                hh /= 2;
                            }

                            // ReSharper disable RedundantAssignment
                            double y1 = 0.0;
                            double y2 = 0.0;
                            // ReSharper restore RedundantAssignment


                            #region DECIBELS

                            if (Settings.Default.AudioWaveForm_UseDecibels)
                            {
                                double mindB = min_;
                                double maxdB = max_;

                                if (!m_ViewModel.IsUseDecibelsNoAverage)
                                {
                                    mindB = total / (double)nSamplesRead; //AVERAGE
                                    maxdB = mindB;
                                }

                                bool minIsNegative = mindB < 0;
                                double minAbs = Math.Abs(mindB);
                                if (minAbs == 0)
                                {
                                    mindB = (m_ViewModel.IsUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                                }
                                else
                                {
                                    mindB = logFactor * Math.Log10(minAbs / reference);
                                    dBMinReached = Math.Min(dBMinReached, mindB);
                                    if (m_ViewModel.IsUseDecibelsNoAverage && !minIsNegative)
                                    {
                                        mindB = -mindB;
                                    }
                                }

                                bool maxIsNegative = maxdB < 0;
                                double maxAbs = Math.Abs(maxdB);
                                if (maxAbs == 0)
                                {
                                    maxdB = (m_ViewModel.IsUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                                }
                                else
                                {
                                    maxdB = logFactor * Math.Log10(maxAbs / reference);
                                    dBMaxReached = Math.Max(dBMaxReached, maxdB);
                                    if (m_ViewModel.IsUseDecibelsNoAverage && !maxIsNegative)
                                    {
                                        maxdB = -maxdB;
                                    }
                                }

                                double totalDbRange = dbMaxValue - dbMinValue;
                                double pixPerDbUnit = hh / totalDbRange;

                                if (m_ViewModel.IsUseDecibelsNoAverage)
                                {
                                    mindB = dbMinValue - mindB;
                                }
                                y1 = pixPerDbUnit * (mindB - dbMinValue) + decibelDrawDelta;
                                if (!m_ViewModel.IsUseDecibelsNoAverage)
                                {
                                    y1 = hh - y1;
                                }
                                if (m_ViewModel.IsUseDecibelsNoAverage)
                                {
                                    maxdB = dbMaxValue - maxdB;
                                }
                                y2 = pixPerDbUnit * (maxdB - dbMinValue) - decibelDrawDelta;
                                if (!m_ViewModel.IsUseDecibelsNoAverage)
                                {
                                    y2 = hh - y2;
                                }
                            }
                            #endregion DECIBELS

                            else
                            {
                                const short MaxValue = short.MaxValue; // Int 16 signed 32767
                                const short MinValue = short.MinValue; // Int 16 signed -32768

                                double pixPerUnit = hh /
                                                    (MaxValue - MinValue); // == ushort.MaxValue => Int 16 unsigned 65535

                                y1 = pixPerUnit * (min_ - MinValue);
                                y1 = hh - y1;
                                y2 = pixPerUnit * (max_ - MinValue);
                                y2 = hh - y2;
                            }

                            if (!(Settings.Default.AudioWaveForm_UseDecibels && m_ViewModel.IsUseDecibelsAdjust))
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

                            lastXdrawn = x;
                            double xTile = x - zoom * currentImageTile.m_data.m_originalX;

                            if (channel == 0)
                            {
                                var p1 = new Point(xTile, y1);

                                if (
                                    //(Settings.Default.AudioWaveForm_IsBordered || Settings.Default.AudioWaveForm_IsStroked) &&
                                     listTopPointsCh1 != null)
                                {
                                    listTopPointsCh1.Add(p1);
                                }
                                //if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
                                //{
                                //    if (firstY1)
                                //    {
                                //        sgcCh1.BeginFigure(p1, false, false);
                                //        firstY1 = false;
                                //    }
                                //    else
                                //    {
                                //        sgcCh1.LineTo(p1, bJoinInterSamples, false);
                                //    }
                                //}
                            }
                            else if (listTopPointsCh2 != null)
                            {
                                y1 += hh;
                                var p2 = new Point(xTile, y1);

                                if (
                                    //(Settings.Default.AudioWaveForm_IsBordered || Settings.Default.AudioWaveForm_IsStroked) &&
                                    listTopPointsCh2 != null)
                                {
                                    listTopPointsCh2.Add(p2);
                                }
                                //if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
                                //{
                                //    if (firstY1_)
                                //    {
                                //        sgcCh2.BeginFigure(p2, false, false);
                                //        firstY1_ = false;
                                //    }
                                //    else
                                //    {
                                //        sgcCh2.LineTo(p2, bJoinInterSamples, false);
                                //    }
                                //}
                            }

                            if (channel == 0)
                            {
                                var p3 = new Point(xTile, y2);

                                //if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
                                //{
                                //    sgcCh1.LineTo(p3, true, false);
                                //}
                                if (
                                    //(Settings.Default.AudioWaveForm_IsBordered || Settings.Default.AudioWaveForm_IsStroked) &&
                                    listBottomPointsCh1 != null)
                                {
                                    listBottomPointsCh1.Add(p3);
                                }
                            }
                            else if (listTopPointsCh2 != null)
                            {
                                y2 += hh;
                                var p4 = new Point(xTile, y2);

                                //if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
                                //{
                                //    sgcCh2.LineTo(p4, true, false);
                                //}
                                if (
                                    //(Settings.Default.AudioWaveForm_IsBordered || Settings.Default.AudioWaveForm_IsStroked) &&
                                    listBottomPointsCh2 != null)
                                {
                                    listBottomPointsCh2.Add(p4);
                                }
                            }
                        }
                    }
                    else
                    {
                        lastXdrawn = 0;
                    }
                    if (m_CancelRequested) break;

#if ENABLE_INCREMENTAL_WAVEFORM_DRAWING

                    if (!drawnOnce && totalRead > (nBytesScrollOffset + nBytesScrollVisibleWidth))
                    {
                        drawnOnce = true;
#if DEBUG
                        m_Logger.Log("loadWavForm TOTAL READ:  " + totalRead + " => [" + nBytesScrollOffset + "," + (nBytesScrollOffset + nBytesScrollVisibleWidth) + "]", Category.Debug, Priority.Medium);
#endif

                        drawWaveForm(
                            imageAndDraw,
                            //audioStream,
                            false,
                            sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                            listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                            heightMagnified, widthMagnified,
                            dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance,
                            bytesPerPixel_Magnified, zoom);
                    }
#endif //ENABLE_INCREMENTAL_WAVEFORM_DRAWING

                    if (m_CancelRequested) break;

                    sumProgress++;
                    if (sumProgress >= m_ProgressVisibleOffset)
                    {
                        sumProgress = 0;

                        if (false && !Dispatcher.CheckAccess() && !onlyUpdateTiles)
                        {
                            var deleg = (Action)(() =>
                                                      {
                                                          long timeInLocalUnits
                                                              =
                                                              m_ViewModel.State.
                                                                  Audio.
                                                                  GetCurrentPcmFormat
                                                                  ().Data.
                                                                  ConvertBytesToTime
                                                                  (totalRead);

                                                          m_ViewModel.
                                                              m_TimeStringOther
                                                              =
                                                              AudioPaneViewModel
                                                                  .
                                                                  FormatTimeSpan_Units
                                                                  (timeInLocalUnits);
                                                          TimeMessageShow();
                                                          //TimeMessageRefresh();

                                                          WaveFormProgress.Value
                                                              +=
                                                              m_ProgressVisibleOffset;
                                                      });

                            if (!Dispatcher.CheckAccess())
                            {
                                DispatcherOperation op = Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                                               deleg);
                            }
                            else
                            {
                                deleg.Invoke();
                            }
                        }
                    }

                    x += (read / bytesPerPixel_Magnified); //ViewModel.WaveStepX;
                    DebugFix.Assert(x <= widthMagnified + 3 * zoom); // 3px tolerance, to avoid false positives due to rounding imprecisions

                    if (m_CancelRequested) break;
                }

                if (stopWatch != null && stopWatch.IsRunning)
                {
                    stopWatch.Stop();
                    //m_Logger.Log("loadWavForm 1:  " + stopWatch.ElapsedMilliseconds, Category.Debug, Priority.Medium);
                }

                #endregion LOOP

                if (false && !Dispatcher.CheckAccess() && !onlyUpdateTiles)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                                                                                        {
                                                                                            WaveFormProgress.
                                                                                                IsIndeterminate = true;
                                                                                            m_ViewModel.
                                                                                                m_TimeStringOther =
                                                                                                String.Empty;
                                                                                            TimeMessageHide();
                                                                                        }));
                    }
                    else
                    {
                        WaveFormProgress.
                            IsIndeterminate = true;
                        m_ViewModel.
                            m_TimeStringOther =
                            String.Empty;
                        TimeMessageHide();
                    }
                }
            }
            finally
            {
                if (stopWatch != null && stopWatch.IsRunning)
                {
                    stopWatch.Stop();
                    //m_Logger.Log("loadWavForm 2:  " + stopWatch.ElapsedMilliseconds, Category.Debug, Priority.Medium);
                }
            }
        }

        private void drawWaveForm(
            ImageAndDrawing imageAndDraw,
            //Stream audioStream,
            bool freeze,
            //StreamGeometryContext sgcCh1, StreamGeometryContext sgcCh2,
            //StreamGeometry geometryCh1, StreamGeometry geometryCh2,
            List<Point> listBottomPointsCh1, List<Point> listBottomPointsCh2,
            List<Point> listTopPointsCh1, List<Point> listTopPointsCh2,
            double heightMagnified, double widthMagnified,
            double dBMinHardCoded, double dBMinReached, double dBMaxReached,
            double decibelDrawDelta, int tolerance,
            double bytesPerPixel_Magnified, double zoom,
            Tuple<TreeNode, TreeNode> treeNodeSelection)
        {
            //Console.WriteLine(@"Drawing waveform...1");

            //#if DEBUG
            //            m_Logger.Log("drawWaveForm", Category.Debug, Priority.Medium);
            //#endif

            DrawingGroupComponents drawGrpCps = drawWaveFormUsingCollectedPoints(
                imageAndDraw,
                //audioStream,
                freeze,
                //sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                heightMagnified, widthMagnified,
                dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified, zoom,
                treeNodeSelection);


            var drawGrp = new DrawingGroup();

            if (drawGrpCps.back != null)
            {
                drawGrp.Children.Add(drawGrpCps.back);
            }

            // HAS BORDER
            if (Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.Border
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderBars
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                )
            {
                // HAS FILL
                if (
                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                    )
                {
                    if (drawGrpCps.enveloppeCh1 != null)
                    {
                        drawGrp.Children.Add(drawGrpCps.enveloppeCh1);
                    }

                    // HAS BARS
                    if (Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                        && freeze)
                    {
                        drawGrp.Children.Add(drawGrpCps.verticalStrokesCh1);
                    }
                }

                // HAS NO FILL
                else
                {
                    // HAS BARS
                    if (
                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderBars
                        && freeze)
                    {
                        drawGrp.Children.Add(drawGrpCps.verticalStrokesCh1);
                    }
                    if (drawGrpCps.enveloppeCh1 != null)
                    {
                        drawGrp.Children.Add(drawGrpCps.enveloppeCh1);
                    }
                }
            }

                // HAS NO BORDER
            else if (
                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.Bars
                && freeze)
            {
                drawGrp.Children.Add(drawGrpCps.verticalStrokesCh1);
            }



            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
            {
                if (
                    Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.Border
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderBars
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                    )
                {
                    // HAS FILL
                    if (
                    Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill
                    || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                        )
                    {
                        if (drawGrpCps.enveloppeCh2 != null)
                        {
                            drawGrp.Children.Add(drawGrpCps.enveloppeCh2);
                        }

                        // HAS BARS
                        if (Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                            && drawGrpCps.verticalStrokesCh2 != null && freeze)
                        {
                            drawGrp.Children.Add(drawGrpCps.verticalStrokesCh2);
                        }
                    }

                    // HAS NO FILL
                    else
                    {
                        // HAS BARS
                        if (
                    Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderBars
                            && drawGrpCps.verticalStrokesCh2 != null && freeze)
                        {
                            drawGrp.Children.Add(drawGrpCps.verticalStrokesCh2);
                        }
                        if (drawGrpCps.enveloppeCh2 != null)
                        {
                            drawGrp.Children.Add(drawGrpCps.enveloppeCh2);
                        }
                    }
                }

                // HAS NO BORDER
                else if (
                    Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.Bars
                    && drawGrpCps.verticalStrokesCh2 != null && freeze)
                {
                    drawGrp.Children.Add(drawGrpCps.verticalStrokesCh2);
                }
            }

            if (treeNodeSelection.Item1 != null && drawGrpCps.markers != null)
            {
                drawGrp.Children.Add(drawGrpCps.markers);
            }

#if DEBUG && DRAW_SUBSTREAMS
            
            if (geoDrawStreamSubs != null)
            {
                drawGrp.Children.Add(geoDrawStreamSubs);
            }
#endif

            /*
            double m_offsetFixX = 0;
            m_offsetFixX = drawGrp.Bounds.Width - width;

            double m_offsetFixY = 0;
            m_offsetFixY = drawGrp.Bounds.Height - height;

            if (bAdjustOffsetFix && (m_offsetFixX != 0 || m_offsetFixY != 0))
            {
                TransformGroup trGrp = new TransformGroup();
                //trGrp.Children.Add(new TranslateTransform(-drawGrp.Bounds.Left, -drawGrp.Bounds.Top));
                trGrp.Children.Add(new ScaleTransform(width / drawGrp.Bounds.Width, height / drawGrp.Bounds.Height));
                drawGrp.Transform = trGrp;
            }*/


            //if (m_ViewModel.State.Audio.PlayStreamMarkers != null
            //    && m_ViewModel.State.Audio.PlayStreamMarkers.Count > Settings.Default.AudioWaveForm_TextCacheRenderThreshold)
            //{
            //    //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            //    //var zoom = (m_ShellView != null
            //    //                ? m_ShellView.MagnificationLevel
            //    //                : (Double)FindResource("MagnificationLevel"));

            //    //DrawingContext dc = drawGrp.Open();

            //    //var imageDrawing = new ImageDrawing();
            //    //imageDrawing.Rect = null;

            //    //var drawingImage = new DrawingImage();
            //    //drawingImage.Drawing = imageDrawing;

            //    //var drawGroup = new DrawingGroup();
            //    //drawGroup.Children.Add(imageDrawing);

            //    m_WaveFormTimeTicksAdorner.drawChunkInfos(imageAndDraw, null, drawGrp, 0, heightMagnified, imageAndDraw.m_originalCanvasW * zoom, bytesPerPixel_Magnified, zoom);
            //    //dc.Close();

            //    //drawGrp.Children.Add(imageDrawing);
            //}

            drawGrp.Freeze();




            var drawGrpResize = new DrawingGroup();

            if (drawGrpCps.back != null)
            {
                drawGrpResize.Children.Add(drawGrpCps.back);
            }
            if (drawGrpCps.verticalStrokesCh1 != null)
            {
                drawGrpResize.Children.Add(drawGrpCps.verticalStrokesCh1);
                if (drawGrpCps.verticalStrokesCh2 != null)
                {
                    drawGrpResize.Children.Add(drawGrpCps.verticalStrokesCh2);
                }
            }

            drawGrpResize.Freeze();


            //Console.WriteLine(@"Drawing waveform...2");

            if (freeze)
            {
                if (useVectorResize())
                {
                    var drawImg = new DrawingImage(drawGrpResize);
                    drawImg.Freeze();
                    imageAndDraw.m_drawingImage = drawImg;
                }

                //Console.WriteLine(@"Drawing waveform...freeze2");

                //GC.Collect();
                //GC.WaitForFullGCComplete(1000);

                Action del = () =>
                {

                    //#if DEBUG
                    //                    m_Logger.Log("CACHE WAVEFORM WIDTH = " +
                    //                                                   imageAndDraw.m_image.Width + "-" + imageAndDraw.m_originalW + " // " +
                    //                                                   imageAndDraw.m_image.ActualWidth + " # " + imageAndDraw.m_originalCanvasW, Category.Debug, Priority.Medium);
                    //#endif

                    if (useVectorResize()
                                       && imageAndDraw.m_image.Stretch == Stretch.Fill
                                       && imageAndDraw.m_drawingImage != null
                                       )
                    {
                        imageAndDraw.m_image.Source = imageAndDraw.m_drawingImage;
                        return;
                    }

                    bool drawNET3 = false;
#if NET40
                    if (imageAndDraw.m_image.Width <= 2048) // should always work because waveform tiles are small
                    {
                        //if (useVectorResize() && imageAndDraw.m_drawingImage != null)
                        //{
                        //    imageAndDraw.m_image.Source = imageAndDraw.m_drawingImage;
                        //    imageAndDraw.m_imageSourceLoaded = true;
                        //}
                        //else
                        //{
                        //    var drawImg = new DrawingImage(drawGrpResize);
                        //    drawImg.Freeze();
                        //    imageAndDraw.m_image.Source = drawImg;
                        //    imageAndDraw.m_imageSourceLoaded = true;
                        //}

                        var drawImg = new DrawingImage(drawGrp);
                        drawImg.Freeze();
                        imageAndDraw.m_image.Source = drawImg;
                        imageAndDraw.m_imageSourceLoaded = true;

                        //var zoom = (m_ShellView != null
                        //                ? m_ShellView.MagnificationLevel
                        //                : (Double)FindResource("MagnificationLevel"));

                        if (
                            imageAndDraw.m_image.CacheMode == null
                            ||
                            (long)Math.Round(
                            ((BitmapCache)imageAndDraw.m_image.CacheMode).RenderAtScale
                            * 100
                            )
                            !=
                            (long)Math.Round(
                            zoom
                            * 100
                            )
                            )
                        {
                            imageAndDraw.m_image.CacheMode = null;
                            //WaveFormImage.UseLayoutRounding = true;
                            imageAndDraw.m_image.CacheMode = new BitmapCache
                                                          {
                                                              RenderAtScale = zoom,
                                                              EnableClearType = false,
                                                              SnapsToDevicePixels = false
                                                          };
#if false && DEBUG
                        var bitmapCacheBrush = new BitmapCacheBrush
                        {
                            AutoLayoutContent = false,
                            Target = WaveFormImage,
                            BitmapCache = new BitmapCache
                            {
                                RenderAtScale = 0.3,
                                EnableClearType = false,
                                SnapsToDevicePixels = false
                            }
                        };
                        var imageTooltip = new Rectangle
                        {
                            Width = WaveFormImage.Width * bitmapCacheBrush.BitmapCache.RenderAtScale,
                            Height = WaveFormImage.Height * bitmapCacheBrush.BitmapCache.RenderAtScale,
                            Fill = bitmapCacheBrush
                        };
                        ZoomSlider.ToolTip = imageTooltip;
#endif
                        }
                    }
                    else
                    {
                        imageAndDraw.m_image.CacheMode = null;
                        drawNET3 = true;
                    }
#else
                                     drawNET3 = true;
#endif // ELSE NET40

                    if (drawNET3)
                    {
                        WaveFormRenderMethod renderMethod = Settings.Default.AudioWaveForm_RenderMethod;

                        if (renderMethod == WaveFormRenderMethod.Vector)
                        {
                            //if (useVectorResize() && imageAndDraw.m_drawingImage != null)
                            //{
                            //    imageAndDraw.m_image.Source = imageAndDraw.m_drawingImage;
                            //    imageAndDraw.m_imageSourceLoaded = true;
                            //}
                            //else
                            //{
                            //    var drawImg = new DrawingImage(drawGrpResize);
                            //    drawImg.Freeze();
                            //    imageAndDraw.m_image.Source = drawImg;
                            //    imageAndDraw.m_imageSourceLoaded = true;
                            //}

                            var drawImg = new DrawingImage(drawGrp);
                            drawImg.Freeze();
                            imageAndDraw.m_image.Source = drawImg;
                            imageAndDraw.m_imageSourceLoaded = true;
                        }
                        else
                        {
                            var drawingVisual = new DrawingVisual();
                            using (DrawingContext drawContext = drawingVisual.RenderOpen())
                            {
                                drawContext.DrawDrawing(drawGrp);

                                // To draw any Visual:
                                //VisualBrush visualBrush = new VisualBrush
                                //{
                                //    Visual =  visual,
                                //    AutoLayoutContent = true
                                //};
                                //Rect bounds = VisualTreeHelper.GetContentBounds(visual);
                                //drawContext.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));


                                //if (false &&
                                //    m_ViewModel.State.Audio.PlayStreamMarkers != null
                                //    && m_ViewModel.State.Audio.PlayStreamMarkers.Count > Settings.Default.AudioWaveForm_TextPreRenderThreshold)
                                //{
                                //    m_WaveFormTimeTicksAdorner.drawChunkInfos(drawContext, null, 0, heightMagnified, widthMagnified, bytesPerPixel_Magnified, zoom);
                                //}
                            }

                            int wInt = (int)widthMagnified;
                            if (wInt > 0)
                            {
                                var renderTargetBitmap = new RenderTargetBitmap(wInt,
                                                                                (int)heightMagnified, 96, 96,
                                                                                PixelFormats.Pbgra32);

                                renderTargetBitmap.Render(drawingVisual);

                                BitmapSourceGC.Manage(renderTargetBitmap);
                                renderTargetBitmap.Freeze();

                                if (renderMethod == WaveFormRenderMethod.WriteableBitmap
                                    || renderMethod == WaveFormRenderMethod.BitmapSource)
                                {
                                    //FormatConvertedBitmap formatConv = new FormatConvertedBitmap();
                                    //formatConv.BeginInit();
                                    //formatConv.Source = myBitmapSource;
                                    //formatConv.DestinationFormat = PixelFormats.Rgb24;
                                    //formatConv.EndInit();  

                                    uint[] arrBits =
                                        new uint[renderTargetBitmap.PixelWidth * renderTargetBitmap.PixelHeight];
                                    // PixelFormats.Pbgra32 => 4 bytes per pixel, so a full line is:
                                    int stride = 4 * renderTargetBitmap.PixelWidth;
                                    renderTargetBitmap.CopyPixels(arrBits, stride, 0);

                                    if (renderMethod == WaveFormRenderMethod.BitmapSource)
                                    {
                                        var bitmapSource = BitmapSource.Create(wInt,
                                                                               (int)heightMagnified, 96, 96,
                                                                               PixelFormats.Pbgra32, null, arrBits,
                                                                               stride);
                                        BitmapSourceGC.Manage(bitmapSource);
                                        bitmapSource.Freeze();

                                        imageAndDraw.m_image.Source = bitmapSource;
                                        imageAndDraw.m_imageSourceLoaded = true;
                                    }
                                    else
                                    {
                                        var writeableBitmap = new WriteableBitmap(renderTargetBitmap.PixelWidth,
                                                                                  renderTargetBitmap.PixelHeight,
                                                                                  96, 96, PixelFormats.Pbgra32,
                                                                                  null);
                                        writeableBitmap.WritePixels(
                                            new Int32Rect(0, 0, renderTargetBitmap.PixelWidth,
                                                          renderTargetBitmap.PixelHeight), arrBits, stride, 0);

                                        BitmapSourceGC.Manage(writeableBitmap);
                                        writeableBitmap.Freeze();

                                        imageAndDraw.m_image.Source = writeableBitmap;
                                        imageAndDraw.m_imageSourceLoaded = true;
                                    }
                                }
                                else
                                {
                                    imageAndDraw.m_image.Source = renderTargetBitmap;
                                    imageAndDraw.m_imageSourceLoaded = true;
                                }
                            }
                            else
                            {
#if DEBUG
                                Debugger.Break();
#endif //DEBUG
                                imageAndDraw.m_image.Source = null;
                                imageAndDraw.m_imageSourceLoaded = false;
                            }
                        }
                    }
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
                };

                if (!Dispatcher.CheckAccess())
                {
                    //Console.WriteLine(@"Drawing waveform...freeze3");

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, del);
                }
                else
                {
                    del.Invoke();
                }
            }
            else
            {
                //Console.WriteLine(@"Drawing waveform...NOT Freeze1");

                var drawImg = new DrawingImage(drawGrpResize);
                drawImg.Freeze();

                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        imageAndDraw.m_image.Source = drawImg;
                        imageAndDraw.m_imageSourceLoaded = true;
                    }));
                }
                else
                {
                    imageAndDraw.m_image.Source = drawImg;
                    imageAndDraw.m_imageSourceLoaded = true;
                }
            }
        }

        private struct DrawingGroupComponents
        {
            public Drawing enveloppeCh1;
            public Drawing verticalStrokesCh1;
            public Drawing enveloppeCh2;
            public Drawing verticalStrokesCh2;
            public Drawing markers;
            public Drawing back;
        }

        private DrawingGroupComponents drawWaveFormUsingCollectedPoints(
            ImageAndDrawing imageAndDraw,
            //Stream audioStream,
            bool freeze,
            //StreamGeometryContext sgcCh1, StreamGeometryContext sgcCh2,
            //StreamGeometry geometryCh1, StreamGeometry geometryCh2,
            List<Point> listBottomPointsCh1, List<Point> listBottomPointsCh2,
            List<Point> listTopPointsCh1, List<Point> listTopPointsCh2,
            double heightMagnified, double widthMagnified,
            double dBMinHardCoded, double dBMinReached, double dBMaxReached, double decibelDrawDelta, int tolerance,
            double bytesPerPixel_Magnified, double zoom,
            Tuple<TreeNode, TreeNode> treeNodeSelection
            )
        {
            Brush brushColorBars = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Bars);
            //brushColorBars.Freeze();

            GeometryDrawing geoDraw1 = null;
            GeometryDrawing geoDraw2 = null;
            if (
                true
                //Settings.Default.AudioWaveForm_DrawStyle==WaveFormDrawStyle.BorderBars
                //&& freeze
            )
            {
                StreamGeometry geometryCh1 = null;
                StreamGeometryContext sgcCh1 = null;

                geometryCh1 = new StreamGeometry();
                sgcCh1 = geometryCh1.Open();

                for (int i = 0; i < listTopPointsCh1.Count; i++)
                {
                    var pCh1Top = listTopPointsCh1[i];
                    var pCh1Bottom = listBottomPointsCh1[i];

                    if (i == 0)
                    {
                        sgcCh1.BeginFigure(pCh1Top, false, false);
                    }
                    else
                    {
                        sgcCh1.LineTo(pCh1Top, false, false);
                    }

                    sgcCh1.LineTo(pCh1Bottom, true, false);
                }

                sgcCh1.Close();

                geometryCh1.Freeze();
                geoDraw1 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh1);
                geoDraw1.Freeze();


                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                {

                    StreamGeometry geometryCh2 = null;
                    StreamGeometryContext sgcCh2 = null;

                    geometryCh2 = new StreamGeometry();
                    sgcCh2 = geometryCh2.Open();

                    for (int i = 0; i < listTopPointsCh2.Count; i++)
                    {
                        var pCh2Top = listTopPointsCh2[i];
                        var pCh2Bottom = listBottomPointsCh2[i];

                        if (i == 0)
                        {
                            sgcCh2.BeginFigure(pCh2Top, false, false);
                        }
                        else
                        {
                            sgcCh2.LineTo(pCh2Top, false, false);
                        }

                        sgcCh2.LineTo(pCh2Bottom, true, false);
                    }

                    sgcCh2.Close();

                    geometryCh2.Freeze();
                    geoDraw2 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh2);
                    geoDraw2.Freeze();
                }
            }


            GeometryDrawing geoDraw1_envelope = null;
            GeometryDrawing geoDraw2_envelope = null;
            if (Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.Border
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderBars
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill
                || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                )
            {
                try
                {
                    createGeometry_envelope(freeze, out geoDraw1_envelope, out geoDraw2_envelope,
                                            ref listTopPointsCh1, ref listTopPointsCh2,
                                            ref listBottomPointsCh1, ref listBottomPointsCh2,
                                            dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance,
                                            heightMagnified, widthMagnified);
                }
                catch (OperationCanceledException ex)
                {
                    //Console.WriteLine("OperationCanceledException");
                }
            }
            //
#if DEBUG && DRAW_SUBSTREAMS
            
            GeometryDrawing geoDrawStreamSubs = null;
            if (freeze)
            {
                geoDrawStreamSubs = createGeometry_StreamSubs(heightMagnified, audioStream, bytesPerPixel_Magnified);
            }
#endif
            //
            GeometryDrawing geoDrawMarkers = null;

            if (m_ViewModel.State.Audio.PlayStreamMarkers != null && treeNodeSelection.Item1 != null)
            {
                geoDrawMarkers = createGeometry_Markers(imageAndDraw, heightMagnified, bytesPerPixel_Magnified, zoom);
            }
            //
            //var drawGrp = new DrawingGroup();
            DrawingGroupComponents drawingGroupComponents = default(DrawingGroupComponents);


            drawingGroupComponents.markers = geoDrawMarkers;
            drawingGroupComponents.enveloppeCh1 = geoDraw1_envelope;
            drawingGroupComponents.verticalStrokesCh1 = geoDraw1;
            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
            {
                drawingGroupComponents.enveloppeCh2 = geoDraw2_envelope;
                drawingGroupComponents.verticalStrokesCh2 = geoDraw2;
            }

            GeometryDrawing geoDrawBack = createGeometry_Back(heightMagnified, widthMagnified);
            //drawGrp.Children.Add(geoDrawBack);
            drawingGroupComponents.back = geoDrawBack;

            //drawGrp.Freeze();
            //return drawGrp;

            return drawingGroupComponents;
        }
#if DEBUG && DRAW_SUBSTREAMS

        private Geometry createGeometry_StreamSubs_SubStream(SubStream subStream, double heightMagnified, double bytesPerPixel_Magnified, StreamGeometryContext sgcMarkers, long bytes)
        {
            double pixels = (bytes + subStream.Length) / bytesPerPixel_Magnified;

            sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
            sgcMarkers.LineTo(new Point(pixels, heightMagnified), true, false);

            if (string.IsNullOrEmpty(subStream.OptionalInfo)) return null;

            CultureInfo m_culture = CultureInfo.GetCultureInfo("en-us");
            var m_typeFace = new Typeface("Helvetica");

            var m_timeTextBrush = ColorBrushCache.Get(m_ViewModel.ColorPlayhead);
            m_timeTextBrush.Freeze();

            var formattedText = new FormattedText(
                Path.GetFileName(subStream.OptionalInfo),
                m_culture,
                FlowDirection.LeftToRight,
                m_typeFace,
                20,
                m_timeTextBrush
                );
            return formattedText.BuildGeometry(new Point(pixels - formattedText.Width, heightMagnified - formattedText.Height));
        }

        private IEnumerable<Geometry> createGeometry_StreamSubs_SequenceStream(SequenceStream seqStream, double heightMagnified, double bytesPerPixel_Magnified, StreamGeometryContext sgcMarkers, long bytes)
        {
            long bytesLeft = bytes;
            foreach (var audioStream in seqStream.ChildStreams)
            {
                if (audioStream is SequenceStream)
                {
                    foreach (var geo in
                        createGeometry_StreamSubs_SequenceStream((SequenceStream)audioStream,
                        heightMagnified, bytesPerPixel_Magnified, sgcMarkers, bytesLeft))
                    {
                        yield return geo;
                    }
                }
                else if (audioStream is SubStream)
                {
                    yield return createGeometry_StreamSubs_SubStream((SubStream)audioStream,
                        heightMagnified, bytesPerPixel_Magnified, sgcMarkers, bytesLeft);
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }

                bytesLeft += audioStream.Length;
            }
        }

        private GeometryDrawing createGeometry_StreamSubs(double heightMagnified, Stream audioStream, double bytesPerPixel_Magnified)
        {
            List<Geometry> listOfTextGeometries = null;

            var geometrySubStreams = new StreamGeometry();
            using (StreamGeometryContext sgcSubStreams = geometrySubStreams.Open())
            {
                if (audioStream is SequenceStream)
                {
                    listOfTextGeometries = new List<Geometry>(createGeometry_StreamSubs_SequenceStream((SequenceStream)audioStream,
                        heightMagnified, bytesPerPixel_Magnified, sgcSubStreams, 0));
                }
                else if (audioStream is SubStream)
                {
                    listOfTextGeometries = new List<Geometry>
                                               {
                                                   createGeometry_StreamSubs_SubStream((SubStream) audioStream,
                                                                                       heightMagnified,
                                                                                       bytesPerPixel_Magnified,
                                                                                       sgcSubStreams, 0)
                                               };
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }

                sgcSubStreams.Close();
            }

            geometrySubStreams.Freeze();

            Brush brushColorSubStreams = ColorBrushCache.Get(m_ViewModel.ColorPlayhead);
            brushColorSubStreams.Freeze();

            GeometryGroup geoGroup = new GeometryGroup();
            geoGroup.Children.Add(geometrySubStreams);

            foreach (var geometry in listOfTextGeometries)
            {
                geoGroup.Children.Add(geometry);
            }

            var geoDrawSubStreams = new GeometryDrawing(brushColorSubStreams,
                                                                 new Pen(brushColorSubStreams, 1.5) { DashStyle = DashStyles.Dot },
                                                                 geoGroup);
            geoDrawSubStreams.Freeze();

            return geoDrawSubStreams;
        }
#endif
        private GeometryDrawing createGeometry_Markers(ImageAndDrawing imageAndDraw, double heightMagnified, double bytesPerPixel_Magnified, double zoom)
        {
            Brush brushColorMarkers = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Phrases); //m_ViewModel.ColorMarkers);
            //brushColorMarkers.Freeze();

            bool atLeastOneDrawn = false;

            var geometryMarkers = new StreamGeometry();
            using (StreamGeometryContext sgcMarkers = geometryMarkers.Open())
            {
                if (imageAndDraw == m_WaveformTileImages.m_First.m_data)
                {
                    atLeastOneDrawn = true;

                    sgcMarkers.BeginFigure(new Point(0.5, 0), false, false);
                    sgcMarkers.LineTo(new Point(0.5, heightMagnified), true, false);
                }

                long bytesLeft = 0;

#if USE_NORMAL_LIST
                foreach (TreeNodeAndStreamDataLength marker in  m_ViewModel.State.Audio.PlayStreamMarkers)
                {
#else
                LightLinkedList<TreeNodeAndStreamDataLength>.Item current = m_ViewModel.State.Audio.PlayStreamMarkers.m_First;
                while (current != null)
                {
                    TreeNodeAndStreamDataLength marker = current.m_data;
#endif //USE_NORMAL_LIST

                    double pixels = (bytesLeft + marker.m_LocalStreamDataLength) / bytesPerPixel_Magnified;
                    double xZoomed = imageAndDraw.m_originalX * zoom;
                    if (pixels > xZoomed
                        && pixels <= xZoomed + imageAndDraw.m_originalW * zoom)
                    {
                        atLeastOneDrawn = true;

                        pixels -= xZoomed;

                        sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
                        sgcMarkers.LineTo(new Point(pixels, heightMagnified), true, false);
                    }

                    bytesLeft += marker.m_LocalStreamDataLength;

#if USE_NORMAL_LIST
                }
#else
                    current = current.m_nextItem;
                }
#endif //USE_NORMAL_LIST

                sgcMarkers.Close();
            }

            if (atLeastOneDrawn)
            {
                geometryMarkers.Freeze();
                var geoDrawMarkers = new GeometryDrawing(brushColorMarkers,
                                                         new Pen(brushColorMarkers, 1.0),
                                                         geometryMarkers);
                geoDrawMarkers.Freeze();

                return geoDrawMarkers;
            }

            return null;
        }

#if DEBUG
        private bool m_backHackToggle = true;
#endif //DEBUG
        private GeometryDrawing createGeometry_Back(double heightMagnified, double widthMagnified)
        {
            var geometryBack = new StreamGeometry();
            using (StreamGeometryContext sgcBack = geometryBack.Open())
            {
                sgcBack.BeginFigure(new Point(0, 0), true, true);
                sgcBack.LineTo(new Point(0, heightMagnified), false, false);
                sgcBack.LineTo(new Point(widthMagnified, heightMagnified), false, false);
                sgcBack.LineTo(new Point(widthMagnified, 0), false, false);
                sgcBack.Close();
            }
            geometryBack.Freeze();


#if DEBUG
            m_backHackToggle = !m_backHackToggle;
#endif //DEBUG


            Brush brushColorBack = ColorBrushCache.Get(

#if false && DEBUG
m_backHackToggle ?
                Settings.Default.AudioWaveForm_Color_CursorFill
                : Settings.Default.AudioWaveForm_Color_CursorBorder
#else
                //Settings.Default.AudioWaveForm_Color_Back
Colors.Transparent
#endif //DEBUG

); //m_ViewModel.ColorWaveBackground);
            //brushColorBack.Freeze();

            var geoDrawBack = new GeometryDrawing(brushColorBack, null, geometryBack); //new Pen(brushColorBack, 1.0)
            geoDrawBack.Freeze();

            return geoDrawBack;
        }

        private void createGeometry_envelope(
            bool freeze,
            out GeometryDrawing geoDraw1_envelope, out GeometryDrawing geoDraw2_envelope,
            ref List<Point> listTopPointsCh1, ref List<Point> listTopPointsCh2,
            ref List<Point> listBottomPointsCh1, ref List<Point> listBottomPointsCh2,
            double dBMinHardCoded, double dBMinReached,
            double dBMaxReached,
            double decibelDrawDelta, double tolerance,
            double heightMagnified, double widthMagnified)
        {
            var geometryCh1_envelope = new StreamGeometry();
            StreamGeometryContext sgcCh1_envelope = geometryCh1_envelope.Open();

            StreamGeometry geometryCh2_envelope = null;
            StreamGeometryContext sgcCh2_envelope = null;

            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
            {
                geometryCh2_envelope = new StreamGeometry();
                sgcCh2_envelope = geometryCh2_envelope.Open();
            }

            int bottomIndexStartCh1 = listTopPointsCh1.Count;
            int bottomIndexStartCh2 = listTopPointsCh2 != null ? listTopPointsCh2.Count : -1;

            //if (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage)
            //{
            //    listBottomPointsCh1.Reverse();
            //    listTopPointsCh1.AddRange(listBottomPointsCh1);
            //    listBottomPointsCh1.Clear();

            //    if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
            //    {
            //        listBottomPointsCh2.Reverse();
            //        listTopPointsCh2.AddRange(listBottomPointsCh2);
            //        listBottomPointsCh2.Clear();
            //    }
            //}

            if (Settings.Default.AudioWaveForm_UseDecibels && m_ViewModel.IsUseDecibelsAdjust &&
                (dBMinHardCoded != dBMinReached ||
                (m_ViewModel.IsUseDecibelsNoAverage && (-dBMinHardCoded) != dBMaxReached)))
            {
                var listNewCh1 = new List<Point>(listTopPointsCh1.Count);
                var listNewCh2 = listTopPointsCh2 != null ? new List<Point>(listTopPointsCh2.Count) : null;

                double hh = heightMagnified;
                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                {
                    hh /= 2;
                }

                double range = ((m_ViewModel.IsUseDecibelsNoAverage ? -dBMinHardCoded : 0) - dBMinHardCoded);
                double pixPerDbUnit = hh / range;

                int index = -1;

                var p2 = new Point();
                foreach (Point p in listTopPointsCh1)
                {
                    if (m_CancelRequested && m_CancelInterruptDrawingToo) throw new OperationCanceledException();

                    index++;

                    p2.X = p.X;
                    p2.Y = p.Y;

                    /*
                     if (ViewModel.IsUseDecibelsNoAverage)
                     * 
                        YY = pixPerDbUnit * (MaxValue - DB - MinValue) - decibelDrawDelta [+HH]
                     * 
                     * 
                       DB = (-YY - decibelDrawDelta)/pixPerDbUnit + MaxValue - MinValue
                           
                     */


                    /*if (!ViewModel.IsUseDecibelsNoAverage)
                     * 
                        YY = hh - (pixPerDbUnit * (DB - MinValue) - decibelDrawDelta) [+HH]
                     * 
                     * 
                        DB = ( hh + decibelDrawDelta- YY)/pixPerDbUnit + MinValue
                            
                     */


                    double newRange = ((m_ViewModel.IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                    double pixPerDbUnit_new = hh / newRange;

                    double dB;
                    if (m_ViewModel.IsUseDecibelsNoAverage)
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

                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                {
                    index = -1;

                    foreach (Point p in listTopPointsCh2)
                    {
                        if (m_CancelRequested && m_CancelInterruptDrawingToo) throw new OperationCanceledException();
                        index++;

                        p2.X = p.X;
                        p2.Y = p.Y;

                        double newRange = ((m_ViewModel.IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                        double pixPerDbUnit_new = hh / newRange;

                        double dB;
                        if (m_ViewModel.IsUseDecibelsNoAverage)
                        {
                            if (index >= bottomIndexStartCh2)
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
            var pp = new Point();


            if (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage)
            {
                foreach (Point p in listTopPointsCh1)
                {
                    if (m_CancelRequested && m_CancelInterruptDrawingToo) throw new OperationCanceledException();
                    pp.X = p.X;
                    pp.Y = p.Y;

                    if (pp.X > widthMagnified)
                    {
                        pp.X = widthMagnified;
                    }
                    if (pp.X < 0)
                    {
                        pp.X = 0;
                    }
                    if (pp.Y > heightMagnified - tolerance)
                    {
                        pp.Y = heightMagnified - tolerance;
                    }
                    if (pp.Y < 0 + tolerance)
                    {
                        pp.Y = 0 + tolerance;
                    }
                    if (count == 0)
                    {
                        sgcCh1_envelope.BeginFigure(pp,
                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                            && (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
                    }
                    else
                    {
                        sgcCh1_envelope.LineTo(pp, true, false);
                    }
                    count++;
                }
                for (int i = listBottomPointsCh1.Count - 1; i >= 0; i--)
                {
                    Point p = listBottomPointsCh1[i];

                    pp.X = p.X;
                    pp.Y = p.Y;

                    if (pp.X > widthMagnified)
                    {
                        pp.X = widthMagnified;
                    }
                    if (pp.X < 0)
                    {
                        pp.X = 0;
                    }
                    if (pp.Y > heightMagnified - tolerance)
                    {
                        pp.Y = heightMagnified - tolerance;
                    }
                    if (pp.Y < 0 + tolerance)
                    {
                        pp.Y = 0 + tolerance;
                    }
                    if (count == 0)
                    {
                        sgcCh1_envelope.BeginFigure(pp,
                            Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                            && (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
                    }
                    else
                    {
                        sgcCh1_envelope.LineTo(pp, true, false);
                    }
                    count++;
                }
                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1 && sgcCh2_envelope != null)
                {
                    count = 0;

                    foreach (Point p in listTopPointsCh2)
                    {
                        if (m_CancelRequested && m_CancelInterruptDrawingToo) throw new OperationCanceledException();
                        pp.X = p.X;
                        pp.Y = p.Y;

                        if (pp.X > widthMagnified)
                        {
                            pp.X = widthMagnified;
                        }
                        if (pp.X < 0)
                        {
                            pp.X = 0;
                        }
                        if (pp.Y > heightMagnified - tolerance)
                        {
                            pp.Y = heightMagnified - tolerance;
                        }
                        if (pp.Y < 0 + tolerance)
                        {
                            pp.Y = 0 + tolerance;
                        }
                        if (count == 0)
                        {
                            sgcCh2_envelope.BeginFigure(pp,
                                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                                && (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
                        }
                        else
                        {
                            sgcCh2_envelope.LineTo(pp, true, false);
                        }
                        count++;
                    }
                    for (int i = listBottomPointsCh2.Count - 1; i >= 0; i--)
                    {
                        Point p = listBottomPointsCh2[i];

                        pp.X = p.X;
                        pp.Y = p.Y;

                        if (pp.X > widthMagnified)
                        {
                            pp.X = widthMagnified;
                        }
                        if (pp.X < 0)
                        {
                            pp.X = 0;
                        }
                        if (pp.Y > heightMagnified - tolerance)
                        {
                            pp.Y = heightMagnified - tolerance;
                        }
                        if (pp.Y < 0 + tolerance)
                        {
                            pp.Y = 0 + tolerance;
                        }
                        if (count == 0)
                        {
                            sgcCh2_envelope.BeginFigure(pp,
                                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                                && (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
                        }
                        else
                        {
                            sgcCh2_envelope.LineTo(pp, true, false);
                        }
                        count++;
                    }
                }
            }
            else
            {
                foreach (Point p in listTopPointsCh1)
                {
                    if (m_CancelRequested && m_CancelInterruptDrawingToo) throw new OperationCanceledException();
                    pp.X = p.X;
                    pp.Y = p.Y;

                    if (pp.X > widthMagnified)
                    {
                        pp.X = widthMagnified;
                    }
                    if (pp.X < 0)
                    {
                        pp.X = 0;
                    }
                    if (pp.Y > heightMagnified - tolerance)
                    {
                        pp.Y = heightMagnified - tolerance;
                    }
                    if (pp.Y < 0 + tolerance)
                    {
                        pp.Y = 0 + tolerance;
                    }
                    if (count == 0)
                    {
                        sgcCh1_envelope.BeginFigure(pp,
                            Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                            && (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
                    }
                    else
                    {
                        sgcCh1_envelope.LineTo(pp, true, false);
                    }
                    count++;
                }
                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1 && sgcCh2_envelope != null)
                {
                    count = 0;

                    foreach (Point p in listTopPointsCh2)
                    {
                        if (m_CancelRequested && m_CancelInterruptDrawingToo) throw new OperationCanceledException();
                        pp.X = p.X;
                        pp.Y = p.Y;

                        if (pp.X > widthMagnified)
                        {
                            pp.X = widthMagnified;
                        }
                        if (pp.X < 0)
                        {
                            pp.X = 0;
                        }
                        if (pp.Y > heightMagnified - tolerance)
                        {
                            pp.Y = heightMagnified - tolerance;
                        }
                        if (pp.Y < 0 + tolerance)
                        {
                            pp.Y = 0 + tolerance;
                        }
                        if (count == 0)
                        {
                            sgcCh2_envelope.BeginFigure(pp,
                                Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFill || Settings.Default.AudioWaveForm_DrawStyle == WaveFormDrawStyle.BorderFillBars
                                && (!Settings.Default.AudioWaveForm_UseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
                        }
                        else
                        {
                            sgcCh2_envelope.LineTo(pp, true, false);
                        }
                        count++;
                    }
                }
            }

            sgcCh1_envelope.Close();
            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1 && sgcCh2_envelope != null)
            {
                sgcCh2_envelope.Close();
            }

            Brush brushColorEnvelopeOutline = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Border); //m_ViewModel.ColorEnvelopeOutline);
            //brushColorEnvelopeOutline.Freeze();

            Brush brushColorEnvelopeFill = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Fill); //m_ViewModel.ColorEnvelopeFill);
            //brushColorEnvelopeFill.Freeze();

            geometryCh1_envelope.Freeze();
            geoDraw1_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh1_envelope);
            geoDraw1_envelope.Freeze();

            geoDraw2_envelope = null;
            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1 && geometryCh2_envelope != null)
            {
                geometryCh2_envelope.Freeze();
                geoDraw2_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh2_envelope);
                geoDraw2_envelope.Freeze();
            }
        }
    }

    //static class BytesToShort
    //{
    //    private static bool IsLittleEndian = true;

    //    [SecuritySafeCritical]
    //    public static unsafe short Convert(byte[] value, int startIndex)
    //    {
    //        fixed (byte* numRef = &(value[startIndex]))
    //        {
    //            if ((startIndex % 2) == 0)
    //            {
    //                return *(((short*)numRef));
    //            }
    //            if (IsLittleEndian)
    //            {
    //                return (short)(numRef[0] | (numRef[1] << 8));
    //            }
    //            return (short)((numRef[0] << 8) | numRef[1]);
    //        }
    //    }
    //}

}
