using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common;
using Colors=System.Windows.Media.Colors;

namespace Tobi.Plugin.AudioPane
{
    public class WaveFormLoadingAdorner : Adorner
    {
        private AudioPaneViewModel m_AudioPaneViewModel;
        private Pen m_pen;
        private SolidColorBrush m_renderBrush;
        private Typeface m_typeFace;
        private CultureInfo m_culture;
        private Pen m_textPen;
        private Point m_pointText;
        private Rect m_rectRect;
        private SolidColorBrush m_textBrush;

        public WaveFormLoadingAdorner(FrameworkElement adornedElement, AudioPaneViewModel view)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            ClipToBounds = true;
            m_AudioPaneViewModel = view;

            m_renderBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.6 };
            m_renderBrush.Freeze();

            m_typeFace = new Typeface("Helvetica");

            m_culture = CultureInfo.GetCultureInfo("en-us");
            
            m_textPen = new Pen(Brushes.Black, 1);
            m_textPen.Freeze();

            m_pointText = new Point(1, 1);
            m_rectRect = new Rect(1, 1, 1, 1);
            
            ResetBrushes();
        }

        public void ResetBrushes()
        {
            m_textBrush = new SolidColorBrush(m_AudioPaneViewModel.ColorTimeInfoText);
            m_textBrush.Freeze();

            m_pen = new Pen(m_textBrush, 1);
            m_pen.Freeze();
        }

        public bool DisplayRecorderTime
        {
            get; set;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var formattedText = new FormattedText(
                (DisplayRecorderTime ? m_AudioPaneViewModel.TimeStringCurrent : Tobi_Plugin_AudioPane_Lang.Loading),
                m_culture,
                FlowDirection.LeftToRight,
                m_typeFace,
                30,
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

            m_rectRect.X = margin;
            m_rectRect.Y = margin;
            m_rectRect.Width = width - margin - margin;
            m_rectRect.Height = height - margin - margin;

            drawingContext.DrawRoundedRectangle(m_renderBrush, m_pen,
                                                m_rectRect,
                                                10.0, 10.0);
            m_pointText.X = leftOffset;
            m_pointText.Y = topOffset;
            Geometry textGeometry = formattedText.BuildGeometry(m_pointText);

            drawingContext.DrawGeometry(m_textBrush,
                                        m_textPen,
                                        textGeometry);
        }
    }
}