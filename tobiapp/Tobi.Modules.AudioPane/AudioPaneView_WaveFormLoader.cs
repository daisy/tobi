using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using urakawa.core;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneView
    {
        private double m_ProgressVisibleOffset = 0;

        private DrawingImage m_WaveFormImageSourceDrawingImage;

        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_LoadWaveForm(bool wasPlaying, bool play)
        {
            if (ViewModel.PcmFormat.NumberOfChannels == 1)
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Collapsed;
                PeakOverloadLabelCh1.SetValue(Grid.ColumnSpanProperty, 2);
            }
            else
            {
                PeakOverloadLabelCh2.Visibility = Visibility.Visible;
                PeakOverloadLabelCh1.SetValue(Grid.ColumnSpanProperty, 1);
            }

            double width = WaveFormCanvas.ActualWidth;
            if (double.IsNaN(width) || width == 0)
            {
                width = WaveFormCanvas.Width;
            }
            double height = WaveFormCanvas.ActualHeight;
            if (double.IsNaN(height) || height == 0)
            {
                height = WaveFormCanvas.Height;
            }

            BytesPerPixel = ViewModel.DataLength / width;

            int byteDepth = ViewModel.PcmFormat.BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((BytesPerPixel * ViewModel.WaveStepX) / byteDepth);
            samplesPerStep += (samplesPerStep % ViewModel.PcmFormat.NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;

            var estimatedCapacity = (int)(width / (bytesPerStep / BytesPerPixel)) + 1;

            if (estimatedCapacity * ViewModel.PcmFormat.NumberOfChannels > 501)
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

                ViewModel.IsWaveFormLoading = true;

                var fileWorker = new BackgroundWorker();
                fileWorker.DoWork += (sender, e) => loadWaveForm(true, width, height, wasPlaying, play);
                fileWorker.RunWorkerCompleted += (sender, e) =>
                {
                    WaveFormProgress.IsIndeterminate = true;
                    ViewModel.IsWaveFormLoading = false;
                };
                fileWorker.RunWorkerAsync();
            }
            else
            {
                ViewModel.IsWaveFormLoading = true;
                loadWaveForm(false, width, height, wasPlaying, play);
                ViewModel.IsWaveFormLoading = false;
            }
        }

        // ReSharper disable InconsistentNaming

        private void loadWaveForm(bool inBackgroundThread, double width, double height, bool wasPlaying, bool play)
        {
            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            int byteDepth = ViewModel.PcmFormat.BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((BytesPerPixel * ViewModel.WaveStepX) / byteDepth);
            samplesPerStep += (samplesPerStep % ViewModel.PcmFormat.NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;

            var estimatedCapacity = (int)(width / (bytesPerStep / BytesPerPixel)) + 1;

            var bytes = new byte[bytesPerStep]; // Int 8 unsigned
            var samples = new short[samplesPerStep]; // Int 16 signed

            List<Point> listTopPointsCh1 = null;
            List<Point> listTopPointsCh2 = null;
            List<Point> listBottomPointsCh1 = null;
            List<Point> listBottomPointsCh2 = null;
            if (ViewModel.IsEnvelopeVisible)
            {
                listTopPointsCh1 = new List<Point>(estimatedCapacity);
                listBottomPointsCh1 = new List<Point>(estimatedCapacity);
                if (ViewModel.PcmFormat.NumberOfChannels > 1)
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

            const int tolerance = 5;
            try
            {
                var audioStream = ViewModel.AudioPlayer_GetPlayStream();

                audioStream.Position = 0;
                audioStream.Seek(0, SeekOrigin.Begin);

                /*if (!string.IsNullOrEmpty(ViewModel.FilePath))
                {
                    ViewModel.AudioPlayer_ResetPlayStreamPosition();
                }*/
                double dBMinReached = double.PositiveInfinity;
                double dBMaxReached = double.NegativeInfinity;
                double decibelDrawDelta = (ViewModel.IsUseDecibelsNoAverage ? 0 : 2);

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
                double adjustFactor = ViewModel.DecibelResolution;
                if (adjustFactor != 0)
                {
                    reference *= adjustFactor;
                    //0.707 adjustment to more realistic noise floor value, to avoid clipping (otherwise, use MinValue = -45 or -60 directly)
                }

                double dbMinValue = logFactor * Math.Log10(1.0 / reference); //-90.3 dB
                //double val = reference*Math.Pow(10, MinValue/20); // val == 1.0, just checking

                System.Diagnostics.Debug.Print(dbMinValue + "");

                double dBMinHardCoded = dbMinValue;

                double dbMaxValue = (ViewModel.IsUseDecibelsNoAverage ? -dbMinValue : 0);

                bool firstY1 = true;
                bool firstY1_ = true;

                var geometryCh1 = new StreamGeometry();
                StreamGeometryContext sgcCh1 = geometryCh1.Open();

                StreamGeometry geometryCh2 = null;
                StreamGeometryContext sgcCh2 = null;

                if (ViewModel.PcmFormat.NumberOfChannels > 1)
                {
                    geometryCh2 = new StreamGeometry();
                    sgcCh2 = geometryCh2.Open();
                }

                #region LOOP

                double sumProgress = 0;

                int read;
                while ((read = audioStream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    // converts Int 8 unsigned to Int 16 signed
                    Buffer.BlockCopy(bytes, 0, samples, 0, Math.Min(read, samples.Length));

                    for (int channel = 0; channel < ViewModel.PcmFormat.NumberOfChannels; channel++)
                    {
                        int limit = samples.Length;

                        if (read < bytes.Length)
                        {
                            // ReSharper disable SuggestUseVarKeywordEvident
                            int nSamples = (int)Math.Floor((double)read / byteDepth);
                            // ReSharper restore SuggestUseVarKeywordEvident
                            nSamples = ViewModel.PcmFormat.NumberOfChannels *
                                       (int)Math.Floor((double)nSamples / ViewModel.PcmFormat.NumberOfChannels);
                            limit = nSamples;
                            limit = Math.Min(limit, samples.Length);
                        }

                        double total = 0;
                        int nSamplesRead = 0;

                        double min = short.MaxValue; // Int 16 signed 32767
                        double max = short.MinValue; // Int 16 signed -32768

                        for (int i = channel; i < limit; i += ViewModel.PcmFormat.NumberOfChannels)
                        {
                            nSamplesRead++;

                            short sample = samples[i];
                            if (sample == short.MinValue)
                            {
                                total += short.MaxValue + 1;
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

                        double avg = total / nSamplesRead;

                        double hh = height;
                        if (ViewModel.PcmFormat.NumberOfChannels > 1)
                        {
                            hh /= 2;
                        }

                        // ReSharper disable RedundantAssignment
                        double y1 = 0.0;
                        double y2 = 0.0;
                        // ReSharper restore RedundantAssignment

                        if (ViewModel.IsUseDecibels)
                        {

                            if (!ViewModel.IsUseDecibelsNoAverage)
                            {
                                min = avg;
                                max = avg;
                            }

                            bool minIsNegative = min < 0;
                            double minAbs = Math.Abs(min);
                            if (minAbs == 0)
                            {
                                min = (ViewModel.IsUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                            }
                            else
                            {
                                min = logFactor * Math.Log10(minAbs / reference);
                                dBMinReached = Math.Min(dBMinReached, min);
                                if (ViewModel.IsUseDecibelsNoAverage && !minIsNegative)
                                {
                                    min = -min;
                                }
                            }

                            bool maxIsNegative = max < 0;
                            double maxAbs = Math.Abs(max);
                            if (maxAbs == 0)
                            {
                                max = (ViewModel.IsUseDecibelsNoAverage ? 0 : double.NegativeInfinity);
                            }
                            else
                            {
                                max = logFactor * Math.Log10(maxAbs / reference);
                                dBMaxReached = Math.Max(dBMaxReached, max);
                                if (ViewModel.IsUseDecibelsNoAverage && !maxIsNegative)
                                {
                                    max = -max;
                                }
                            }

                            double totalDbRange = dbMaxValue - dbMinValue;
                            double pixPerDbUnit = hh / totalDbRange;

                            if (ViewModel.IsUseDecibelsNoAverage)
                            {
                                min = dbMinValue - min;
                            }
                            y1 = pixPerDbUnit * (min - dbMinValue) + decibelDrawDelta;
                            if (!ViewModel.IsUseDecibelsNoAverage)
                            {
                                y1 = hh - y1;
                            }
                            if (ViewModel.IsUseDecibelsNoAverage)
                            {
                                max = dbMaxValue - max;
                            }
                            y2 = pixPerDbUnit * (max - dbMinValue) - decibelDrawDelta;
                            if (!ViewModel.IsUseDecibelsNoAverage)
                            {
                                y2 = hh - y2;
                            }
                        }
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

                        if (!(ViewModel.IsUseDecibels && ViewModel.IsUseDecibelsAdjust))
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
                            if (ViewModel.IsEnvelopeVisible && listTopPointsCh1 != null)
                            {
                                listTopPointsCh1.Add(new Point(x, y1));
                            }
                            if (ViewModel.IsWaveFillVisible)
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
                            if (ViewModel.IsEnvelopeVisible && listTopPointsCh2 != null)
                            {
                                listTopPointsCh2.Add(new Point(x, y1));
                            }
                            if (ViewModel.IsWaveFillVisible)
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
                            if (ViewModel.IsWaveFillVisible)
                            {
                                sgcCh1.LineTo(new Point(x, y2), true, false);
                            }
                            if (ViewModel.IsEnvelopeVisible && listBottomPointsCh1 != null)
                            {
                                listBottomPointsCh1.Add(new Point(x, y2));
                            }
                        }
                        else if (sgcCh2 != null)
                        {
                            y2 += hh;

                            if (ViewModel.IsWaveFillVisible)
                            {
                                sgcCh2.LineTo(new Point(x, y2), true, false);
                            }
                            if (ViewModel.IsEnvelopeVisible && listBottomPointsCh2 != null)
                            {
                                listBottomPointsCh2.Add(new Point(x, y2));
                            }
                        }
                    }

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

                    x += (read / BytesPerPixel); //ViewModel.WaveStepX;
                    if (x > width)
                    {
                        break;
                    }
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

                //
                Brush brushColorBars = new SolidColorBrush(ViewModel.ColorWaveBars);

                sgcCh1.Close();
                if (ViewModel.PcmFormat.NumberOfChannels > 1 && sgcCh2 != null)
                {
                    sgcCh2.Close();
                }

                geometryCh1.Freeze();
                var geoDraw1 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh1);
                geoDraw1.Freeze();

                GeometryDrawing geoDraw2 = null;
                if (ViewModel.PcmFormat.NumberOfChannels > 1 && geometryCh2 != null)
                {
                    geometryCh2.Freeze();
                    geoDraw2 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh2);
                    geoDraw2.Freeze();
                }
                //
                GeometryDrawing geoDraw1_envelope = null;
                GeometryDrawing geoDraw2_envelope = null;
                if (ViewModel.IsEnvelopeVisible)
                {
                    createGeometry_envelope(out geoDraw1_envelope, out geoDraw2_envelope,
                                            ref listTopPointsCh1, ref listTopPointsCh2,
                                            ref listBottomPointsCh1, ref listBottomPointsCh2,
                                            dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance,
                                            height, width);
                }
                //
                GeometryDrawing geoDrawMarkers = null;
                if (ViewModel.PlayStreamMarkers != null)
                {
                    geoDrawMarkers = createGeometry_Markers(height);
                }
                //
                GeometryDrawing geoDrawBack = createGeometry_Back(height, width);
                //
                //
                var drawGrp = new DrawingGroup();

                if (ViewModel.IsBackgroundVisible)
                {
                    drawGrp.Children.Add(geoDrawBack);
                }
                if (ViewModel.IsEnvelopeVisible)
                {
                    if (ViewModel.IsEnvelopeFilled)
                    {
                        if (geoDraw1_envelope != null)
                        {
                            drawGrp.Children.Add(geoDraw1_envelope);
                        }
                        if (ViewModel.IsWaveFillVisible)
                        {
                            drawGrp.Children.Add(geoDraw1);
                        }
                    }
                    else
                    {
                        if (ViewModel.IsWaveFillVisible)
                        {
                            drawGrp.Children.Add(geoDraw1);
                        }
                        if (geoDraw1_envelope != null)
                        {
                            drawGrp.Children.Add(geoDraw1_envelope);
                        }
                    }
                }
                else if (ViewModel.IsWaveFillVisible)
                {
                    drawGrp.Children.Add(geoDraw1);
                }
                if (ViewModel.PcmFormat.NumberOfChannels > 1)
                {
                    if (ViewModel.IsEnvelopeVisible)
                    {
                        if (ViewModel.IsEnvelopeFilled)
                        {
                            if (geoDraw2_envelope != null)
                            {
                                drawGrp.Children.Add(geoDraw2_envelope);
                            }
                            if (ViewModel.IsWaveFillVisible && geoDraw2 != null)
                            {
                                drawGrp.Children.Add(geoDraw2);
                            }
                        }
                        else
                        {
                            if (ViewModel.IsWaveFillVisible && geoDraw2 != null)
                            {
                                drawGrp.Children.Add(geoDraw2);
                            }
                            if (geoDraw2_envelope != null)
                            {
                                drawGrp.Children.Add(geoDraw2_envelope);
                            }
                        }
                    }
                    else if (ViewModel.IsWaveFillVisible && geoDraw2 != null)
                    {
                        drawGrp.Children.Add(geoDraw2);
                    }
                }
                if (ViewModel.PlayStreamMarkers != null && geoDrawMarkers != null)
                {
                    drawGrp.Children.Add(geoDrawMarkers);
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

                var drawImg = new DrawingImage(drawGrp);
                drawImg.Freeze();
                m_WaveFormImageSourceDrawingImage = drawImg;

                /*
                WaveFormImage.Source = drawImg;
                 */

                var renderBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                var drawingVisual = new DrawingVisual();
                using (DrawingContext context = drawingVisual.RenderOpen())
                {
                    context.DrawDrawing(drawGrp);
                }
                renderBitmap.Render(drawingVisual);
                renderBitmap.Freeze();

                if (inBackgroundThread)
                {
                    //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        WaveFormImage.Source = renderBitmap;
                        m_WaveFormTimeTicksAdorner.InvalidateVisual();
                    }));
                }
                else
                {
                    WaveFormImage.Source = renderBitmap;
                    m_WaveFormTimeTicksAdorner.InvalidateVisual();
                }
            }
            finally
            {
                ShowHideWaveFormLoadingMessage(false);

                ViewModel.AudioPlayer_UpdateWaveFormPlayHead();

                if (inBackgroundThread)
                {
                    //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() => ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying, play)));
                }
                else
                {
                    ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying, play);
                }
            }
        }

        private GeometryDrawing createGeometry_Markers(double height)
        {
            Brush brushColorMarkers = new SolidColorBrush(ViewModel.ColorMarkers);

            var geometryMarkers = new StreamGeometry();
            using (StreamGeometryContext sgcMarkers = geometryMarkers.Open())
            {
                sgcMarkers.BeginFigure(new Point(0.5, 0), false, false);
                sgcMarkers.LineTo(new Point(0.5, height), true, false);

                long sumData = 0;
                foreach (TreeNodeAndStreamDataLength marker in ViewModel.PlayStreamMarkers)
                {
                    double pixels = (sumData + marker.m_LocalStreamDataLength) / BytesPerPixel;

                    sgcMarkers.BeginFigure(new Point(pixels, 0), false, false);
                    sgcMarkers.LineTo(new Point(pixels, height), true, false);

                    sumData += marker.m_LocalStreamDataLength;
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

        private GeometryDrawing createGeometry_Back(double height, double width)
        {
            var geometryBack = new StreamGeometry();
            using (StreamGeometryContext sgcBack = geometryBack.Open())
            {
                sgcBack.BeginFigure(new Point(0, 0), true, true);
                sgcBack.LineTo(new Point(0, height), false, false);
                sgcBack.LineTo(new Point(width, height), false, false);
                sgcBack.LineTo(new Point(width, 0), false, false);
                sgcBack.Close();
            }
            geometryBack.Freeze();
            Brush brushColorBack = new SolidColorBrush(ViewModel.ColorWaveBackground);
            var geoDrawBack = new GeometryDrawing(brushColorBack, null, geometryBack); //new Pen(brushColorBack, 1.0)
            geoDrawBack.Freeze();

            return geoDrawBack;
        }

        private void createGeometry_envelope(out GeometryDrawing geoDraw1_envelope, out GeometryDrawing geoDraw2_envelope,
            ref List<Point> listTopPointsCh1, ref List<Point> listTopPointsCh2,
            ref List<Point> listBottomPointsCh1, ref List<Point> listBottomPointsCh2,
            double dBMinHardCoded, double dBMinReached,
            double dBMaxReached,
            double decibelDrawDelta, double tolerance,
            double height, double width)
        {
            var geometryCh1_envelope = new StreamGeometry();
            StreamGeometryContext sgcCh1_envelope = geometryCh1_envelope.Open();

            StreamGeometry geometryCh2_envelope = null;
            StreamGeometryContext sgcCh2_envelope = null;

            if (ViewModel.PcmFormat.NumberOfChannels > 1)
            {
                geometryCh2_envelope = new StreamGeometry();
                sgcCh2_envelope = geometryCh2_envelope.Open();
            }

            int bottomIndexStartCh1 = listTopPointsCh1.Count;
            int bottomIndexStartCh2 = listTopPointsCh2.Count;

            if (!ViewModel.IsUseDecibels || ViewModel.IsUseDecibelsNoAverage)
            {
                listBottomPointsCh1.Reverse();
                listTopPointsCh1.AddRange(listBottomPointsCh1);
                listBottomPointsCh1.Clear();

                if (ViewModel.PcmFormat.NumberOfChannels > 1)
                {
                    listBottomPointsCh2.Reverse();
                    listTopPointsCh2.AddRange(listBottomPointsCh2);
                    listBottomPointsCh2.Clear();
                }
            }

            if (ViewModel.IsUseDecibels && ViewModel.IsUseDecibelsAdjust &&
                (dBMinHardCoded != dBMinReached ||
                (ViewModel.IsUseDecibelsNoAverage && (-dBMinHardCoded) != dBMaxReached)))
            {
                var listNewCh1 = new List<Point>(listTopPointsCh1.Count);
                var listNewCh2 = new List<Point>(listTopPointsCh2.Count);

                double hh = height;
                if (ViewModel.PcmFormat.NumberOfChannels > 1)
                {
                    hh /= 2;
                }

                double range = ((ViewModel.IsUseDecibelsNoAverage ? -dBMinHardCoded : 0) - dBMinHardCoded);
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


                    double newRange = ((ViewModel.IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                    double pixPerDbUnit_new = hh / newRange;

                    double dB;
                    if (ViewModel.IsUseDecibelsNoAverage)
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

                if (ViewModel.PcmFormat.NumberOfChannels > 1)
                {
                    index = -1;

                    foreach (Point p in listTopPointsCh2)
                    {
                        index++;

                        p2.X = p.X;
                        p2.Y = p.Y;

                        double newRange = ((ViewModel.IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                        double pixPerDbUnit_new = hh / newRange;

                        double dB;
                        if (ViewModel.IsUseDecibelsNoAverage)
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
                    sgcCh1_envelope.BeginFigure(pp, ViewModel.IsEnvelopeFilled && (!ViewModel.IsUseDecibels || ViewModel.IsUseDecibelsNoAverage), false);
                }
                else
                {
                    sgcCh1_envelope.LineTo(pp, true, false);
                }
                count++;
            }
            if (ViewModel.PcmFormat.NumberOfChannels > 1 && sgcCh2_envelope != null)
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
                        sgcCh2_envelope.BeginFigure(pp, ViewModel.IsEnvelopeFilled && (!ViewModel.IsUseDecibels || ViewModel.IsUseDecibelsNoAverage), false);
                    }
                    else
                    {
                        sgcCh2_envelope.LineTo(pp, true, false);
                    }
                    count++;
                }
            }

            sgcCh1_envelope.Close();
            if (ViewModel.PcmFormat.NumberOfChannels > 1 && sgcCh2_envelope != null)
            {
                sgcCh2_envelope.Close();
            }

            Brush brushColorEnvelopeOutline = new SolidColorBrush(ViewModel.ColorEnvelopeOutline);
            Brush brushColorEnvelopeFill = new SolidColorBrush(ViewModel.ColorEnvelopeFill);

            geometryCh1_envelope.Freeze();
            geoDraw1_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh1_envelope);
            geoDraw1_envelope.Freeze();

            geoDraw2_envelope = null;
            if (ViewModel.PcmFormat.NumberOfChannels > 1 && geometryCh2_envelope != null)
            {
                geometryCh2_envelope.Freeze();
                geoDraw2_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh2_envelope);
                geoDraw2_envelope.Freeze();
            }
        }

        // ReSharper restore InconsistentNaming
    }
}
