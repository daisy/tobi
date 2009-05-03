using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
                else
                {
                    var drawImage = values[1] as DrawingImage;
                    if (drawImage != null)
                    {
                        var image = new Image {Source = drawImage};
                        visualBrush = new VisualBrush(image);
                    }
                }
            }

            if (visualBrush == null)
            {
                return null;
            }

            var width = (Double)values[1];
            var height = (Double)values[2];

            bool grey = false;
            if (parameter != null && parameter is Boolean)
            {
                grey = (Boolean)parameter;
            }

            return convert(visualBrush, width, height, grey);
        }

        public static BitmapSource convert(VisualBrush visualBrush, double width, double height, Boolean grey)
        {
            if (Double.IsNaN(width))
            {
                return null;
            }
            if (Double.IsNaN(height))
            {
                return null;
            }

            var rectangle = new Rectangle
            {
                SnapsToDevicePixels = true,
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

            if (grey)
            {
                return new FormatConvertedBitmap(renderBitmap,
                                                PixelFormats.Gray32Float, null, 0);
            }

            return renderBitmap; //renderBitmap.GetAsFrozen();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}