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

            var brush = new SolidColorBrush(Colors.Red) { Opacity = 0.0 };
            drawingContext.DrawRectangle(brush, null,
                                             new Rect(new Point(0, 0),
                                                      new Size(widthAvailable,
                                                               heightAvailable)));
           
            var penTick = new Pen(Brushes.White, 1);

            const double tickHeight = 3;

            if (m_MousePosX != -1)
            {
                var renderBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.6 };

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
            }

            drawingContext.PushOpacity(0.6);


            long minorTickInterval_milliseconds = 1000; //1s minor ticks
            double minorTickInterval_pixels =
                m_AudioPaneView.ViewModel.AudioPlayer_ConvertMillisecondsToByte(minorTickInterval_milliseconds)
                    / m_AudioPaneView.BytesPerPixel;

            double hoffset = m_AudioPaneView.WaveFormScroll.HorizontalOffset;

            int numberOfEntireTicksHidden = (int) Math.Floor(hoffset/minorTickInterval_pixels);

            double firstTickX = minorTickInterval_pixels -
                                    (hoffset - (numberOfEntireTicksHidden * minorTickInterval_pixels));


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

                    drawingContext.DrawText(formattedText, new Point(currentTickX - formattedText.Width / 2,
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

                    var renderBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.6 };
                    var point = new Point(xPos,
                                            heightAvailable - formattedText.Height - tickHeight);

                    drawingContext.DrawRectangle(renderBrush, null,
                                                 new Rect(point,
                                                          new Size(formattedText.Width,
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
