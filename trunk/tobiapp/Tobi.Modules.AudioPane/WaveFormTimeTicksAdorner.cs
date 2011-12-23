using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AudioLib;
using Tobi.Common.UI.XAML;
using urakawa.core;
using urakawa.data;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    public class WaveFormTimeTicksAdorner : Adorner
    {
        private AudioPaneView m_AudioPaneView;
        private AudioPaneViewModel m_AudioPaneViewModel;

        //private double m_standardTextHeight;

        public WaveFormTimeTicksAdorner(FrameworkElement adornedElement, AudioPaneView view, AudioPaneViewModel viewModel)
            : base(adornedElement)
        {
            //m_standardTextHeight = -1;

            IsHitTestVisible = false;
            ClipToBounds = true;

            m_AudioPaneView = view;
            m_AudioPaneViewModel = viewModel;

            //MouseMove += OnAdornerMouseMove;
            //MouseLeave += OnAdornerMouseLeave;

            ResetBrushes();

            m_point1 = new Point(1, 1);
            m_point2 = new Point(1, 1);

            m_typeFace = new Typeface("Helvetica");

            m_culture = CultureInfo.GetCultureInfo("en-us");

            m_point3 = new Point(1, 1);

            m_rectRect = new Rect(1, 1, 1, 1);
        }

        private double m_zoom = 1.0;
        private ScaleTransform m_cachedScaleTransform = new ScaleTransform(1, 1);
        private ScaleTransform checkTransform()
        {
            m_zoom = (m_AudioPaneView.m_ShellView != null
                            ? m_AudioPaneView.m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

            long zoomNormalized = (long)Math.Round(m_zoom * 1000);
            if (zoomNormalized == 1000)
            {
                return null;
            }
            else
            {
                long scaleNormalized = (long)Math.Round(m_cachedScaleTransform.ScaleX * 1000);
                
                double inverseZoom = 1 / m_zoom;
                long inverseZoomNormalized = (long)Math.Round(inverseZoom * 1000);
                
                if (scaleNormalized != inverseZoomNormalized)
                {
                    m_cachedScaleTransform.ScaleX = inverseZoom;
                    m_cachedScaleTransform.ScaleY = inverseZoom;
                }
                return m_cachedScaleTransform;
            }
        }

        public void ResetBrushes()
        {
            checkTransform();

            m_renderBrush = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Back); // { Opacity = 0.8 };
            //m_renderBrush.Freeze();

            m_phraseBrush = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Phrases); //m_AudioPaneViewModel.ColorMarkers);
            //m_phraseBrush.Freeze();

            m_timeTextBrush = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_TimeText);//m_AudioPaneViewModel.ColorTimeInfoText
            //m_timeTextBrush.Freeze();

            m_penTick = new Pen(m_timeTextBrush, 1);
            m_penTick.Freeze();

            m_penPhrases = new Pen(m_phraseBrush, 1);
            m_penPhrases.Freeze();
        }

        private Typeface m_typeFace;
        private CultureInfo m_culture;
        private double m_MousePosX = -1;

        private Pen m_penTick;
        private Pen m_penPhrases;


        private SolidColorBrush m_renderBrush;
        private SolidColorBrush m_phraseBrush;
        private SolidColorBrush m_timeTextBrush;

        private Point m_point1;
        private Point m_point2;
        private Point m_point3;
        private Point m_point4;
        private Rect m_rectRect;

        public const double m_horizontalMargin = 2;

        public const double m_tickHeight = 3;


        public void OnAdornerMouseLeave(object sender, MouseEventArgs e)
        {
            m_MousePosX = -1;
            InvalidateVisual();
        }

        public void OnAdornerMouseMove(object sender, MouseEventArgs e)
        {
            m_MousePosX = e.GetPosition(AdornedElement).X;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!m_AudioPaneViewModel.State.Audio.HasContent || m_AudioPaneView.BytesPerPixel <= 0)
            {
                return;
            }

            ScaleTransform trans = checkTransform();

            double heightAvailable = ((FrameworkElement)AdornedElement).ActualHeight;
            double widthAvailable = ((FrameworkElement)AdornedElement).ActualWidth;

            heightAvailable = m_AudioPaneView.WaveFormScroll.ViewportHeight;
            widthAvailable = m_AudioPaneView.WaveFormScroll.ViewportWidth;

            double hoffset = m_AudioPaneView.WaveFormScroll.HorizontalOffset;

            //if (m_AudioPaneViewModel.State.Audio.PlayStreamMarkers != null
            //    && m_AudioPaneViewModel.State.Audio.PlayStreamMarkers.Count <= Settings.Default.AudioWaveForm_TextCacheRenderThreshold)
            //{
            //    drawChunkInfos(null, drawingContext, null, hoffset, heightAvailable, widthAvailable, m_AudioPaneView.BytesPerPixel,
            //        1 //m_zoom
            //        , trans);
            //}

            if (m_AudioPaneViewModel.State.Audio.PlayStreamMarkers != null)
            {
                drawChunkInfos(drawingContext, hoffset, heightAvailable, widthAvailable, m_AudioPaneView.BytesPerPixel, trans);
            }

            drawTimeRuler(drawingContext, hoffset, heightAvailable, widthAvailable, trans);

            drawMouseOver(drawingContext, hoffset, heightAvailable, widthAvailable, trans);
        }

        /* var brush = ColorBrushCache.Get(Colors.Red) { Opacity = 0.0 };
        drawingContext.DrawRectangle(brush, null,
                                         new Rect(new Point(0, 0),
                                                  new Size(widthAvailable,
                                                           heightAvailable))); */

        /*if (m_MousePosX != -1)
         {

             var formattedTextTMP = new FormattedText(
                 "test",
                 CultureInfo.GetCultureInfo("en-us"),
                 FlowDirection.LeftToRight,
                 new Typeface("Helvetica"),
                 12,
                 Brushes.White
                 );
             drawingContext.DrawRectangle(renderBrush, null,
                                          new Rect(new Point(0, 0),
                                                   new Size(widthAvailable,
                                                            m_tickHeight + m_tickHeight + formattedTextTMP.Height)));
         }*/

        private void drawTimeRuler(DrawingContext drawingContext, double hoffset, double heightAvailable, double widthAvailable, ScaleTransform trans)
        {
            double widthAvailableWaveFormOnly =
                Math.Min(widthAvailable, m_AudioPaneViewModel.State.Audio.DataLength / m_AudioPaneView.BytesPerPixel);

            long minorTickInterval_milliseconds = 1000; //1s minor ticks
            double minorTickInterval_pixels =
                m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes(minorTickInterval_milliseconds * AudioLibPCMFormat.TIME_UNIT) / m_AudioPaneView.BytesPerPixel;

            const double idealTickInterval = 20;

            bool tooSmall = minorTickInterval_pixels < idealTickInterval;
            bool tooLarge = minorTickInterval_pixels > idealTickInterval;

            bool needReAdjusting = false;
            if (tooSmall || tooLarge)
            {
                minorTickInterval_pixels = idealTickInterval;

                minorTickInterval_milliseconds =
                    m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                    m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                    (long)Math.Round(m_AudioPaneView.BytesPerPixel * minorTickInterval_pixels))) / AudioLibPCMFormat.TIME_UNIT;

                if (minorTickInterval_milliseconds > 0)
                {
                    if (minorTickInterval_milliseconds % 10 != 0)
                    {
                        minorTickInterval_milliseconds = (long)Math.Truncate(Math.Round(minorTickInterval_milliseconds / 10.0) * 10);
                        if (minorTickInterval_milliseconds > 0)
                        {
                            minorTickInterval_pixels =
                                m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes(minorTickInterval_milliseconds * AudioLibPCMFormat.TIME_UNIT) / m_AudioPaneView.BytesPerPixel;
                        }
                        else
                        {
                            needReAdjusting = true;
                        }
                    }
                }
                else
                {
                    needReAdjusting = true;
                }
            }

            int numberOfEntireTicksHidden = (int)Math.Floor(hoffset / minorTickInterval_pixels);

            double firstTickX = minorTickInterval_pixels -
                                (hoffset - (numberOfEntireTicksHidden * minorTickInterval_pixels));

            //drawingContext.PushOpacity(0.6);

            int count = numberOfEntireTicksHidden;
            double currentTickX = firstTickX;
            while (currentTickX <= widthAvailableWaveFormOnly)
            {
                count++;

                if (count % 5 == 0)
                {
                    m_point1.X = currentTickX;
                    m_point1.Y = 0;

                    m_point2.X = currentTickX;
                    m_point2.Y = m_tickHeight * 2;

                    drawingContext.DrawLine(m_penTick, m_point1, m_point2);

                    if (trans != null)
                    {
                        drawingContext.PushTransform(trans);
                    }

                    long timeInLocalUnits = m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                        m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                        (long)Math.Round(m_AudioPaneView.BytesPerPixel * (hoffset + currentTickX))));

                    var formattedText = new FormattedText(
                        AudioPaneViewModel.FormatTimeSpan_Units(new Time(timeInLocalUnits)),
                        m_culture,
                        FlowDirection.LeftToRight,
                        m_typeFace,
                        12 * (trans != null ? m_zoom : 1),
                        m_timeTextBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
);
                    double formattedTextWidth = formattedText.Width;
                    double formattedTextHeight = formattedText.Height;
                    if (trans != null)
                    {
                        formattedTextWidth *= (1 / m_zoom);
                        formattedTextHeight *= (1 / m_zoom);
                    }

                    double posX = currentTickX - formattedTextWidth / 2;

                    m_point3.X = posX;
                    m_point3.Y = m_tickHeight * 2;

                    m_rectRect.X = m_point3.X - m_horizontalMargin;
                    m_rectRect.Y = m_point3.Y;
                    m_rectRect.Width = Math.Min(formattedTextWidth + m_horizontalMargin * 2, widthAvailableWaveFormOnly - m_rectRect.X);
                    m_rectRect.Height = formattedTextHeight;

                    if (trans != null)
                    {
                        m_point3.X *= m_zoom;
                        m_point3.Y *= m_zoom;

                        m_rectRect.X *= m_zoom;
                        m_rectRect.Y *= m_zoom;
                        m_rectRect.Width *= m_zoom;
                        m_rectRect.Height *= m_zoom;
                    }

                    if (m_MousePosX != -1)
                    {
                        //drawingContext.Pop();

                        drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                        //drawingContext.PushOpacity(0.6);
                    }

                    var clipGeo = new RectangleGeometry(m_rectRect);
                    clipGeo.Freeze();
                    drawingContext.PushClip(clipGeo);

                    drawingContext.DrawText(formattedText, m_point3);

                    drawingContext.Pop();

                    if (trans != null)
                    {
                        drawingContext.Pop();
                    }
                }
                else
                {
                    m_point1.X = currentTickX;
                    m_point1.Y = 0;

                    m_point2.X = currentTickX;
                    m_point2.Y = m_tickHeight;

                    drawingContext.DrawLine(m_penTick, m_point1, m_point2);
                }

                currentTickX += minorTickInterval_pixels;
            }
        }

        private void drawMouseOver(DrawingContext drawingContext, double hoffset, double heightAvailable, double widthAvailable, ScaleTransform trans)
        {
            if (m_MousePosX >= 0)
            {
                long timeInLocalUnits = m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                    m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                    (long)Math.Round(m_AudioPaneView.BytesPerPixel * (hoffset + m_MousePosX))));

                var formattedText = new FormattedText(
                    AudioPaneViewModel.FormatTimeSpan_Units(new Time(timeInLocalUnits)),
                    m_culture,
                    FlowDirection.LeftToRight,
                    m_typeFace,
                    12 * (trans != null ? m_zoom : 1),
                    m_timeTextBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
);

                m_point1.X = m_MousePosX;
                m_point1.Y = 0;

                m_point2.X = m_MousePosX;
                m_point2.Y = heightAvailable;

                drawingContext.DrawLine(m_penTick, m_point1, m_point2);

                if (trans != null)
                {
                    drawingContext.PushTransform(trans);
                }

                double formattedTextWidth = formattedText.Width;
                double formattedTextHeight = formattedText.Height;
                if (trans != null)
                {
                    formattedTextWidth *= (1 / m_zoom);
                    formattedTextHeight *= (1 / m_zoom);
                }

                double xPos = Math.Max(5, m_MousePosX - formattedTextWidth / 2);
                xPos = Math.Min(xPos, widthAvailable - formattedTextWidth - 5);
                xPos = Math.Max(5, xPos);

                //drawingContext.Pop(); //PushOpacity

                m_point3.X = xPos;
                //m_point3.Y = heightAvailable - formattedText.Height - m_tickHeight;
                m_point3.Y = formattedTextHeight + m_tickHeight + m_tickHeight;


                m_rectRect.X = m_point3.X - m_horizontalMargin;
                m_rectRect.Y = m_point3.Y;
                m_rectRect.Width = formattedTextWidth + m_horizontalMargin * 2;
                m_rectRect.Height = formattedTextHeight;

                if (trans != null)
                {
                    m_point3.X *= m_zoom;
                    m_point3.Y *= m_zoom;

                    m_rectRect.X *= m_zoom;
                    m_rectRect.Y *= m_zoom;
                    m_rectRect.Width *= m_zoom;
                    m_rectRect.Height *= m_zoom;
                }

                drawingContext.DrawRectangle(m_renderBrush, m_penTick, m_rectRect);

                drawingContext.DrawText(formattedText, m_point3);

                if (trans != null)
                {
                    drawingContext.Pop();
                }
            }
        }


        public void drawChunkInfos(DrawingContext drawingContext,
            double hoffset, double heightAvailable, double widthAvailable, double bytesPerPixel
            , ScaleTransform trans)
        {
            //double xZoomed = imageAndDraw == null ? -1 : imageAndDraw.m_originalX * zoom;
            //double wZoomed = imageAndDraw == null ? -1 : imageAndDraw.m_originalW * zoom;

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_AudioPaneViewModel.m_UrakawaSession.GetTreeNodeSelection();
            if (treeNodeSelection.Item1 != null && m_AudioPaneViewModel.State.Audio.PlayStreamMarkers != null)
            {
                //drawingContext.Pop(); //PushOpacity

                long sumData = 0;
                double pixelsLeft = 0;
                double pixelsRight = 0;
                double widthChunk = 0;
#if USE_NORMAL_LIST
                foreach (TreeNodeAndStreamDataLength marker in m_AudioPaneViewModel.State.Audio.PlayStreamMarkers)
                {
#else
                LightLinkedList<TreeNodeAndStreamDataLength>.Item current = m_AudioPaneViewModel.State.Audio.PlayStreamMarkers.m_First;
                while (current != null)
                {
                    TreeNodeAndStreamDataLength marker = current.m_data;
#endif //USE_NORMAL_LIST
                    if (pixelsLeft > (hoffset + widthAvailable))
                    {
                        break;
                    }

                    if (pixelsLeft > hoffset)
                    {
                        m_point1.X = pixelsLeft - hoffset;
                        m_point2.X = m_point1.X;
                        m_point1.Y = 0;
                        m_point2.Y = heightAvailable;
                        drawingContext.DrawLine(m_penPhrases, m_point1, m_point2);
                    }

                    pixelsRight = (sumData + marker.m_LocalStreamDataLength) / bytesPerPixel;

                    if (pixelsRight > hoffset && pixelsRight < (hoffset + widthAvailable))
                    {
                        m_point1.X = pixelsRight - hoffset;
                        m_point2.X = m_point1.X;
                        m_point1.Y = 0;
                        m_point2.Y = heightAvailable;
                        drawingContext.DrawLine(m_penPhrases, m_point1, m_point2);
                    }


                    widthChunk = pixelsRight - pixelsLeft;
                    if (pixelsRight > hoffset && pixelsLeft < (hoffset + widthAvailable))
                    {
                        if (widthChunk < 20)
                        {
                            sumData += marker.m_LocalStreamDataLength;
                            pixelsLeft = pixelsRight;

#if !USE_NORMAL_LIST
                            current = current.m_nextItem;
#endif //USE_NORMAL_LIST

                            continue;
                        }

                        if (trans != null)
                        {
                            drawingContext.PushTransform(trans);
                        }


                        long timeInLocalUnits = m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                            m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                            (long)Math.Round(bytesPerPixel * (pixelsRight - pixelsLeft))));

                        double tickHeight = m_tickHeight;

                        double chunkWidthForText = widthChunk - tickHeight - tickHeight - 1;

                        //                        if (m_standardTextHeight <= 0)
                        //                        {
                        //                            var txt = new FormattedText("Test Height",
                        //                                                                  m_culture,
                        //                                                                  FlowDirection.LeftToRight,
                        //                                                                  m_typeFace,
                        //                                                                  12,
                        //                                                                  m_phraseBrush
                        //#if NET40
                        //, null, TextFormattingMode.Display
                        //#endif //NET40
                        //);
                        //                            m_standardTextHeight = txt.Height;
                        //                        }

                        var formattedTextDuration = new FormattedText(
                                                AudioPaneViewModel.FormatTimeSpan_Units(new Time(timeInLocalUnits)),
                                                              m_culture,
                                                              FlowDirection.LeftToRight,
                                                              m_typeFace,
                                                              12 * (trans != null ? m_zoom : 1),
                                                              m_timeTextBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
);

                        double formattedTextDurationWidth = formattedTextDuration.Width;
                        double formattedTextDurationHeight = formattedTextDuration.Height;
                        if (trans != null)
                        {
                            formattedTextDurationWidth *= (1 / m_zoom);
                            formattedTextDurationHeight *= (1 / m_zoom);
                        }

                        formattedTextDuration.Trimming = TextTrimming.CharacterEllipsis;
                        formattedTextDuration.MaxTextWidth = chunkWidthForText * (trans != null ? m_zoom : 1);
                        formattedTextDuration.MaxTextHeight = (formattedTextDurationHeight + tickHeight) * (trans != null ? m_zoom : 1);

                        double horizontalMargin = m_horizontalMargin;

                        m_point3.X = pixelsLeft - hoffset + horizontalMargin + tickHeight;
                        if (m_point3.X < tickHeight)
                        {
                            widthChunk += m_point3.X;
                            m_point3.X = tickHeight;
                        }

                        m_point3.Y = heightAvailable - (formattedTextDurationHeight * 2) - tickHeight - tickHeight - tickHeight;

                        double diff = (pixelsRight - hoffset) - widthAvailable;
                        if (diff > 0)
                        {
                            widthChunk -= diff;
                        }

                        double minW = Math.Min(formattedTextDurationWidth + horizontalMargin + horizontalMargin,
                                                widthChunk - tickHeight - tickHeight - 1);
                        if (minW > 0)
                        {
                            m_rectRect.X = m_point3.X - horizontalMargin;
                            m_rectRect.Y = m_point3.Y;
                            m_rectRect.Width = minW;
                            m_rectRect.Height = formattedTextDurationHeight;

                            //if (drawingGroup != null)
                            //{
                            //    if (imageAndDraw == null
                            //        ||
                            //        (
                            //        m_rectRect.Left >= xZoomed
                            //        && m_rectRect.Left < xZoomed + wZoomed
                            //        ||
                            //        m_rectRect.Right > xZoomed
                            //        && m_rectRect.Right <= xZoomed + wZoomed
                            //        )
                            //        )
                            //    {
                            //        m_rectRect.X -= xZoomed;
                            //        if (m_rectRect.X <0)
                            //        {
                            //            m_rectRect.Width -= -m_rectRect.X;
                            //            m_rectRect.X = 0;
                            //        }
                            //        var rectGeo = new RectangleGeometry(m_rectRect);
                            //        rectGeo.Freeze();
                            //        var rectGeoDraw = new GeometryDrawing(m_renderBrush, null, rectGeo);
                            //        rectGeoDraw.Freeze();
                            //        drawingGroup.Children.Add(rectGeoDraw);

                            //        m_point4.X = m_point3.X;
                            //        m_point4.Y = m_point3.Y;
                            //        m_point4.X -= xZoomed;
                            //        if (m_point4.X >= 0)
                            //        {
                            //            var textGeo = formattedTextDuration.BuildGeometry(m_point4);
                            //            textGeo.Freeze();
                            //            var textGeoDraw = new GeometryDrawing(m_timeTextBrush, null, textGeo);
                            //            textGeoDraw.Freeze();
                            //            drawingGroup.Children.Add(textGeoDraw);
                            //        }
                            //    }
                            //}
                            //else
                            //{


                            if (trans != null)
                            {
                                m_point3.X *= m_zoom;
                                m_point3.Y *= m_zoom;

                                m_rectRect.X *= m_zoom;
                                m_rectRect.Y *= m_zoom;
                                m_rectRect.Width *= m_zoom;
                                m_rectRect.Height *= m_zoom;
                            }


                            drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                            var clipGeo = new RectangleGeometry(m_rectRect);
                            clipGeo.Freeze();
                            drawingContext.PushClip(clipGeo);

                            drawingContext.DrawText(formattedTextDuration, m_point3);

                            drawingContext.Pop(); //PushClip
                            //}
                        }

                        if (chunkWidthForText <= 10)
                        {
                            sumData += marker.m_LocalStreamDataLength;
                            pixelsLeft = pixelsRight;

#if !USE_NORMAL_LIST
                            current = current.m_nextItem;
#endif //USE_NORMAL_LIST

                            if (trans != null)
                            {
                                drawingContext.Pop();
                            }
                            continue;
                        }

                        //QualifiedName qName = marker.m_TreeNode.GetXmlElementQName();
                        //string imgAlt = null;
                        //if (qName != null && qName.LocalName.ToLower() == "img")
                        //{
                        //    XmlAttribute xmlAttr = marker.m_TreeNode.GetXmlProperty().GetAttribute("alt");
                        //    if (xmlAttr != null)
                        //    {
                        //        imgAlt = xmlAttr.Value;
                        //    }
                        //}
                        //string nodeTxt = !String.IsNullOrEmpty(imgAlt) ? imgAlt : marker.m_TreeNode.GetTextMediaFlattened(false);

                        string nodeTxt = marker.m_TreeNode.GetTextFlattened(true);

                        if (!String.IsNullOrEmpty(nodeTxt))
                        {
                            nodeTxt = nodeTxt.Replace("\r\n", "");
                            nodeTxt = nodeTxt.Replace("\n", "");
                            nodeTxt = nodeTxt.Replace(Environment.NewLine, "");
                            nodeTxt = nodeTxt.Trim();

                            var formattedText = new FormattedText(nodeTxt,
                                                                  m_culture,
                                                                  FlowDirection.LeftToRight,
                                                                  m_typeFace,
                                                                  12 * (trans != null ? m_zoom : 1),
                                                                  m_phraseBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
);

                            formattedText.Trimming = TextTrimming.CharacterEllipsis;
                            formattedText.MaxTextWidth = chunkWidthForText * (trans != null ? m_zoom : 1);
                            formattedText.MaxTextHeight = (formattedTextDurationHeight + tickHeight) * (trans != null ? m_zoom : 1);

                            double formattedTextWidth = formattedText.Width;
                            double formattedTextHeight = formattedText.Height;
                            if (trans != null)
                            {
                                formattedTextWidth *= (1 / m_zoom);
                                formattedTextHeight *= (1 / m_zoom);
                            }
                            //FormattedText formattedTextDots = null;

                            m_point3.Y = heightAvailable - formattedTextDurationHeight - tickHeight - tickHeight;

                            minW = Math.Min(formattedTextWidth + horizontalMargin + horizontalMargin,
                                                widthChunk - tickHeight - tickHeight - 1); //chunkWidthForText
                            if (minW > 0)
                            {
                                m_rectRect.X = m_point3.X / m_zoom - horizontalMargin;
                                m_rectRect.Y = m_point3.Y;
                                m_rectRect.Width = minW;
                                m_rectRect.Height = formattedTextDurationHeight;

                                //if (drawingGroup != null)
                                //{
                                //    if (imageAndDraw == null
                                //        ||
                                //        (
                                //        m_rectRect.Left >= xZoomed
                                //        && m_rectRect.Left < xZoomed + wZoomed
                                //        ||
                                //        m_rectRect.Right > xZoomed
                                //        && m_rectRect.Right <= xZoomed + wZoomed
                                //        )
                                //        )
                                //    {
                                //        m_rectRect.X -= xZoomed;
                                //        if (m_rectRect.X < 0)
                                //        {
                                //            m_rectRect.Width -= -m_rectRect.X;
                                //            m_rectRect.X = 0;
                                //        }
                                //        var rectGeo = new RectangleGeometry(m_rectRect);
                                //        rectGeo.Freeze();
                                //        var rectGeoDraw = new GeometryDrawing(m_renderBrush, null, rectGeo);
                                //        rectGeoDraw.Freeze();
                                //        drawingGroup.Children.Add(rectGeoDraw);

                                //        m_point3.X -= xZoomed;
                                //        if (m_point3.X >= 0)
                                //        {
                                //            var textGeo = formattedText.BuildGeometry(m_point3);
                                //            textGeo.Freeze();
                                //            var textGeoDraw = new GeometryDrawing(m_phraseBrush, null, textGeo);
                                //            textGeoDraw.Freeze();
                                //            drawingGroup.Children.Add(textGeoDraw);
                                //        }
                                //    }
                                //}
                                //else
                                //{
                                //drawingContext.PushOpacity(0.6);


                                if (trans != null)
                                {
                                    //m_point3.X *= m_zoom; ==> X is "borrowed" from previous text render, already zoomed
                                    m_point3.Y *= m_zoom;

                                    m_rectRect.X *= m_zoom;
                                    m_rectRect.Y *= m_zoom;
                                    m_rectRect.Width *= m_zoom;
                                    m_rectRect.Height *= m_zoom;
                                }


                                drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                                //Boolean mouseIn = m_MousePosX >= (pixelsLeft - hoffset) &&
                                //                  m_MousePosX < (pixelsRight - hoffset);
                                //if (mouseIn)
                                //{
                                //    //drawingContext.Pop(); //PushOpacity
                                //}
                                var clipGeo = new RectangleGeometry(m_rectRect);
                                clipGeo.Freeze();
                                drawingContext.PushClip(clipGeo);

                                drawingContext.DrawText(formattedText, m_point3);

                                drawingContext.Pop(); //PushClip

                                //if (!mouseIn)
                                //{
                                //    //drawingContext.Pop(); //PushOpacity
                                //}

                                //if (false && formattedText.Width >= minW)
                                //{
                                //    formattedTextDots = new FormattedText(" ...",
                                //                                      m_culture,
                                //                                      FlowDirection.LeftToRight,
                                //                                      m_typeFace,
                                //                                      12,
                                //                                      m_timeTextBrush
                                //    );
                                //}

                                //if (formattedTextDots != null && formattedTextDots.Width < minW)
                                //{
                                //    m_point3.X = m_rectRect.X + m_rectRect.Width - formattedTextDots.Width;
                                //    m_rectRect.X = m_point3.X;
                                //    m_rectRect.Width = formattedTextDots.Width;

                                //    drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);
                                //    drawingContext.DrawText(formattedTextDots, m_point3);
                                //}
                                //}
                            }
                        }

                        if (trans != null)
                        {
                            drawingContext.Pop();
                        }
                    }

                    sumData += marker.m_LocalStreamDataLength;
                    pixelsLeft = pixelsRight;

#if USE_NORMAL_LIST
                }
#else
                    current = current.m_nextItem;
                }
