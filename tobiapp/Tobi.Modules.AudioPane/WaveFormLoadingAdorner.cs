using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Modules.AudioPane
{
    public class WaveFormLoadingAdorner : Adorner
    {
        public WaveFormLoadingAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            FormattedText formattedText = new FormattedText(
                "Loading...",
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Helvetica"),
                40,
                Brushes.Black
                );

            double margin = 20;

            double width = ((ScrollViewer)AdornedElement).ActualWidth;
            double height = ((ScrollViewer)AdornedElement).ActualHeight - 20;

            if (width <= margin + margin || height <= margin + margin)
            {
                return;
            }

            double leftOffset = (width - formattedText.Width) / 2;
            double topOffset = (height - formattedText.Height) / 2;


            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Black);
            renderBrush.Opacity = 0.6;
            Pen pen = new Pen(Brushes.White, 1);

            drawingContext.DrawRoundedRectangle(renderBrush, pen,
                                                new Rect(new Point(margin, margin),
                                                         new Size(width - margin - margin,
                                                                  height - margin - margin)),
                                                10.0, 10.0);

            Geometry textGeometry = formattedText.BuildGeometry(
                new Point(leftOffset, topOffset));
            drawingContext.DrawGeometry(Brushes.White,
                                        new Pen(Brushes.Black, 1),
                                        textGeometry);
        }
    }
}