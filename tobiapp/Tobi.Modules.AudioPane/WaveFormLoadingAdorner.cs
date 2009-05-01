using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Modules.AudioPane
{
    public class WaveFormLoadingAdorner : Adorner
    {
        public WaveFormLoadingAdorner(FrameworkElement adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            ClipToBounds = true;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var formattedText = new FormattedText(
                "Loading...",
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Helvetica"),
                40,
                Brushes.Black
                );

            const double margin = 20;

            double width = ((FrameworkElement)AdornedElement).ActualWidth;
            double height = ((FrameworkElement)AdornedElement).ActualHeight - margin;

            if (width <= margin + margin || height <= margin + margin)
            {
                return;
            }

            double leftOffset = (width - formattedText.Width) / 2;
            double topOffset = (height - formattedText.Height) / 2;


            var renderBrush = new SolidColorBrush(Colors.Black) {Opacity = 0.6};
            var pen = new Pen(Brushes.White, 1);

            drawingContext.DrawRoundedRectangle(renderBrush, pen,
                                                new Rect(new Point(margin, margin),
                                                         new Size(width - margin - margin,
                                                                  height - margin - margin)),
                                                10.0, 10.0);

            Geometry textGeometry = formattedText.BuildGeometry(new Point(leftOffset, topOffset));

            drawingContext.DrawGeometry(Brushes.White,
                                        new Pen(Brushes.Black, 1),
                                        textGeometry);
        }
    }
}