#endif //USE_NORMAL_LIST
                //drawingContext.PushOpacity(0.6);
            }
        }

        private static void drawGlyph(DrawingContext dc, string text)
        {
            Typeface typeface = new Typeface(new FontFamily("Arial"),
                                FontStyles.Italic,
                                FontWeights.Normal,
                                FontStretches.Normal);

            GlyphTypeface glyphTypeface;
            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
                throw new InvalidOperationException("No glyphtypeface found");

            double size = 40;

            ushort[] glyphIndexes = new ushort[text.Length];
            double[] advanceWidths = new double[text.Length];

            double totalWidth = 0;

            for (int n = 0; n < text.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
                glyphIndexes[n] = glyphIndex;

                double width = glyphTypeface.AdvanceWidths[glyphIndex] * size;
                advanceWidths[n] = width;

                totalWidth += width;
            }

            Point origin = new Point(50, 50);

            GlyphRun glyphRun = new GlyphRun(glyphTypeface, 0, false, size,
                glyphIndexes, origin, advanceWidths, null, null, null, null,
                null, null);

            dc.DrawGlyphRun(Brushes.Black, glyphRun);

            double y = origin.Y;
            dc.DrawLine(new Pen(Brushes.Red, 1), new Point(origin.X, y),
                new Point(origin.X + totalWidth, y));

            y -= (glyphTypeface.Baseline * size);
            dc.DrawLine(new Pen(Brushes.Green, 1), new Point(origin.X, y),
                new Point(origin.X + totalWidth, y));

            y += (glyphTypeface.Height * size);
            dc.DrawLine(new Pen(Brushes.Blue, 1), new Point(origin.X, y),
                new Point(origin.X + totalWidth, y));
        }
    }
}
