using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using urakawa.core;
using urakawa.data;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneView
    {
        private BackgroundWorker m_BackgroundLoader;

        public void CancelWaveFormLoad()
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action)CancelWaveFormLoad);
                return;
            }

            if (!m_ViewModel.IsWaveFormLoading) return;
            if (m_BackgroundLoader == null) return;
            if (!m_BackgroundLoader.IsBusy) return;

            m_BackgroundLoader.CancelAsync();

            //while (m_BackgroundLoader != null || m_ViewModel.IsWaveFormLoading)
            //{
            //    Thread.Sleep(100);
            //    m_ShellView.PumpDispatcherFrames(); // make sure we don't block the UI thread
            //}
        }

        private void OnWaveFormCancelButtonClick(object sender, RoutedEventArgs e)
        {
            CancelWaveFormLoad();
        }


        private double m_ProgressVisibleOffset = 0;

        private DrawingImage m_WaveFormImageSourceDrawingImage;


        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_LoadWaveForm(bool wasPlaying, bool play)
        {
            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels == 1)
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Collapsed;
                PeakOverloadLabelCh1.SetValue(Grid.ColumnSpanProperty, 2);
            }
            else
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Visible;
                PeakOverloadLabelCh1.SetValue(Grid.ColumnSpanProperty, 1);
            }

            double widthReal = WaveFormCanvas.ActualWidth;
            if (double.IsNaN(widthReal) || widthReal == 0)
            {
                widthReal = WaveFormCanvas.Width;
            }
            double heightReal = WaveFormCanvas.ActualHeight;
            if (double.IsNaN(heightReal) || heightReal == 0)
            {
                heightReal = WaveFormCanvas.Height;
            }

            BytesPerPixel = m_ViewModel.State.Audio.DataLength / widthReal;


            var zoom = (m_ShellView != null ? m_ShellView.MagnificationLevel : (Double)FindResource("MagnificationLevel"));

            double widthMagnified = widthReal * zoom;
            double heightMagnified = heightReal * zoom;

            double bytesPerPixel_Magnified = m_ViewModel.State.Audio.DataLength / widthMagnified;

            int byteDepth = m_ViewModel.State.Audio.PcmFormat.Data.BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((bytesPerPixel_Magnified * m_ViewModel.WaveStepX) / byteDepth);
            samplesPerStep += (samplesPerStep % m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;

            var estimatedCapacity = (int)(widthMagnified / (bytesPerStep / bytesPerPixel_Magnified)) + 1;

            if (true) //estimatedCapacity * m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 101) //501
            {
                bool vertical = WaveFormProgress.Orientation == Orientation.Vertical;
                double sizeProgress = (vertical ? WaveFormScroll.Height : WaveFormScroll.Width);
                if (double.IsNaN(sizeProgress) || sizeProgress == 0)
                {
                    sizeProgress = (vertical ? WaveFormScroll.ActualHeight : WaveFormScroll.ActualWidth);
                }

                //WaveFormProgress.SmallChange = 100;

                double numberOfVisibleXIncrements = sizeProgress / 20; // progressbar update will be triggered every 35 pixels, which will minimize the Dispatcher access while reading the audio bytes and therefore increase performance.
                double progressStep = estimatedCapacity / numberOfVisibleXIncrements;

                //WaveFormProgress.LargeChange = progressStep;
                m_ProgressVisibleOffset = Math.Floor(progressStep);

                WaveFormProgress.IsIndeterminate = false;
                WaveFormProgress.Value = 0;
                WaveFormProgress.Minimum = 0;
                WaveFormProgress.Maximum = estimatedCapacity;

                m_ViewModel.IsWaveFormLoading = true;

                m_BackgroundLoader = new BackgroundWorker() { WorkerSupportsCancellation = true };
                Exception workException = null;

                // Executes on a separate thread
                m_BackgroundLoader.DoWork += (sender, e) => loadWaveForm(true, widthMagnified, heightMagnified, wasPlaying, play, bytesPerPixel_Magnified);

                // Executes on the thread that created the background worker (synchronization context)
                // which is the WPF dispatcher and which has a message pump on its own
                m_BackgroundLoader.RunWorkerCompleted += (sender, e) =>
                {
                    workException = e.Error;

                    WaveFormProgress.IsIndeterminate = true;

                    if (workException != null)
                    {
                        throw workException;
                    }

                    m_ViewModel.IsWaveFormLoading = false;

                    m_BackgroundLoader = null;
                };
                m_BackgroundLoader.RunWorkerAsync();
            }
            else
            {
                m_ViewModel.IsWaveFormLoading = true;

                loadWaveForm(false, widthMagnified, heightMagnified, wasPlaying, play, bytesPerPixel_Magnified);

                m_ViewModel.IsWaveFormLoading = false;
            }
        }


        private void loadWaveForm(bool inBackgroundThread, double widthMagnified, double heightMagnified, bool wasPlaying, bool play, double bytesPerPixel_Magnified)
        {
            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            int byteDepth = m_ViewModel.State.Audio.PcmFormat.Data.BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((bytesPerPixel_Magnified * m_ViewModel.WaveStepX) / byteDepth);
            samplesPerStep += (samplesPerStep % m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);

            if (samplesPerStep <= 0) return;

            int bytesPerStep = samplesPerStep * byteDepth;

            var estimatedCapacity = (int)(widthMagnified / (bytesPerStep / bytesPerPixel_Magnified)) + 1;

            var bytes = new byte[bytesPerStep]; // Int 8 unsigned
#if USE_BLOCK_COPY
            var samples = new short[samplesPerStep]; // Int 16 signed
#endif //USE_BLOCK_COPY
            List<Point> listTopPointsCh1 = null;
            List<Point> listTopPointsCh2 = null;
            List<Point> listBottomPointsCh1 = null;
            List<Point> listBottomPointsCh2 = null;
            if (m_ViewModel.IsEnvelopeVisible)
            {
                listTopPointsCh1 = new List<Point>(estimatedCapacity);
                listBottomPointsCh1 = new List<Point>(estimatedCapacity);
                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                {
                    listTopPointsCh2 = new List<Point>(estimatedCapacity);
                    listBottomPointsCh2 = new List<Point>(estimatedCapacity);
                }
                else
                {
                    listTopPointsCh2 = new List<Point>(1);
                    listBottomPointsCh2 = new List<Point>(1);
                }
            }

            double x = 0.5;
            const bool bJoinInterSamples = false;

            if (m_BackgroundLoader.CancellationPending) return;

            const int tolerance = 5;
            try
            {
                Stream audioStream = m_ViewModel.AudioPlayer_GetPlayStream();

                if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement

                audioStream.Position = 0;
                audioStream.Seek(0, SeekOrigin.Begin);

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

                System.Diagnostics.Debug.Print(dbMinValue + "");

                double dBMinHardCoded = dbMinValue;

                double dbMaxValue = (m_ViewModel.IsUseDecibelsNoAverage ? -dbMinValue : 0);

                bool firstY1 = true;
                bool firstY1_ = true;

                var geometryCh1 = new StreamGeometry();
                StreamGeometryContext sgcCh1 = geometryCh1.Open();

                StreamGeometry geometryCh2 = null;
                StreamGeometryContext sgcCh2 = null;

                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
                {
                    geometryCh2 = new StreamGeometry();
                    sgcCh2 = geometryCh2.Open();
                }

                //bool isLittleEndian = BitConverter.IsLittleEndian;

                double sumProgress = 0;

                if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement

                Stopwatch stopWatch = Stopwatch.StartNew();

                int read;

                #region LOOP

                while ((read = audioStream.Read(bytes, 0, bytesPerStep)) > 0)
                {
#if USE_BLOCK_COPY
                    // converts Int 8 unsigned to Int 16 signed
                    Buffer.BlockCopy(bytes, 0, samples, 0, read);
#endif //USE_BLOCK_COPY

                    if (m_BackgroundLoader.CancellationPending) break;

                    for (int channel = 0; channel < m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels; channel++)
                    {
                        int limit = samplesPerStep;

                        if (read < bytesPerStep)
                        {
                            var nSamples = (int)Math.Floor((double)read / byteDepth);

                            nSamples = m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels *
                                       (int)Math.Floor((double)nSamples / m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);
                            limit = nSamples;
                            limit = Math.Min(limit, samplesPerStep);
                        }

                        double total = 0;
                        int nSamplesRead = 0;

                        double min = short.MaxValue; // Int 16 signed 32767
                        double max = short.MinValue; // Int 16 signed -32768

                        for (int i = channel; i < limit; i += m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels)
                        {
                            nSamplesRead++;
#if USE_BLOCK_COPY
                            short sample = samples[i];

                            int newI = i << 1;
                            byte byte1 = bytes[newI];
                            byte byte2 = bytes[newI + 1];

                            short sampleDirectFromByteArray = BitConverter.ToInt16(bytes, newI);
                            if (sampleDirectFromByteArray == -1)
                                Debug.Assert(sample == 0 || sample == -1);
                            else
                                Debug.Assert(sample == sampleDirectFromByteArray);

                            // Little Indian
                            short sampleDirectFromByteArray1 = (short)(byte1 | (byte2 << 8));
                            short sampleDirectFromByteArray2 = (short)(byte1 + byte2 * 256);
                            Debug.Assert(sampleDirectFromByteArray1 == sampleDirectFromByteArray2);
                            if (sampleDirectFromByteArray2 == -1)
                                Debug.Assert(sample == 0 || sample == -1);
                            else
                            {
                                if (sample != sampleDirectFromByteArray2)
                                {
                                    // Big Indian
                                    short sampleDirectFromByteArray3 = (short) ((byte1 << 8) | byte2);
                                    short sampleDirectFromByteArray4 = (short) (byte1*256 + byte2);
                                    Debug.Assert(sampleDirectFromByteArray3 == sampleDirectFromByteArray4);
                                    Debug.Assert(sample == sampleDirectFromByteArray4);

                                    if (sample == sampleDirectFromByteArray4)
                                        Debug.Assert(sample == sampleDirectFromByteArray2);
                                }
                            }
#else //USE_BLOCK_COPY
                            // LITTLE INDIAN !
                            var sample = (short)(bytes[i << 1] | (bytes[(i << 1) + 1] << 8));
#endif //USE_BLOCK_COPY

                            if (sample == short.MinValue)
                            {
                                total += short.MaxValue + 1;
                            }
                            else
                            {
                                total += Math.Abs(sample);
                            }

                            if (sample < min)
                            {
                                min = sample;
                            }
                            if (sample > max)
                            {
                                max = sample;
                            }
                        }

                        double avg = total / nSamplesRead;

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

                        if (m_ViewModel.IsUseDecibels)
                        {

                            if (!m_ViewModel.IsUseDecibelsNoAverage)
                            {
                                min = avg;
                                max = avg;
                            }

                            bool minIsNegative = min < 0;
                            double minAbs = Math.Abs(min);
                            if (minAbs == 0)
                            {
                                min = (m_ViewModel.IsUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                            }
                            else
                            {
                                min = logFactor * Math.Log10(minAbs / reference);
                                dBMinReached = Math.Min(dBMinReached, min);
                                if (m_ViewModel.IsUseDecibelsNoAverage && !minIsNegative)
                                {
                                    min = -min;
                                }
                            }

                            bool maxIsNegative = max < 0;
                            double maxAbs = Math.Abs(max);
                            if (maxAbs == 0)
                            {
                                max = (m_ViewModel.IsUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                            }
                            else
                            {
                                max = logFactor * Math.Log10(maxAbs / reference);
                                dBMaxReached = Math.Max(dBMaxReached, max);
                                if (m_ViewModel.IsUseDecibelsNoAverage && !maxIsNegative)
                                {
                                    max = -max;
                                }
                            }

                            double totalDbRange = dbMaxValue - dbMinValue;
                            double pixPerDbUnit = hh / totalDbRange;

                            if (m_ViewModel.IsUseDecibelsNoAverage)
                            {
                                min = dbMinValue - min;
                            }
                            y1 = pixPerDbUnit * (min - dbMinValue) + decibelDrawDelta;
                            if (!m_ViewModel.IsUseDecibelsNoAverage)
                            {
                                y1 = hh - y1;
                            }
                            if (m_ViewModel.IsUseDecibelsNoAverage)
                            {
                                max = dbMaxValue - max;
                            }
                            y2 = pixPerDbUnit * (max - dbMinValue) - decibelDrawDelta;
                            if (!m_ViewModel.IsUseDecibelsNoAverage)
                            {
                                y2 = hh - y2;
                            }
                        }
                        #endregion DECIBELS
                        else
                        {
                            const double MaxValue = short.MaxValue; // Int 16 signed 32767
                            const double MinValue = short.MinValue; // Int 16 signed -32768

                            double pixPerUnit = hh /
                                                (MaxValue - MinValue); // == ushort.MaxValue => Int 16 unsigned 65535

                            y1 = pixPerUnit * (min - MinValue);
                            y1 = hh - y1;
                            y2 = pixPerUnit * (max - MinValue);
                            y2 = hh - y2;
                        }

                        if (!(m_ViewModel.IsUseDecibels && m_ViewModel.IsUseDecibelsAdjust))
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
                            if (m_ViewModel.IsEnvelopeVisible && listTopPointsCh1 != null)
                            {
                                listTopPointsCh1.Add(new Point(x, y1));
                            }
                            if (m_ViewModel.IsWaveFillVisible)
                            {
                                if (firstY1)
                                {
                                    sgcCh1.BeginFigure(new Point(x, y1), false, false);
                                    firstY1 = false;
                                }
                                else
                                {
                                    sgcCh1.LineTo(new Point(x, y1), bJoinInterSamples, false);
                                }
                            }
                        }
                        else if (sgcCh2 != null)
                        {
                            y1 += hh;
                            if (m_ViewModel.IsEnvelopeVisible && listTopPointsCh2 != null)
                            {
                                listTopPointsCh2.Add(new Point(x, y1));
                            }
                            if (m_ViewModel.IsWaveFillVisible)
                            {
                                if (firstY1_)
                                {
                                    sgcCh2.BeginFigure(new Point(x, y1), false, false);
                                    firstY1_ = false;
                                }
                                else
                                {
                                    sgcCh2.LineTo(new Point(x, y1), bJoinInterSamples, false);
                                }
                            }
                        }

                        if (channel == 0)
                        {
                            if (m_ViewModel.IsWaveFillVisible)
                            {
                                sgcCh1.LineTo(new Point(x, y2), true, false);
                            }
                            if (m_ViewModel.IsEnvelopeVisible && listBottomPointsCh1 != null)
                            {
                                listBottomPointsCh1.Add(new Point(x, y2));
                            }
                        }
                        else if (sgcCh2 != null)
                        {
                            y2 += hh;

                            if (m_ViewModel.IsWaveFillVisible)
                            {
                                sgcCh2.LineTo(new Point(x, y2), true, false);
                            }
                            if (m_ViewModel.IsEnvelopeVisible && listBottomPointsCh2 != null)
                            {
                                listBottomPointsCh2.Add(new Point(x, y2));
                            }
                        }
                    }


                    stopWatch.Stop();
                    if (stopWatch.ElapsedMilliseconds >= 1000)
                    {
                        drawWaveForm(audioStream, inBackgroundThread, false,
                            sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                            listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                            heightMagnified, widthMagnified,
                            dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified);

                        stopWatch.Reset();
                    }
                    stopWatch.Start();

                    if (inBackgroundThread)
                    {
                        sumProgress++;
                        if (sumProgress >= m_ProgressVisibleOffset)
                        {
                            sumProgress = 0;

                            //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                WaveFormProgress.Value += m_ProgressVisibleOffset;
                            }));
                        }
                    }

                    x += (read / bytesPerPixel_Magnified); //ViewModel.WaveStepX;
                    if (x > widthMagnified)
                    {
                        break;
                    }

                    if (m_BackgroundLoader.CancellationPending) break;
                }

                #endregion LOOP

                if (inBackgroundThread)
                {
                    //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        WaveFormProgress.IsIndeterminate = true;
                    }));
                }

                drawWaveForm(audioStream, inBackgroundThread, true,
                    sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                    listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                    heightMagnified, widthMagnified,
                    dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified);

            }
            finally
            {
                ShowHideWaveFormLoadingMessage(false);

                if (inBackgroundThread)
                {
                    //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() => m_ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying, play)));
                }
                else
                {
                    m_ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying, play);
                }
            }
        }

        private void drawWaveForm(Stream audioStream, bool inBackgroundThread, bool freeze, StreamGeometryContext sgcCh1, StreamGeometryContext sgcCh2, StreamGeometry geometryCh1, StreamGeometry geometryCh2, List<Point> listBottomPointsCh1, List<Point> listBottomPointsCh2, List<Point> listTopPointsCh1, List<Point> listTopPointsCh2, double heightMagnified, double widthMagnified, double dBMinHardCoded, double dBMinReached, double dBMaxReached, double decibelDrawDelta, int tolerance, double bytesPerPixel_Magnified)
        {
            Console.WriteLine(@"Drawing waveform...");

            //if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement

            DrawingGroup drawGrp = drawWaveFormUsingCollectedPoints(audioStream, freeze,
                sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                heightMagnified, widthMagnified,
                dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified);

            //if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement


            //if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement

            //if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement

            var drawImg = new DrawingImage(drawGrp);
            drawImg.Freeze();
            m_WaveFormImageSourceDrawingImage = drawImg;

            if (freeze)
            {
                var drawingVisual = new DrawingVisual();
                using (DrawingContext context = drawingVisual.RenderOpen())
                {
                    context.DrawDrawing(drawGrp);
                }

                var renderBitmap = new RenderTargetBitmap((int)widthMagnified, (int)heightMagnified, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(drawingVisual);
                renderBitmap.Freeze();

                GC.Collect();

                if (inBackgroundThread)
                {
                    //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        WaveFormImage.Source = renderBitmap;

                        m_WaveFormTimeTicksAdorner.InvalidateVisual();
                        m_WaveFormTimeTicksAdorner.ResetBrushes();
                        m_WaveFormLoadingAdorner.ResetBrushes();

                    }));
                }
                else
                {
                    WaveFormImage.Source = renderBitmap;

                    m_WaveFormTimeTicksAdorner.InvalidateVisual();
                    m_WaveFormTimeTicksAdorner.ResetBrushes();
                    m_WaveFormLoadingAdorner.ResetBrushes();
                }
            }
            else
            {
                if (inBackgroundThread)
                {
                    //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        WaveFormImage.Source = drawImg;
                    }));
                }
                else
                {
                    WaveFormImage.Source = drawImg;
                }
            }

            //if (m_BackgroundLoader.CancellationPending) return; // will run the code in the "finally" statement
        }


        private DrawingGroup drawWaveFormUsingCollectedPoints(Stream audioStream, bool freeze,
            StreamGeometryContext sgcCh1, StreamGeometryContext sgcCh2,
            StreamGeometry geometryCh1, StreamGeometry geometryCh2,
            List<Point> listBottomPointsCh1, List<Point> listBottomPointsCh2, List<Point> listTopPointsCh1, List<Point> listTopPointsCh2,
            double heightMagnified, double widthMagnified,
            double dBMinHardCoded, double dBMinReached, double dBMaxReached, double decibelDrawDelta, int tolerance,
            double bytesPerPixel_Magnified
            )
        {
            Brush brushColorBars = new SolidColorBrush(m_ViewModel.ColorWaveBars);
            brushColorBars.Freeze();

            GeometryDrawing geoDraw1 = null;
            GeometryDrawing geoDraw2 = null;
            if (m_ViewModel.IsWaveFillVisible && freeze)
            {
                sgcCh1.Close();
                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1 && sgcCh2 != null)
                {
                    sgcCh2.Close();
                }

                geometryCh1.Freeze();
                geoDraw1 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh1);
                geoDraw1.Freeze();

                if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1 && geometryCh2 != null)
                {
                    geometryCh2.Freeze();
                    geoDraw2 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh2);
                    geoDraw2.Freeze();
                }
            }
            //
            GeometryDrawing geoDraw1_envelope = null;
            GeometryDrawing geoDraw2_envelope = null;
            if (m_ViewModel.IsEnvelopeVisible)
            {
                createGeometry_envelope(freeze, out geoDraw1_envelope, out geoDraw2_envelope,
                                        ref listTopPointsCh1, ref listTopPointsCh2,
                                        ref listBottomPointsCh1, ref listBottomPointsCh2,
                                        dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance,
                                        heightMagnified, widthMagnified);
            }
            //
            GeometryDrawing geoDrawStreamSubs = null;
