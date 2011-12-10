using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using AudioLib;
using Tobi.Common.UI;
using urakawa.core;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    public enum WaveFormRenderMethod : byte
    {
        RenderTargetBitmap,
        WriteableBitmap,
        BitmapSource
    }

    public partial class AudioPaneView
    {
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

        private DrawingImage m_WaveFormImageSourceDrawingImage;


        /// <summary>
        /// (DOES NOT ensures invoke on UI Dispatcher thread)
        /// </summary>
        public void RefreshUI_LoadWaveForm(bool wasPlaying)
        {
            ShowHideWaveFormLoadingMessage(true);

            ResetPeakLabels();

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

            if (Settings.Default.AudioWaveForm_SkipDrawing)
            {
                m_ViewModel.IsWaveFormLoading = false;
                //m_BackgroundLoader = null;

                CommandManager.InvalidateRequerySuggested();

                ShowHideWaveFormLoadingMessage(false);

                m_ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying);

                return;
            }

            var zoom = (m_ShellView != null
                            ? m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

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

            var estimatedCapacity = (int)(widthMagnified / (bytesPerStep / bytesPerPixel_Magnified)) + 1;

            //estimatedCapacity * m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 101) //501
            //if (true)
            //{
            bool vertical = WaveFormProgress.Orientation == Orientation.Vertical;
            double sizeProgress = (vertical ? WaveFormScroll.Height : WaveFormScroll.Width);
            if (double.IsNaN(sizeProgress) || sizeProgress == 0)
            {
                sizeProgress = (vertical ? WaveFormScroll.ActualHeight : WaveFormScroll.ActualWidth);
            }

            //WaveFormProgress.SmallChange = 100;

            double numberOfVisibleXIncrements = sizeProgress / 20;
            // progressbar update will be triggered every 35 pixels, which will minimize the Dispatcher access while reading the audio bytes and therefore increase performance.
            double progressStep = estimatedCapacity / numberOfVisibleXIncrements;

            //WaveFormProgress.LargeChange = progressStep;
            m_ProgressVisibleOffset = Math.Floor(progressStep);

            WaveFormProgress.IsIndeterminate = false;
            WaveFormProgress.Value = 0;
            WaveFormProgress.Minimum = 0;
            WaveFormProgress.Maximum = estimatedCapacity;

            ThreadStart threadDelegate = delegate()
                                    {
                                        m_LoadThreadIsAlive = true;
                                        try
                                        {
                                            //Console.WriteLine(@"BEFORE loadWaveForm");

                                            loadWaveForm(widthMagnified, heightMagnified, wasPlaying, bytesPerPixel_Magnified, zoom);

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
                                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                                ExceptionHandler.Handle(ex, false, m_ShellView)));
                                        }
                                        finally
                                        {
                                            //Console.WriteLine(@">>>> SEND BEFORE 1");

                                            Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(() =>
                                            {
                                                //Console.WriteLine(@">>>> SEND IsWaveFormLoading");
                                                m_ViewModel.IsWaveFormLoading = false;
                                                m_LoadThreadIsAlive = false;
                                            }));
                                            //Console.WriteLine(@">>>> SEND BEFORE 2");

                                            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                (Action)(() => m_ViewModel.AudioPlayer_PlayAfterWaveFormLoaded(wasPlaying)));

                                            //Console.WriteLine(@">>>> SEND BEFORE 3");

                                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                                            {
                                                WaveFormProgress.IsIndeterminate = true;
                                                TimeMessageHide();
                                                m_ViewModel.m_TimeStringOther = String.Empty;
                                                ShowHideWaveFormLoadingMessage(false);

                                                CommandManager.InvalidateRequerySuggested();
                                            }));
                                            //Console.WriteLine(@">>>> SEND AFTER 3");
                                        }
                                        m_LoadThreadIsAlive = false;
                                    };

            if (m_LoadThread != null)
            {
                //Console.WriteLine(@"CancelWaveFormLoad !!!!!!!");
                CancelWaveFormLoad(true);
            }

            m_LoadThreadIsAlive = false;

            DebugFix.Assert(m_LoadThread == null);

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


        private void loadWaveForm(double widthMagnified, double heightMagnified, bool wasPlaying, double bytesPerPixel_Magnified, double zoom)
        {
            //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

            int byteDepth = m_ViewModel.State.Audio.PcmFormat.Data.BitDepth / 8; //bytes per sample (data for one channel only)

            var samplesPerStep = (int)Math.Floor((bytesPerPixel_Magnified
                * Settings.Default.AudioWaveForm_Resolution //m_ViewModel.WaveStepX
                ) / byteDepth);
            samplesPerStep += (samplesPerStep % m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels);

            if (samplesPerStep <= 0) return;

            int bytesPerStep = samplesPerStep * byteDepth;

            var bytes = new byte[bytesPerStep]; // Int 8 unsigned
#if USE_BLOCK_COPY
            var samples = new short[samplesPerStep]; // Int 16 signed
#endif //USE_BLOCK_COPY


            double visibleWidth = WaveFormScroll.ViewportWidth * zoom;

            int nStepsScrollVisibleWidth = (int)Math.Floor((visibleWidth * bytesPerPixel_Magnified) / bytesPerStep);
            long nBytesScrollVisibleWidth = Math.Max(0, nStepsScrollVisibleWidth * bytesPerStep);

            double hoffset = WaveFormScroll.HorizontalOffset * zoom;

            int nStepsScrollOffset = (int)Math.Floor((hoffset * bytesPerPixel_Magnified) / bytesPerStep);
            long nBytesScrollOffset = Math.Max(0, nStepsScrollOffset * bytesPerStep);

            bool onlyLoadVisibleScroll = false;

            List<Point> listTopPointsCh1 = null;
            List<Point> listTopPointsCh2 = null;
            List<Point> listBottomPointsCh1 = null;
            List<Point> listBottomPointsCh2 = null;

            var estimatedCapacity = (int)((onlyLoadVisibleScroll ? visibleWidth  * zoom : widthMagnified) / (bytesPerStep / bytesPerPixel_Magnified)) + 1;
            
            if (Settings.Default.AudioWaveForm_IsBordered) //m_ViewModel.IsEnvelopeVisible)
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

            const bool bJoinInterSamples = false;

            if (m_CancelRequested) return;

            Stopwatch stopWatch = null;


            double x = 0.5;
            if (onlyLoadVisibleScroll)
                x += (nBytesScrollOffset / bytesPerPixel_Magnified); //ViewModel.WaveStepX;

            const int tolerance = 5;
            try
            {
                Stream audioStream = m_ViewModel.AudioPlayer_GetPlayStream();

                if (m_CancelRequested) return;

                if (onlyLoadVisibleScroll)
                {
                    audioStream.Position = nBytesScrollOffset;
                    audioStream.Seek(nBytesScrollOffset, SeekOrigin.Begin);
                }
                else
                {
                    audioStream.Position = 0;
                    audioStream.Seek(0, SeekOrigin.Begin);
                }

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

                if (m_CancelRequested) return;

                stopWatch = Stopwatch.StartNew();

                int read;

                #region LOOP

                long totalRead = nBytesScrollOffset;
                while ((read = audioStream.Read(bytes, 0, bytesPerStep)) > 0)
                {
                    totalRead += read;

#if USE_BLOCK_COPY
                    // converts Int 8 unsigned to Int 16 signed
                    Buffer.BlockCopy(bytes, 0, samples, 0, read);
#endif //USE_BLOCK_COPY

                    if (m_CancelRequested) break;

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
                                DebugFix.Assert(sample == 0 || sample == -1);
                            else
                                DebugFix.Assert(sample == sampleDirectFromByteArray);

                            // Little Indian
                            short sampleDirectFromByteArray1 = (short)(byte1 | (byte2 << 8));
                            short sampleDirectFromByteArray2 = (short)(byte1 + byte2 * 256);
                            DebugFix.Assert(sampleDirectFromByteArray1 == sampleDirectFromByteArray2);
                            if (sampleDirectFromByteArray2 == -1)
                                DebugFix.Assert(sample == 0 || sample == -1);
                            else
                            {
                                if (sample != sampleDirectFromByteArray2)
                                {
                                    // Big Indian
                                    short sampleDirectFromByteArray3 = (short) ((byte1 << 8) | byte2);
                                    short sampleDirectFromByteArray4 = (short) (byte1*256 + byte2);
                                    DebugFix.Assert(sampleDirectFromByteArray3 == sampleDirectFromByteArray4);
                                    DebugFix.Assert(sample == sampleDirectFromByteArray4);

                                    if (sample == sampleDirectFromByteArray4)
                                        DebugFix.Assert(sample == sampleDirectFromByteArray2);
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
                            if (Settings.Default.AudioWaveForm_IsBordered //m_ViewModel.IsEnvelopeVisible
                                && listTopPointsCh1 != null)
                            {
                                listTopPointsCh1.Add(new Point(x, y1));
                            }
                            if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
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
                            if (Settings.Default.AudioWaveForm_IsBordered //m_ViewModel.IsEnvelopeVisible
                                && listTopPointsCh2 != null)
                            {
                                listTopPointsCh2.Add(new Point(x, y1));
                            }
                            if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
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
                            if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
                            {
                                sgcCh1.LineTo(new Point(x, y2), true, false);
                            }
                            if (Settings.Default.AudioWaveForm_IsBordered //m_ViewModel.IsEnvelopeVisible
                                && listBottomPointsCh1 != null)
                            {
                                listBottomPointsCh1.Add(new Point(x, y2));
                            }
                        }
                        else if (sgcCh2 != null)
                        {
                            y2 += hh;

                            if (Settings.Default.AudioWaveForm_IsStroked) //m_ViewModel.IsWaveFillVisible)
                            {
                                sgcCh2.LineTo(new Point(x, y2), true, false);
                            }
                            if (Settings.Default.AudioWaveForm_IsBordered //m_ViewModel.IsEnvelopeVisible
                                && listBottomPointsCh2 != null)
                            {
                                listBottomPointsCh2.Add(new Point(x, y2));
                            }
                        }
                    }
                    if (m_CancelRequested) break;

                    stopWatch.Stop();
                    if (stopWatch.ElapsedMilliseconds >= 1000)
                    {
                        drawWaveForm(audioStream, false,
                            sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                            listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                            heightMagnified, widthMagnified,
                            dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified, zoom);

                        stopWatch.Reset();
                    }
                    stopWatch.Start();

                    if (m_CancelRequested) break;

                    if (!Dispatcher.CheckAccess())
                    {
                        sumProgress++;
                        if (sumProgress >= m_ProgressVisibleOffset)
                        {
                            sumProgress = 0;

                            DispatcherOperation op = Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                long timeInLocalUnits = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(totalRead);

                                m_ViewModel.m_TimeStringOther = AudioPaneViewModel.FormatTimeSpan_Units(new Time(timeInLocalUnits));
                                TimeMessageShow();
                                //TimeMessageRefresh();

                                WaveFormProgress.Value += m_ProgressVisibleOffset;
                            }));
                        }
                    }

                    x += (read / bytesPerPixel_Magnified); //ViewModel.WaveStepX;
                    if (x > widthMagnified
                        || (onlyLoadVisibleScroll && totalRead > (nBytesScrollOffset + nBytesScrollVisibleWidth)))
                    {
                        break;
                    }

                    if (m_CancelRequested) break;
                }
                if (stopWatch != null && stopWatch.IsRunning) stopWatch.Stop();

                #endregion LOOP

                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        WaveFormProgress.IsIndeterminate = true;
                        m_ViewModel.m_TimeStringOther = String.Empty;
                        TimeMessageHide();
                    }));
                }

                if (m_CancelRequested && m_CancelInterruptDrawingToo) return;

                drawWaveForm(audioStream, true,
                    sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                    listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                    heightMagnified, widthMagnified,
                    dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified, zoom);

            }
            finally
            {
                if (stopWatch != null && stopWatch.IsRunning) stopWatch.Stop();
            }
        }

        private void drawWaveForm(Stream audioStream, bool freeze, StreamGeometryContext sgcCh1, StreamGeometryContext sgcCh2, StreamGeometry geometryCh1, StreamGeometry geometryCh2, List<Point> listBottomPointsCh1, List<Point> listBottomPointsCh2, List<Point> listTopPointsCh1, List<Point> listTopPointsCh2, double heightMagnified, double widthMagnified, double dBMinHardCoded, double dBMinReached, double dBMaxReached, double decibelDrawDelta, int tolerance, double bytesPerPixel_Magnified, double zoom)
        {
            //Console.WriteLine(@"Drawing waveform...1");

            DrawingGroup drawGrp = drawWaveFormUsingCollectedPoints(audioStream, freeze,
                sgcCh1, sgcCh2, geometryCh1, geometryCh2,
                listBottomPointsCh1, listBottomPointsCh2, listTopPointsCh1, listTopPointsCh2,
                heightMagnified, widthMagnified,
                dBMinHardCoded, dBMinReached, dBMaxReached, decibelDrawDelta, tolerance, bytesPerPixel_Magnified, zoom);

            //Console.WriteLine(@"Drawing waveform...2");

            if (freeze)
            {
                if (Settings.Default.AudioWaveForm_UseVectorAtResize)
                {
                    var drawImg = new DrawingImage(drawGrp);
                    drawImg.Freeze();
                    m_WaveFormImageSourceDrawingImage = drawImg;
                }

                //Console.WriteLine(@"Drawing waveform...freeze2");

                GC.Collect();
                GC.WaitForFullGCComplete(1000);

                Action del = () =>
                {

                    System.Diagnostics.Debug.Print("CACHE WAVEFORM WIDTH = " +
                                                   WaveFormImage.Width + " // " +
                                                   WaveFormImage.ActualWidth);

                    bool drawNET3 = false;
#if NET40
                    if (WaveFormImage.Width <= 2048)
                    {
                        if (Settings.Default.AudioWaveForm_UseVectorAtResize)
                        {
                            WaveFormImage.Source = m_WaveFormImageSourceDrawingImage;
                        }
                        else
                        {
                            var drawImg = new DrawingImage(drawGrp);
                            drawImg.Freeze();
                            WaveFormImage.Source = drawImg;
                        }


                        //var zoom = (m_ShellView != null
                        //                ? m_ShellView.MagnificationLevel
                        //                : (Double)FindResource("MagnificationLevel"));

                        if (
                            WaveFormImage.CacheMode == null
                            || ((BitmapCache)WaveFormImage.CacheMode).RenderAtScale != zoom
                            )
                        {
                            WaveFormImage.SetValue(RenderOptions.ClearTypeHintProperty,
                                                   ClearTypeHint.Auto);
                            WaveFormImage.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
                            WaveFormImage.SetValue(RenderOptions.BitmapScalingModeProperty,
                                                   BitmapScalingMode.Fant);
                            WaveFormImage.SetValue(RenderOptions.CachingHintProperty, CachingHint.Cache);

                            WaveFormImage.SnapsToDevicePixels = false;

                            //WaveFormImage.UseLayoutRounding = true;
                            WaveFormImage.CacheMode = new BitmapCache
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
                        WaveFormImage.CacheMode = null;
                        drawNET3 = true;
                    }
#else
                                     drawNET3 = true;
#endif // ELSE NET40

                    if (drawNET3)
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

                        var renderTargetBitmap = new RenderTargetBitmap((int)widthMagnified,
                                                                        (int)heightMagnified, 96, 96,
                                                                        PixelFormats.Pbgra32);
                        renderTargetBitmap.Render(drawingVisual);
                        renderTargetBitmap.Freeze();

                        WaveFormRenderMethod renderMethod = Settings.Default.AudioWaveForm_RenderMethod;
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
                                var bitmapSource = BitmapSource.Create((int)widthMagnified,
                                                                       (int)heightMagnified, 96, 96,
                                                                       PixelFormats.Pbgra32, null, arrBits,
                                                                       stride);

                                WaveFormImage.Source = bitmapSource;
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

                                WaveFormImage.Source = writeableBitmap;
                            }
                        }
                        else
                        {
                            // Default
                            WaveFormImage.Source = renderTargetBitmap;
                        }
                    }

                    m_WaveFormTimeTicksAdorner.InvalidateVisual();
                    m_WaveFormTimeTicksAdorner.ResetBrushes();
                    m_WaveFormLoadingAdorner.ResetBrushes();
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

                var drawImg = new DrawingImage(drawGrp);
                drawImg.Freeze();

                if (!Dispatcher.CheckAccess())
                {
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
        }


        private DrawingGroup drawWaveFormUsingCollectedPoints(Stream audioStream, bool freeze,
            StreamGeometryContext sgcCh1, StreamGeometryContext sgcCh2,
            StreamGeometry geometryCh1, StreamGeometry geometryCh2,
            List<Point> listBottomPointsCh1, List<Point> listBottomPointsCh2, List<Point> listTopPointsCh1, List<Point> listTopPointsCh2,
            double heightMagnified, double widthMagnified,
            double dBMinHardCoded, double dBMinReached, double dBMaxReached, double decibelDrawDelta, int tolerance,
            double bytesPerPixel_Magnified, double zoom
            )
        {
            Brush brushColorBars = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Stroke); //m_ViewModel.ColorWaveBars);
            brushColorBars.Freeze();

            GeometryDrawing geoDraw1 = null;
            GeometryDrawing geoDraw2 = null;
            if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                && freeze)
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


            GeometryDrawing geoDraw1_envelope = null;
            GeometryDrawing geoDraw2_envelope = null;
            if (Settings.Default.AudioWaveForm_IsBordered) //m_ViewModel.IsEnvelopeVisible)
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
            GeometryDrawing geoDrawStreamSubs = null;
