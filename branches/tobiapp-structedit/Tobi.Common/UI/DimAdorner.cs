using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Common.UI
{
    public class DimAdorner : Adorner
    {
        private Pen m_pen;
        private Rect m_rectRect;

        public DimAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            m_pen = new Pen(Brushes.Black, 1);
            m_pen.Freeze();
            m_rectRect = new Rect(new Point(0, 0), new Size(1, 1));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            m_rectRect.Size = DesiredSize;

            var fe = AdornedElement as FrameworkElement;
            if (fe != null)
            {
                m_rectRect.Width = fe.ActualWidth;
                m_rectRect.Height = fe.ActualHeight;
            }

            drawingContext.PushOpacity(0.4);
            drawingContext.DrawRectangle(Brushes.Black, m_pen, m_rectRect);
            drawingContext.Pop();
        }
    }
}