#if DEBUG
            if (freeze)
            {
                geoDrawStreamSubs = createGeometry_StreamSubs(heightMagnified, audioStream, bytesPerPixel_Magnified);
            }
#endif
            //
            GeometryDrawing geoDrawMarkers = null;

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_ViewModel.m_UrakawaSession.GetTreeNodeSelection();
            if (treeNodeSelection.Item1 != null)
            {
                geoDrawMarkers = createGeometry_Markers(heightMagnified, bytesPerPixel_Magnified);
            }
            //
            GeometryDrawing geoDrawBack = createGeometry_Back(heightMagnified, widthMagnified);
            //
            //
            var drawGrp = new DrawingGroup();

            if (m_ViewModel.IsBackgroundVisible)
            {
                drawGrp.Children.Add(geoDrawBack);
            }
            if (m_ViewModel.IsEnvelopeVisible)
            {
                if (m_ViewModel.IsEnvelopeFilled)
                {
                    if (geoDraw1_envelope != null)
                    {
                        drawGrp.Children.Add(geoDraw1_envelope);
                    }
                    if (m_ViewModel.IsWaveFillVisible && freeze)
                    {
                        drawGrp.Children.Add(geoDraw1);
                    }
                }
                else
                {
                    if (m_ViewModel.IsWaveFillVisible && freeze)
                    {
                        drawGrp.Children.Add(geoDraw1);
                    }
                    if (geoDraw1_envelope != null)
                    {
                        drawGrp.Children.Add(geoDraw1_envelope);
                    }
                }
            }
            else if (m_ViewModel.IsWaveFillVisible && freeze)
            {
                drawGrp.Children.Add(geoDraw1);
            }
            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
            {
                if (m_ViewModel.IsEnvelopeVisible)
                {
                    if (m_ViewModel.IsEnvelopeFilled)
                    {
                        if (geoDraw2_envelope != null)
                        {
                            drawGrp.Children.Add(geoDraw2_envelope);
                        }
                        if (m_ViewModel.IsWaveFillVisible && geoDraw2 != null && freeze)
                        {
                            drawGrp.Children.Add(geoDraw2);
                        }
                    }
                    else
                    {
                        if (m_ViewModel.IsWaveFillVisible && geoDraw2 != null && freeze)
                        {
                            drawGrp.Children.Add(geoDraw2);
                        }
                        if (geoDraw2_envelope != null)
                        {
                            drawGrp.Children.Add(geoDraw2_envelope);
                        }
                    }
                }
                else if (m_ViewModel.IsWaveFillVisible && geoDraw2 != null && freeze)
                {
                    drawGrp.Children.Add(geoDraw2);
                }
            }
            if (treeNodeSelection.Item1 != null && geoDrawMarkers != null)
            {
                drawGrp.Children.Add(geoDrawMarkers);
            }

            if (geoDrawStreamSubs != null)
            {
                drawGrp.Children.Add(geoDrawStreamSubs);
            }

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

            drawGrp.Freeze();

            return drawGrp;
        }

        private Geometry createGeometry_StreamSubs_SubStream(SubStream subStream, double heightMagnified, double bytesPerPixel_Magnified, StreamGeometryContext sgcMarkers, long bytes)
        {
            double pixels = (bytes + subStream.Length) / bytesPerPixel_Magnified;

            sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
            sgcMarkers.LineTo(new Point(pixels, heightMagnified), true, false);

            if (string.IsNullOrEmpty(subStream.OptionalInfo)) return null;

            CultureInfo m_culture = CultureInfo.GetCultureInfo("en-us");
            var m_typeFace = new Typeface("Helvetica");

            var m_timeTextBrush = new SolidColorBrush(m_ViewModel.ColorPlayhead);
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
                    Debugger.Break();
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
                    Debugger.Break();
                }

                sgcSubStreams.Close();
            }

            geometrySubStreams.Freeze();

            Brush brushColorSubStreams = new SolidColorBrush(m_ViewModel.ColorPlayhead);
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

        private GeometryDrawing createGeometry_Markers(double heightMagnified, double bytesPerPixel_Magnified)
        {
            Brush brushColorMarkers = new SolidColorBrush(m_ViewModel.ColorMarkers);
            brushColorMarkers.Freeze();

            var geometryMarkers = new StreamGeometry();
            using (StreamGeometryContext sgcMarkers = geometryMarkers.Open())
            {
                sgcMarkers.BeginFigure(new Point(0.5, 0), false, false);
                sgcMarkers.LineTo(new Point(0.5, heightMagnified), true, false);

                long bytesLeft = 0;
                foreach (TreeNodeAndStreamDataLength marker in m_ViewModel.State.Audio.PlayStreamMarkers)
                {
                    double pixels = (bytesLeft + marker.m_LocalStreamDataLength) / bytesPerPixel_Magnified;

                    sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
                    sgcMarkers.LineTo(new Point(pixels, heightMagnified), true, false);

                    bytesLeft += marker.m_LocalStreamDataLength;
                }
                sgcMarkers.Close();
            }

            geometryMarkers.Freeze();
            var geoDrawMarkers = new GeometryDrawing(brushColorMarkers,
                                                                 new Pen(brushColorMarkers, 1.0),
                                                                 geometryMarkers);
            geoDrawMarkers.Freeze();

            return geoDrawMarkers;
        }

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

            Brush brushColorBack = new SolidColorBrush(m_ViewModel.ColorWaveBackground);
            brushColorBack.Freeze();

            var geoDrawBack = new GeometryDrawing(brushColorBack, null, geometryBack); //new Pen(brushColorBack, 1.0)
            geoDrawBack.Freeze();

            return geoDrawBack;
        }

        private void createGeometry_envelope(bool freeze, out GeometryDrawing geoDraw1_envelope, out GeometryDrawing geoDraw2_envelope,
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
            int bottomIndexStartCh2 = listTopPointsCh2.Count;

            //if (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage)
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

            if (m_ViewModel.IsUseDecibels && m_ViewModel.IsUseDecibelsAdjust &&
                (dBMinHardCoded != dBMinReached ||
                (m_ViewModel.IsUseDecibelsNoAverage && (-dBMinHardCoded) != dBMaxReached)))
            {
                var listNewCh1 = new List<Point>(listTopPointsCh1.Count);
                var listNewCh2 = new List<Point>(listTopPointsCh2.Count);

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


            if (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage)
            {
                foreach (Point p in listTopPointsCh1)
                {
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
                        sgcCh1_envelope.BeginFigure(pp, m_ViewModel.IsEnvelopeFilled && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                        sgcCh1_envelope.BeginFigure(pp, m_ViewModel.IsEnvelopeFilled && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                            sgcCh2_envelope.BeginFigure(pp, m_ViewModel.IsEnvelopeFilled && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                            sgcCh2_envelope.BeginFigure(pp, m_ViewModel.IsEnvelopeFilled && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                        sgcCh1_envelope.BeginFigure(pp, m_ViewModel.IsEnvelopeFilled && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                            sgcCh2_envelope.BeginFigure(pp, m_ViewModel.IsEnvelopeFilled && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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

            Brush brushColorEnvelopeOutline = new SolidColorBrush(m_ViewModel.ColorEnvelopeOutline);
            brushColorEnvelopeOutline.Freeze();

            Brush brushColorEnvelopeFill = new SolidColorBrush(m_ViewModel.ColorEnvelopeFill);
            brushColorEnvelopeFill.Freeze();

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