#if DEBUG && DRAW_SUBSTREAMS
            if (freeze)
            {
                geoDrawStreamSubs = createGeometry_StreamSubs(heightMagnified, audioStream, bytesPerPixel_Magnified);
            }
#endif
            //
            GeometryDrawing geoDrawMarkers = null;

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_ViewModel.m_UrakawaSession.GetTreeNodeSelection();

            if (m_ViewModel.State.Audio.PlayStreamMarkers != null && treeNodeSelection.Item1 != null)
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
            if (Settings.Default.AudioWaveForm_IsBordered) //m_ViewModel.IsEnvelopeVisible)
            {
                if (Settings.Default.AudioWaveForm_IsFilled) //m_ViewModel.IsEnvelopeFilled)
                {
                    if (geoDraw1_envelope != null)
                    {
                        drawGrp.Children.Add(geoDraw1_envelope);
                    }
                    if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                        && freeze)
                    {
                        drawGrp.Children.Add(geoDraw1);
                    }
                }
                else
                {
                    if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                        && freeze)
                    {
                        drawGrp.Children.Add(geoDraw1);
                    }
                    if (geoDraw1_envelope != null)
                    {
                        drawGrp.Children.Add(geoDraw1_envelope);
                    }
                }
            }
            else if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                && freeze)
            {
                drawGrp.Children.Add(geoDraw1);
            }
            if (m_ViewModel.State.Audio.PcmFormat.Data.NumberOfChannels > 1)
            {
                if (Settings.Default.AudioWaveForm_IsBordered) //m_ViewModel.IsEnvelopeVisible)
                {
                    if (Settings.Default.AudioWaveForm_IsFilled) //m_ViewModel.IsEnvelopeFilled)
                    {
                        if (geoDraw2_envelope != null)
                        {
                            drawGrp.Children.Add(geoDraw2_envelope);
                        }
                        if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                            && geoDraw2 != null && freeze)
                        {
                            drawGrp.Children.Add(geoDraw2);
                        }
                    }
                    else
                    {
                        if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                            && geoDraw2 != null && freeze)
                        {
                            drawGrp.Children.Add(geoDraw2);
                        }
                        if (geoDraw2_envelope != null)
                        {
                            drawGrp.Children.Add(geoDraw2_envelope);
                        }
                    }
                }
                else if (Settings.Default.AudioWaveForm_IsStroked //m_ViewModel.IsWaveFillVisible
                    && geoDraw2 != null && freeze)
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


            if (m_ViewModel.State.Audio.PlayStreamMarkers != null
                && m_ViewModel.State.Audio.PlayStreamMarkers.Count > Settings.Default.AudioWaveForm_TextPreRenderThreshold)
            {
                //DrawingGroup dGroup = VisualTreeHelper.GetDrawing(WaveFormCanvas);

                //var zoom = (m_ShellView != null
                //                ? m_ShellView.MagnificationLevel
                //                : (Double)FindResource("MagnificationLevel"));

                //DrawingContext dc = drawGrp.Open();

                //var imageDrawing = new ImageDrawing();
                //imageDrawing.Rect = null;

                //var drawingImage = new DrawingImage();
                //drawingImage.Drawing = imageDrawing;

                //var drawGroup = new DrawingGroup();
                //drawGroup.Children.Add(imageDrawing);

                m_WaveFormTimeTicksAdorner.drawChunkInfos(null, drawGrp, 0, heightMagnified, widthMagnified, bytesPerPixel_Magnified, zoom);
                //dc.Close();

                //drawGrp.Children.Add(imageDrawing);
            }




            drawGrp.Freeze();

            return drawGrp;
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
#endif
        private GeometryDrawing createGeometry_Markers(double heightMagnified, double bytesPerPixel_Magnified)
        {
            Brush brushColorMarkers = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Phrases); //m_ViewModel.ColorMarkers);
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

            Brush brushColorBack = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Back); //m_ViewModel.ColorWaveBackground);
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


            if (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage)
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
                            Settings.Default.AudioWaveForm_IsFilled //m_ViewModel.IsEnvelopeFilled
                            && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                            Settings.Default.AudioWaveForm_IsFilled //m_ViewModel.IsEnvelopeFilled
                            && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                                Settings.Default.AudioWaveForm_IsFilled //m_ViewModel.IsEnvelopeFilled
                                && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                                Settings.Default.AudioWaveForm_IsFilled // m_ViewModel.IsEnvelopeFilled
                                && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                            Settings.Default.AudioWaveForm_IsFilled //m_ViewModel.IsEnvelopeFilled
                            && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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
                                Settings.Default.AudioWaveForm_IsFilled //m_ViewModel.IsEnvelopeFilled
                                && (!m_ViewModel.IsUseDecibels || m_ViewModel.IsUseDecibelsNoAverage), false);
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

            Brush brushColorEnvelopeOutline = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Border); //m_ViewModel.ColorEnvelopeOutline);
            brushColorEnvelopeOutline.Freeze();

            Brush brushColorEnvelopeFill = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Fill); //m_ViewModel.ColorEnvelopeFill);
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
