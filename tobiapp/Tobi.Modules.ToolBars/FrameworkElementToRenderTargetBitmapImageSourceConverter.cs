using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

            var renderBitmap = new RenderTargetBitmap((int)image.Width, (int)image.Height, 96, 96, PixelFormats.Pbgra32);

            var size = new Size(image.Width, image.Height);
            frameworkElement.Measure(size);
            frameworkElement.Arrange(new Rect(0, 0, image.Width, image.Height));
            frameworkElement.UpdateLayout();
            renderBitmap.Render(frameworkElement);

            renderBitmap.Freeze();
            return renderBitmap;

            //return renderBitmap.GetAsFrozen();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}