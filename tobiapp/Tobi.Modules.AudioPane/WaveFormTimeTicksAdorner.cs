﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AudioLib;
using urakawa.core;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    public class WaveFormTimeTicksAdorner : Adorner
    {
        private AudioPaneView m_AudioPaneView;
        private AudioPaneViewModel m_AudioPaneViewModel;

        private double m_standardTextHeight;

        public WaveFormTimeTicksAdorner(FrameworkElement adornedElement, AudioPaneView view, AudioPaneViewModel viewModel)
            : base(adornedElement)
        {
            m_standardTextHeight = -1;

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

        public void ResetBrushes()
        {
            m_renderBrush = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Back); // { Opacity = 0.8 };
            m_renderBrush.Freeze();

            m_phraseBrush = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_Phrases); //m_AudioPaneViewModel.ColorMarkers);
            m_phraseBrush.Freeze();

            m_timeTextBrush = new SolidColorBrush(Settings.Default.AudioWaveForm_Color_TimeText);//m_AudioPaneViewModel.ColorTimeInfoText
            m_timeTextBrush.Freeze();

            m_penTick = new Pen(m_timeTextBrush, 1);
            m_penTick.Freeze();
        }

        private Typeface m_typeFace;
        private CultureInfo m_culture;
        private double m_MousePosX = -1;
        private Pen m_penTick;

        private SolidColorBrush m_renderBrush;
        private SolidColorBrush m_phraseBrush;
        private SolidColorBrush m_timeTextBrush;

        private Point m_point1;
        private Point m_point2;
        private Point m_point3;
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

            double heightAvailable = ((FrameworkElement)AdornedElement).ActualHeight;
            double widthAvailable = ((FrameworkElement)AdornedElement).ActualWidth;

            heightAvailable = m_AudioPaneView.WaveFormScroll.ViewportHeight;
            widthAvailable = m_AudioPaneView.WaveFormScroll.ViewportWidth;

            double hoffset = m_AudioPaneView.WaveFormScroll.HorizontalOffset;

            drawTimeRuler(drawingContext, hoffset, heightAvailable, widthAvailable);

            if (m_AudioPaneViewModel.State.Audio.PlayStreamMarkers != null
                && m_AudioPaneViewModel.State.Audio.PlayStreamMarkers.Count <= Settings.Default.AudioWaveForm_TextPreRenderThreshold)
            {
                drawChunkInfos(drawingContext, null, hoffset, heightAvailable, widthAvailable, m_AudioPaneView.BytesPerPixel, 1);
            }

            drawMouseOver(drawingContext, hoffset, heightAvailable, widthAvailable);
        }

        /* var brush = new SolidColorBrush(Colors.Red) { Opacity = 0.0 };
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

        private void drawTimeRuler(DrawingContext drawingContext, double hoffset, double heightAvailable, double widthAvailable)
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

                    long timeInLocalUnits = m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                        m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                        (long)Math.Round(m_AudioPaneView.BytesPerPixel * (hoffset + currentTickX))));

                    var formattedText = new FormattedText(
                        AudioPaneViewModel.FormatTimeSpan_Units(new Time(timeInLocalUnits)),
                        m_culture,
                        FlowDirection.LeftToRight,
                        m_typeFace,
                        12,
                        m_timeTextBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
                        );

                    double posX = currentTickX - formattedText.Width / 2;

                    m_point3.X = posX;
                    m_point3.Y = m_tickHeight * 2;

                    m_rectRect.X = m_point3.X - m_horizontalMargin;
                    m_rectRect.Y = m_point3.Y;
                    m_rectRect.Width = Math.Min(formattedText.Width + m_horizontalMargin * 2, widthAvailableWaveFormOnly - m_rectRect.X);
                    m_rectRect.Height = formattedText.Height;

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

        private void drawMouseOver(DrawingContext drawingContext, double hoffset, double heightAvailable, double widthAvailable)
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
                    12,
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

                double xPos = Math.Max(5, m_MousePosX - formattedText.Width / 2);
                xPos = Math.Min(xPos, widthAvailable - formattedText.Width - 5);
                xPos = Math.Max(5, xPos);

                //drawingContext.Pop(); //PushOpacity

                m_point3.X = xPos;
                //m_point3.Y = heightAvailable - formattedText.Height - m_tickHeight;
                m_point3.Y = formattedText.Height + m_tickHeight + m_tickHeight;


                m_rectRect.X = m_point3.X - m_horizontalMargin;
                m_rectRect.Y = m_point3.Y;
                m_rectRect.Width = formattedText.Width + m_horizontalMargin * 2;
                m_rectRect.Height = formattedText.Height;
                drawingContext.DrawRectangle(m_renderBrush, m_penTick, m_rectRect);

                drawingContext.DrawText(formattedText, m_point3);
            }
        }


        public void drawChunkInfos(DrawingContext drawingContext, DrawingGroup drawingGroup, double hoffset, double heightAvailable, double widthAvailable, double bytesPerPixel, double zoom)
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_AudioPaneViewModel.m_UrakawaSession.GetTreeNodeSelection();
            if (treeNodeSelection.Item1 != null && m_AudioPaneViewModel.State.Audio.PlayStreamMarkers != null)
            {
                //drawingContext.Pop(); //PushOpacity

                long sumData = 0;
                double pixelsLeft = 0;
                double pixelsRight = 0;
                double widthChunk = 0;
                foreach (TreeNodeAndStreamDataLength marker in m_AudioPaneViewModel.State.Audio.PlayStreamMarkers)
                {
                    if (pixelsLeft > (hoffset + widthAvailable)) break;

                    pixelsRight = (sumData + marker.m_LocalStreamDataLength) / bytesPerPixel;

                    widthChunk = pixelsRight - pixelsLeft;
                    if (pixelsRight > hoffset && pixelsLeft < (hoffset + widthAvailable))
                    {
                        if (widthChunk < 20 * zoom)
                        {
                            sumData += marker.m_LocalStreamDataLength;
                            pixelsLeft = pixelsRight;
                            continue;
                        }

                        long timeInLocalUnits = m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(
                            m_AudioPaneViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                            (long)Math.Round(bytesPerPixel * (pixelsRight - pixelsLeft))));

                        double tickHeight = m_tickHeight * zoom;

                        double chunkWidthForText = widthChunk - tickHeight - tickHeight - 1;

                        if (m_standardTextHeight <= 0)
                        {
                            var txt = new FormattedText("Test Height",
                                                                  m_culture,
                                                                  FlowDirection.LeftToRight,
                                                                  m_typeFace,
                                                                  12,
                                                                  m_phraseBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
                                );
                            m_standardTextHeight = txt.Height;
                        }
                        double standardTextHeight = m_standardTextHeight * zoom;

                        var formattedTextDuration = new FormattedText(
                                                AudioPaneViewModel.FormatTimeSpan_Units(new Time(timeInLocalUnits)),
                                                              m_culture,
                                                              FlowDirection.LeftToRight,
                                                              m_typeFace,
                                                              12 * zoom,
                                                              m_timeTextBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
                            );

                        formattedTextDuration.Trimming = TextTrimming.CharacterEllipsis;
                        formattedTextDuration.MaxTextWidth = chunkWidthForText;
                        formattedTextDuration.MaxTextHeight = standardTextHeight + tickHeight;

                        double horizontalMargin = m_horizontalMargin * zoom;

                        m_point3.X = pixelsLeft - hoffset + horizontalMargin + tickHeight;
                        if (m_point3.X < tickHeight)
                        {
                            widthChunk += m_point3.X;
                            m_point3.X = tickHeight;
                        }

                        m_point3.Y = heightAvailable - standardTextHeight - standardTextHeight - tickHeight - tickHeight - tickHeight;

                        double diff = (pixelsRight - hoffset) - widthAvailable;
                        if (diff > 0)
                        {
                            widthChunk -= diff;
                        }

                        double minW = Math.Min(formattedTextDuration.Width + horizontalMargin + horizontalMargin,
                                                widthChunk - tickHeight - tickHeight - 1);
                        if (minW > 0)
                        {
                            m_rectRect.X = m_point3.X - horizontalMargin;
                            m_rectRect.Y = m_point3.Y;
                            m_rectRect.Width = minW;
                            m_rectRect.Height = standardTextHeight;

                            if (drawingGroup != null)
                            {
                                var rectGeo = new RectangleGeometry(m_rectRect);
                                rectGeo.Freeze();
                                var rectGeoDraw = new GeometryDrawing(m_renderBrush, null, rectGeo);
                                rectGeoDraw.Freeze();
                                drawingGroup.Children.Add(rectGeoDraw);

                                var textGeo = formattedTextDuration.BuildGeometry(m_point3);
                                textGeo.Freeze();
                                var textGeoDraw = new GeometryDrawing(m_timeTextBrush, null, textGeo);
                                textGeoDraw.Freeze();
                                drawingGroup.Children.Add(textGeoDraw);
                            }
                            else
                            {
                                drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                                var clipGeo = new RectangleGeometry(m_rectRect);
                                clipGeo.Freeze();
                                drawingContext.PushClip(clipGeo);

                                drawingContext.DrawText(formattedTextDuration, m_point3);

                                drawingContext.Pop(); //PushClip
                            }
                        }

                        if (chunkWidthForText <= 10)
                        {
                            sumData += marker.m_LocalStreamDataLength;
                            pixelsLeft = pixelsRight;
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
                                                                  12 * zoom,
                                                                  m_phraseBrush
#if NET40
, null, TextFormattingMode.Display
#endif //NET40
                                );

                            formattedText.Trimming = TextTrimming.CharacterEllipsis;
                            formattedText.MaxTextWidth = chunkWidthForText;
                            formattedText.MaxTextHeight = standardTextHeight + tickHeight;

                            //FormattedText formattedTextDots = null;

                            m_point3.Y = heightAvailable - standardTextHeight - tickHeight - tickHeight;

                            minW = Math.Min(formattedText.Width + horizontalMargin + horizontalMargin,
                                                widthChunk - tickHeight - tickHeight - 1); //chunkWidthForText
                            if (minW > 0)
                            {
                                m_rectRect.X = m_point3.X - horizontalMargin;
                                m_rectRect.Y = m_point3.Y;
                                m_rectRect.Width = minW;
                                m_rectRect.Height = standardTextHeight;

                                if (drawingGroup != null)
                                {
                                    var rectGeo = new RectangleGeometry(m_rectRect);
                                    rectGeo.Freeze();
                                    var rectGeoDraw = new GeometryDrawing(m_renderBrush, null, rectGeo);
                                    rectGeoDraw.Freeze();
                                    drawingGroup.Children.Add(rectGeoDraw);

                                    var textGeo = formattedText.BuildGeometry(m_point3);
                                    textGeo.Freeze();
                                    var textGeoDraw = new GeometryDrawing(m_phraseBrush, null, textGeo);
                                    textGeoDraw.Freeze();
                                    drawingGroup.Children.Add(textGeoDraw);
                                }
                                else
                                {
                                    //drawingContext.PushOpacity(0.6);

                                    drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                                    Boolean mouseIn = m_MousePosX >= (pixelsLeft - hoffset) &&
                                                      m_MousePosX < (pixelsRight - hoffset);
                                    if (mouseIn)
                                    {
                                        //drawingContext.Pop(); //PushOpacity
                                    }

                                    var clipGeo = new RectangleGeometry(m_rectRect);
                                    clipGeo.Freeze();
                                    drawingContext.PushClip(clipGeo);

                                    drawingContext.DrawText(formattedText, m_point3);

                                    drawingContext.Pop(); //PushClip

                                    if (!mouseIn)
                                    {
                                        //drawingContext.Pop(); //PushOpacity
                                    }

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
                                }
                            }
                        }
                    }

                    sumData += marker.m_LocalStreamDataLength;
                    pixelsLeft = pixelsRight;
                }

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
