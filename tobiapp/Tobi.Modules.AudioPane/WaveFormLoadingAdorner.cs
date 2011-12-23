using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.UI.XAML;
using Colors=System.Windows.Media.Colors;

namespace Tobi.Plugin.AudioPane
{
    public class WaveFormLoadingAdorner : Adorner
    {
        private AudioPaneView m_AudioPaneView;
        private AudioPaneViewModel m_AudioPaneViewModel;

        private Pen m_pen;
        private SolidColorBrush m_renderBrush;
        private Typeface m_typeFace;
        private CultureInfo m_culture;
        private Pen m_textPen;
        private Point m_pointText;
        private Rect m_rectRect;
        private SolidColorBrush m_textBrush;

        public WaveFormLoadingAdorner(FrameworkElement adornedElement, AudioPaneView view, AudioPaneViewModel viewModel)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            ClipToBounds = true;

            m_AudioPaneView = view;
            m_AudioPaneViewModel = viewModel;

            m_typeFace = new Typeface("Helvetica");

            m_culture = CultureInfo.GetCultureInfo("en-us");
            
            m_pointText = new Point(1, 1);
            m_rectRect = new Rect(1, 1, 1, 1);
            
            ResetBrushes();
        }

        private double m_zoom = 1.0;
        private ScaleTransform m_cachedScaleTransform = new ScaleTransform(1, 1);
        private ScaleTransform checkTransform()
        {
            m_zoom = (m_AudioPaneView.m_ShellView != null
                            ? m_AudioPaneView.m_ShellView.MagnificationLevel
                            : (Double)FindResource("MagnificationLevel"));

            long zoomNormalized = (long)Math.Round(m_zoom * 1000);
            if (zoomNormalized == 1000)
            {
                return null;
            }
            else
            {
                long scaleNormalized = (long)Math.Round(m_cachedScaleTransform.ScaleX * 1000);

                double inverseZoom = 1 / m_zoom;
                long inverseZoomNormalized = (long)Math.Round(inverseZoom * 1000);

                if (scaleNormalized != inverseZoomNormalized)
                {
                    m_cachedScaleTransform.ScaleX = inverseZoom;
                    m_cachedScaleTransform.ScaleY = inverseZoom;
                }
                return m_cachedScaleTransform;
            }
        }
        public void ResetBrushes()
        {
            checkTransform();

            m_renderBrush = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Back).Clone();
            m_renderBrush.Opacity = 0.6;
            m_renderBrush.Freeze();

            m_textPen = new Pen(ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_Back), 1);
            m_textPen.Freeze();

            m_textBrush = ColorBrushCache.Get(Settings.Default.AudioWaveForm_Color_TimeText);//m_AudioPaneViewModel.ColorTimeInfoText
            //m_textBrush.Freeze();

            m_pen = new Pen(m_textBrush, 1);
            m_pen.Freeze();
        }

        public bool DisplayRecorderTime
        {
            get; set;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            ScaleTransform trans = checkTransform();

            var txt = (DisplayRecorderTime ? m_AudioPaneViewModel.TimeStringCurrent : Tobi_Plugin_AudioPane_Lang.Loading);
            var formattedText = new FormattedText(
                txt,
                m_culture,
                FlowDirection.LeftToRight,
                m_typeFace,
                30 * (trans != null ? m_zoom : 1),
                m_textBrush //m_textPen.Brush
#if NET40
                , null, TextFormattingMode.Display
#endif //NET40
                );

            double formattedTextWidth = formattedText.Width;
            double formattedTextHeight = formattedText.Height;
            if (trans != null)
            {
                formattedTextWidth *= (1 / m_zoom);
                formattedTextHeight *= (1 / m_zoom);
            }

            const double margin = 20;

            double width = ((FrameworkElement)AdornedElement).ActualWidth;
            double height = ((FrameworkElement)AdornedElement).ActualHeight - margin;

            if (width <= margin + margin || height <= margin + margin)
            {
                return;
            }

            double leftOffset = (width - formattedTextWidth) / 2;
            double topOffset = (height - formattedTextHeight) / 2;

            m_rectRect.X = margin;
            m_rectRect.Y = margin;
            m_rectRect.Width = width - margin - margin;
            m_rectRect.Height = height - margin - margin;

            drawingContext.DrawRoundedRectangle(m_renderBrush, m_pen,
                                                m_rectRect,
                                                10.0, 10.0);
            m_pointText.X = leftOffset;
            m_pointText.Y = topOffset;

            //Geometry textGeometry = formattedText.BuildGeometry(m_pointText);
            //drawingContext.DrawGeometry(m_textBrush, m_textPen, textGeometry);

            if (trans != null)
            {
                m_pointText.X *= m_zoom;
                m_pointText.Y *= m_zoom;
            }

            if (trans != null)
            {
                drawingContext.PushTransform(trans);
            }

            drawingContext.DrawText(formattedText, m_pointText);

            if (trans != null)
            {
                drawingContext.Pop();
            }
        }
    }
}