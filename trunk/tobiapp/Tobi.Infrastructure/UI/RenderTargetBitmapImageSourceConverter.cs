using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Tobi.Infrastructure.UI
{
    public class RenderTargetBitmapImageSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
            {
                return null;
            }

            var visualBrush = values[0] as VisualBrush;
            if (visualBrush == null)
            {
                var frameworkElement = values[1] as FrameworkElement;
                if (frameworkElement != null)
                {
                    visualBrush = new VisualBrush(frameworkElement);
                }
            }

            if (visualBrush == null)
            {
                return null;
            }

            var width = (Double)values[1];
            if (Double.IsNaN(width))
            {
                return null;
            }
            var height = (Double)values[2];
            if (Double.IsNaN(height))
            {
                return null;
            }

            var rectangle = new Rectangle
            {
                SnapsToDevicePixels = false,
                StrokeThickness = 0,
                Height = height,
                Width = width,
                Fill = visualBrush
            };

            var size = new Size(width, height);
            rectangle.Measure(size);
            rectangle.Arrange(new Rect(0, 0, width, height));
            rectangle.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(rectangle);

            renderBitmap.Freeze();
            return renderBitmap;

            //return renderBitmap.GetAsFrozen();


            /*
            var viewBox = new Viewbox
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                StretchDirection = StretchDirection.Both,
                Stretch = Stretch.Fill
            };
            viewBox.Child = frameworkElement;
             */
            /*
            viewBox.Measure(size);
            viewBox.Arrange(new Rect(0, 0, image.Width, image.Height));
            viewBox.UpdateLayout();
             */
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}