using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Common.UI
{
    public class FocusOutlineAdorner : Adorner
    {
        private Pen m_pen;
        private Rect m_rectRect;

        public FocusOutlineAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            m_pen = new Pen(Brushes.Red, 1);
            m_pen.Freeze();
            m_rectRect = new Rect(0, 0, 1, 1);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var fe = AdornedElement as FrameworkElement;
            if (fe == null)
            {
                return;
            }
            m_rectRect.Width = fe.ActualWidth;
            m_rectRect.Height = fe.ActualHeight;

            drawingContext.DrawRectangle(null, m_pen, m_rectRect);
        }
    }
}
