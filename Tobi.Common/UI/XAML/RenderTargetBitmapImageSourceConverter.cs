using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tobi.Common.UI.XAML
{

    [ValueConversion(typeof(object), typeof(BitmapSource))]
    public class RenderTargetBitmapImageSourceConverter : ValueConverterMarkupExtensionBase<RenderTargetBitmapImageSourceConverter>
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                        var image = new Image { Source = drawImage };
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

            return AutoGreyableImage.CreateFromVectorGraphics(visualBrush, width, height);

            //bool grey = false;
            //if (parameter != null && parameter is Boolean)
            //{
            //    grey = (Boolean)parameter;
            //}

            //return convert(visualBrush, width, height, grey);
        }
    }
}