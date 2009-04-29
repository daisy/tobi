using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Modules.AudioPane
{
    public class WaveFormTimeTicksAdorner : Adorner
    {
        private AudioPaneView m_AudioPaneView;

        public WaveFormTimeTicksAdorner(FrameworkElement adornedElement, AudioPaneView view)
            : base(adornedElement)
        {
            m_AudioPaneView = view;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!m_AudioPaneView.ViewModel.IsAudioLoaded)
            {
                return;
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

            var penTick = new Pen(Brushes.White, 1);
            //double heightAvailable = ((FrameworkElement)AdornedElement).ActualHeight;
            double widthAvailable = ((FrameworkElement)AdornedElement).ActualWidth;

            const double tickHeight = 5;

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
