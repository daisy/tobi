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
        }

        private double m_MousePosX = -1;

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

            var penTick = new Pen(Brushes.White, 1);

            var renderBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.8 };

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
                    drawingContext.DrawLine(penTick, new Point(currentTickX, 0),
                                            new Point(currentTickX, tickHeight * 2));

                    double bytes = m_AudioPaneView.BytesPerPixel * (hoffset + currentTickX);
                    double ms = m_AudioPaneView.ViewModel.AudioPlayer_ConvertByteToMilliseconds(bytes);
                    var formattedText = new FormattedText(
                        formatTimeSpan(TimeSpan.FromMilliseconds(ms)),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Helvetica"),
                        12,
                        Brushes.White
                        );

                    double posX = currentTickX - formattedText.Width / 2;

                    if (m_MousePosX != -1)
                    {
                        var point = new Point(posX - horizontalMargin,
                                              tickHeight * 2);

                        drawingContext.Pop();

                        drawingContext.DrawRectangle(renderBrush, null,
                                                     new Rect(point,
                                                              new Size(formattedText.Width + horizontalMargin*2,
                                                                       formattedText.Height)));

                        drawingContext.PushOpacity(0.6);
                    }
                    drawingContext.DrawText(formattedText, new Point(posX,
                                                                tickHeight * 2));
                }
                else
                {
                    drawingContext.DrawLine(penTick, new Point(currentTickX, 0),
                                                    new Point(currentTickX, tickHeight));
                }

                currentTickX += minorTickInterval_pixels;
            }

            if (m_MousePosX != -1)
            {
                double bytes = m_AudioPaneView.BytesPerPixel * (hoffset + m_MousePosX);
                double ms = m_AudioPaneView.ViewModel.AudioPlayer_ConvertByteToMilliseconds(bytes);
                var formattedText = new FormattedText(
                    formatTimeSpan(TimeSpan.FromMilliseconds(ms)),
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Helvetica"),
                    12,
                    Brushes.White
                    );

                drawingContext.DrawLine(penTick, new Point(m_MousePosX, 0),
                                                new Point(m_MousePosX, heightAvailable));

                double xPos = Math.Max(5, m_MousePosX - formattedText.Width / 2);
                xPos = Math.Min(xPos, widthAvailable - formattedText.Width - 5);
                xPos = Math.Max(5, xPos);

                drawingContext.Pop();

                var point = new Point(xPos,
                                        heightAvailable - formattedText.Height - tickHeight);

                drawingContext.DrawRectangle(renderBrush, null,
                                             new Rect(new Point(point.X - horizontalMargin, point.Y),
                                                      new Size(formattedText.Width + horizontalMargin*2,
                                                               formattedText.Height)));

                drawingContext.DrawText(formattedText, point);
            }
        }

        private static string formatTimeSpan(TimeSpan time)
        {
            return
                (time.Hours != 0 ? time.Hours + "h" : "") +
                (time.Minutes != 0 ? time.Minutes + "mn" : "") +
                (time.Seconds != 0 ? time.Seconds + "s" : "") +
                (time.Milliseconds != 0 ? time.Milliseconds + "ms" : "");
        }

    }
}
