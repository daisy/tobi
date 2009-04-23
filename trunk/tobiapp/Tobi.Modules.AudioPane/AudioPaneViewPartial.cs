using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using urakawa.core;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneView
    {
        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_LoadWaveForm()
        {
            Brush brush1 = new SolidColorBrush(ViewModel.ColorPlayhead);
            WaveFormPlayHeadPath.Stroke = brush1;
            Brush brush2 = new SolidColorBrush(ViewModel.ColorPlayheadFill);
            WaveFormPlayHeadPath.Fill = brush2;
            Brush brush3 = new SolidColorBrush(ViewModel.ColorMarkers);
            WaveFormTimeRangePath.Fill = brush3;
            WaveFormTimeRangePath.Stroke = brush3;

            double stepX = ViewModel.WaveStepX;

            bool bUseDecibels = ViewModel.IsUseDecibels;
            bool bUseDecibelsNoAverage = ViewModel.IsUseDecibelsNoAverage;
            bool bUseDecibelsAdjust = ViewModel.IsUseDecibelsAdjust;
            //bool bUseDecibelsIntensity = IsUseDecibelsIntensity;

            bool bShowBackground = ViewModel.IsBackgroundVisible;
            bool bShowEnvelope = ViewModel.IsEnvelopeVisible;
            bool bFillEnvelope = ViewModel.IsEnvelopeFilled;
            bool bShowSamples = ViewModel.IsWaveFillVisible;
            //bool bAdjustOffsetFix = ViewModel.IsAdjustOffsetFix;

            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            PeakOverloadLabelCh2.Visibility = ViewModel.GetPcmFormat().NumberOfChannels == 1 ? Visibility.Collapsed : Visibility.Visible;

            var geometryCh1 = new StreamGeometry();
            StreamGeometryContext sgcCh1 = geometryCh1.Open();

            var geometryCh1_envelope = new StreamGeometry();
            StreamGeometryContext sgcCh1_envelope = geometryCh1_envelope.Open();

            StreamGeometry geometryCh2 = null;
            StreamGeometryContext sgcCh2 = null;

            StreamGeometry geometryCh2_envelope = null;
            StreamGeometryContext sgcCh2_envelope = null;

            if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
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

            BytesPerPixel = ViewModel.GetDataLength() / width;

            int byteDepth = ViewModel.GetPcmFormat().BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((BytesPerPixel * stepX) / byteDepth);
            samplesPerStep += (samplesPerStep % ViewModel.GetPcmFormat().NumberOfChannels);

            int bytesPerStep = samplesPerStep * byteDepth;

            var bytes = new byte[bytesPerStep]; // Int 8 unsigned
            var samples = new short[samplesPerStep]; // Int 16 signed

            var listTopPointsCh1 = new List<Point>();
            var listTopPointsCh2 = new List<Point>();
            var listBottomPointsCh1 = new List<Point>();
            var listBottomPointsCh2 = new List<Point>();

            double x = 0.5;
            const bool bJoinInterSamples = false;

            const int tolerance = 5;
            try
            {
                if (ViewModel.FilePath.Length > 0)
                {
                    ViewModel.ResetPlayStreamPosition();
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

                const bool bUseDecibelsIntensity = false; // feature removed: no visible changes
                const double logFactor = (bUseDecibelsIntensity ? 10 : 20);

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

                dBMinHardCoded = dbMinValue;

                double dbMaxValue = (bUseDecibelsNoAverage ? -dbMinValue : 0);

                int read = 0;
                while ((read = ViewModel.GetPlayStream().Read(bytes, 0, bytes.Length)) > 0)
                {
                    // converts Int 8 unsigned to Int 16 signed
                    Buffer.BlockCopy(bytes, 0, samples, 0, Math.Min(read, samples.Length));

                    for (int channel = 0; channel < ViewModel.GetPcmFormat().NumberOfChannels; channel++)
                    {
                        int limit = samples.Length;

                        if (read < bytes.Length)
                        {
                            // ReSharper disable SuggestUseVarKeywordEvident
                            int nSamples = (int)Math.Floor((double)read / byteDepth);
                            // ReSharper restore SuggestUseVarKeywordEvident
                            nSamples = ViewModel.GetPcmFormat().NumberOfChannels *
                                       (int)Math.Floor((double)nSamples / ViewModel.GetPcmFormat().NumberOfChannels);
                            limit = nSamples;
                            limit = Math.Min(limit, samples.Length);
                        }

                        double total = 0;
                        int n = 0;

                        double min = short.MaxValue; // Int 16 signed 32767
                        double max = short.MinValue; // Int 16 signed -32768

                        for (int i = channel; i < limit; i += ViewModel.GetPcmFormat().NumberOfChannels)
                        {
                            n++;

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

                        double avg = total / n;

                        double hh = height;
                        if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
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
                            const double MaxValue = short.MaxValue; // Int 16 signed 32767
                            const double MinValue = short.MinValue; // Int 16 signed -32768

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

                    x += (read / BytesPerPixel); //stepX;
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

                    if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
                    {
                        listBottomPointsCh2.Reverse();
                        listTopPointsCh2.AddRange(listBottomPointsCh2);
                        listBottomPointsCh2.Clear();
                    }
                }

                if (bUseDecibels && bUseDecibelsAdjust &&
                    (dBMinHardCoded != dBMinReached ||
                    (ViewModel.IsUseDecibelsNoAverage && (-dBMinHardCoded) != dBMaxReached)))
                {
                    var listNewCh1 = new List<Point>(listTopPointsCh1.Count);
                    var listNewCh2 = new List<Point>(listTopPointsCh2.Count);

                    double hh = height;
                    if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
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


                        double newRange = ((ViewModel.IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                        double pixPerDbUnit_new = hh / newRange;

                        double dB = 0.0;
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

                    if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
                    {
                        index = -1;

                        foreach (Point p in listTopPointsCh2)
                        {
                            index++;

                            p2.X = p.X;
                            p2.Y = p.Y;

                            double newRange = ((ViewModel.IsUseDecibelsNoAverage ? dBMaxReached : 0) - dBMinReached);
                            double pixPerDbUnit_new = hh / newRange;

                            double dB = 0.0;
                            if (ViewModel.IsUseDecibelsNoAverage)
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
                        sgcCh1_envelope.BeginFigure(pp, bFillEnvelope && (!bUseDecibels || bUseDecibelsNoAverage), false);
                    }
                    else
                    {
                        sgcCh1_envelope.LineTo(pp, true, false);
                    }
                    count++;
                }
                if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
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
                if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
                {
                    sgcCh2.Close();
                    sgcCh2_envelope.Close();
                }

                Brush brushColorBars = new SolidColorBrush(ViewModel.ColorWaveBars);
                Brush brushColorEnvelopeOutline = new SolidColorBrush(ViewModel.ColorEnvelopeOutline);
                Brush brushColorEnvelopeFill = new SolidColorBrush(ViewModel.ColorEnvelopeFill);

                //
                geometryCh1.Freeze();
                var geoDraw1 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh1);
                geoDraw1.Freeze();
                //
                geometryCh1_envelope.Freeze();
                var geoDraw1_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh1_envelope);
                geoDraw1_envelope.Freeze();
                //
                GeometryDrawing geoDraw2 = null;
                GeometryDrawing geoDraw2_envelope = null;
                if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
                {
                    geometryCh2.Freeze();
                    geoDraw2 = new GeometryDrawing(brushColorBars, new Pen(brushColorBars, 1.0), geometryCh2);
                    geoDraw2.Freeze();
                    geometryCh2_envelope.Freeze();
                    geoDraw2_envelope = new GeometryDrawing(brushColorEnvelopeFill, new Pen(brushColorEnvelopeOutline, 1.0), geometryCh2_envelope);
                    geoDraw2_envelope.Freeze();
                }
                //

                Brush brushColorMarkers = new SolidColorBrush(ViewModel.ColorMarkers);
                GeometryDrawing geoDrawMarkers = null;
                if (ViewModel.GetPlayStreamMarkers() != null)
                {
                    var geometryMarkers = new StreamGeometry();
                    using (StreamGeometryContext sgcMarkers = geometryMarkers.Open())
                    {
                        sgcMarkers.BeginFigure(new Point(0.5, 0), false, false);
                        sgcMarkers.LineTo(new Point(0.5, height), true, false);

                        long sumData = 0;
                        foreach (TreeNodeAndStreamDataLength markers in ViewModel.GetPlayStreamMarkers())
                        {
                            double pixels = (sumData + markers.m_LocalStreamDataLength) / BytesPerPixel;

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
                //
                var drawGrp = new DrawingGroup();
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
                if (ViewModel.GetPcmFormat().NumberOfChannels > 1)
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
                if (ViewModel.GetPlayStreamMarkers() != null)
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

                RefreshUI_LoadingMessage(false);
            }
            finally
            {
                // ensure the stream is closed before we resume the player
                ViewModel.ClosePlayStream();
            }
        }
    }
}
