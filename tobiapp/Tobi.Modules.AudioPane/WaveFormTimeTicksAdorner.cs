using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Tobi.Modules.AudioPane
{
    public class WaveFormTimeTicksAdorner : Adorner
    {
        private AudioPaneView m_AudioPaneView;

        public WaveFormTimeTicksAdorner(FrameworkElement adornedElement, AudioPaneView view)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            ClipToBounds = true;
            m_AudioPaneView = view;
            //MouseMove += OnAdornerMouseMove;
            //MouseLeave += OnAdornerMouseLeave;

            m_penTick = new Pen(Brushes.White, 1);
            m_penTick.Freeze();

            m_renderBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.8 };
            m_renderBrush.Freeze();

            m_point1 = new Point(1, 1);
            m_point2 = new Point(1, 1);

            m_typeFace = new Typeface("Helvetica");

            m_culture = CultureInfo.GetCultureInfo("en-us");

            m_point3 = new Point(1,1);

            m_rectRect = new Rect(1, 1, 1, 1);
        }

        private Typeface m_typeFace;
        private CultureInfo m_culture;
        private double m_MousePosX = -1;
        private Pen m_penTick;
        private SolidColorBrush m_renderBrush;
        private Point m_point1;
        private Point m_point2;
        private Point m_point3;
        private Rect m_rectRect;

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
            if (!m_AudioPaneView.ViewModel.IsAudioLoaded)
            {
                return;
            }

            double heightAvailable = ((FrameworkElement)AdornedElement).ActualHeight;
            double widthAvailable = ((FrameworkElement)AdornedElement).ActualWidth;

            heightAvailable = m_AudioPaneView.WaveFormScroll.ViewportHeight;
            widthAvailable = m_AudioPaneView.WaveFormScroll.ViewportWidth;

            /* var brush = new SolidColorBrush(Colors.Red) { Opacity = 0.0 };
            drawingContext.DrawRectangle(brush, null,
                                             new Rect(new Point(0, 0),
                                                      new Size(widthAvailable,
                                                               heightAvailable))); */

            const double tickHeight = 3;

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
                                                                tickHeight + tickHeight + formattedTextTMP.Height)));
             }*/

            double minorTickInterval_milliseconds = 1000; //1s minor ticks
            double minorTickInterval_pixels =
                m_AudioPaneView.ViewModel.AudioPlayer_ConvertMillisecondsToByte(minorTickInterval_milliseconds)
                    / m_AudioPaneView.BytesPerPixel;

            const double idealTickInterval = 20;

            bool tooSmall = minorTickInterval_pixels < idealTickInterval;
            bool tooLarge = minorTickInterval_pixels > idealTickInterval;

            bool needReAdjusting = false;
            if (tooSmall || tooLarge)
            {
                minorTickInterval_pixels = idealTickInterval;

                minorTickInterval_milliseconds =
                    m_AudioPaneView.ViewModel.AudioPlayer_ConvertByteToMilliseconds(
                                            m_AudioPaneView.BytesPerPixel * minorTickInterval_pixels);
                minorTickInterval_milliseconds = Math.Round(minorTickInterval_milliseconds);

                if (minorTickInterval_milliseconds > 0)
                {
                    if (minorTickInterval_milliseconds % 10 != 0)
                    {
                        minorTickInterval_milliseconds = Math.Round(minorTickInterval_milliseconds / 10) * 10;
                        if (minorTickInterval_milliseconds > 0)
                        {
                            minorTickInterval_pixels =
                                m_AudioPaneView.ViewModel.AudioPlayer_ConvertMillisecondsToByte(
                                        minorTickInterval_milliseconds)
                                                / m_AudioPaneView.BytesPerPixel;
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

            double hoffset = m_AudioPaneView.WaveFormScroll.HorizontalOffset;

            int numberOfEntireTicksHidden = (int)Math.Floor(hoffset / minorTickInterval_pixels);

            double firstTickX = minorTickInterval_pixels -
                                (hoffset - (numberOfEntireTicksHidden * minorTickInterval_pixels));

            drawingContext.PushOpacity(0.6);

            const double horizontalMargin = 2;

            int count = numberOfEntireTicksHidden;
            double currentTickX = firstTickX;
            while (currentTickX <= widthAvailable)
            {
                count++;

                if (count % 5 == 0)
                {
                    m_point1.X = currentTickX;
                    m_point1.Y = 0;

                    m_point2.X = currentTickX;
                    m_point2.Y = tickHeight * 2;

                    drawingContext.DrawLine(m_penTick, m_point1, m_point2);

                    double bytes = m_AudioPaneView.BytesPerPixel * (hoffset + currentTickX);
                    double ms = m_AudioPaneView.ViewModel.AudioPlayer_ConvertByteToMilliseconds(bytes);
                    var formattedText = new FormattedText(
                        FormatTimeSpan(TimeSpan.FromMilliseconds(ms)),
                        m_culture,
                        FlowDirection.LeftToRight,
                        m_typeFace,
                        12,
                        Brushes.White
                        );

                    double posX = currentTickX - formattedText.Width / 2;

                    if (m_MousePosX != -1)
                    {
                        m_rectRect.X = posX - horizontalMargin;
                        m_rectRect.Y = tickHeight*2;
                        m_rectRect.Width = formattedText.Width + horizontalMargin*2;
                        m_rectRect.Height = formattedText.Height;

                        drawingContext.Pop();

                        drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                        drawingContext.PushOpacity(0.6);
                    }
                    m_point3.X = posX;
                    m_point3.Y = tickHeight*2;
                    drawingContext.DrawText(formattedText, m_point3);
                }
                else
                {
                    m_point1.X = currentTickX;
                    m_point1.Y = 0;

                    m_point2.X = currentTickX;
                    m_point2.Y = tickHeight;

                    drawingContext.DrawLine(m_penTick, m_point1, m_point2);
                }

                currentTickX += minorTickInterval_pixels;
            }

            if (m_MousePosX >= 0)
            {
                double bytes = m_AudioPaneView.BytesPerPixel * (hoffset + m_MousePosX);
                double ms = m_AudioPaneView.ViewModel.AudioPlayer_ConvertByteToMilliseconds(bytes);
                var formattedText = new FormattedText(
                    FormatTimeSpan(TimeSpan.FromMilliseconds(ms)),
                    m_culture,
                    FlowDirection.LeftToRight,
                    m_typeFace,
                    12,
                    Brushes.White
                    );

                m_point1.X = m_MousePosX;
                m_point1.Y = 0;

                m_point2.X = m_MousePosX;
                m_point2.Y = heightAvailable;

                drawingContext.DrawLine(m_penTick, m_point1, m_point2);

                double xPos = Math.Max(5, m_MousePosX - formattedText.Width / 2);
                xPos = Math.Min(xPos, widthAvailable - formattedText.Width - 5);
                xPos = Math.Max(5, xPos);

                drawingContext.Pop();

                m_point3.X = xPos;
                m_point3.Y = heightAvailable - formattedText.Height - tickHeight;


                m_rectRect.X = m_point3.X - horizontalMargin;
                m_rectRect.Y = m_point3.Y;
                m_rectRect.Width = formattedText.Width + horizontalMargin*2;
                m_rectRect.Height = formattedText.Height;


                drawingContext.DrawRectangle(m_renderBrush, null, m_rectRect);

                drawingContext.DrawText(formattedText, m_point3);
            }
        }

        private static string FormatTimeSpan(TimeSpan time)
        {
            return
                (time.Hours != 0 ? time.Hours + "h" : "") +
                (time.Minutes != 0 ? time.Minutes + "mn" : "") +
                (time.Seconds != 0 ? time.Seconds + "s" : "") +
                (time.Milliseconds != 0 ? time.Milliseconds + "ms" : "");
        }
    }
}
