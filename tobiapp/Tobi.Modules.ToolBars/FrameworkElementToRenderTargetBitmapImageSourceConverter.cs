using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Tobi.Modules.ToolBars
{
    public class FrameworkElementToRenderTargetBitmapImageSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return null;
            }

            var image = values[0] as Image;
            if (image == null)
            {
                return null;
            }

            var frameworkElement = values[1] as FrameworkElement;
            if (frameworkElement == null)
            {
                return null;
            }

            var visualBrush = new VisualBrush(frameworkElement);
            var rectangle = new Rectangle { SnapsToDevicePixels = false, StrokeThickness = 0, Height = image.Height, Width = image.Width, Fill = visualBrush };

            var size = new Size(image.Width, image.Height);
            rectangle.Measure(size);
            rectangle.Arrange(new Rect(0, 0, image.Width, image.Height));
            rectangle.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap((int)image.Width, (int)image.Height, 96, 96, PixelFormats.Pbgra32);
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