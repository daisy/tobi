using System;
using System.Diagnostics;
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
#if DEBUG
                Debugger.Break();
#endif
                return null;
            }
            if (Double.IsNaN(height))
            {
#if DEBUG
                Debugger.Break();
#endif
                return null;
            }

            //visualBrush.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
            //visualBrush.Viewbox = new Rect(0, 0, 1, 1);

            //visualBrush.ViewportUnits = BrushMappingMode.Absolute;
            //visualBrush.Viewport = new Rect(0, 0, width, height);

            //if (visualBrush.Visual is FrameworkElement)
            //{
            //    var frameElement = (FrameworkElement)visualBrush.Visual;
            //    frameElement.Width = width;
            //    frameElement.Height = height;
            //}
            //else
            //{
            //    Debugger.Break();
            //}

            var size = new Size(width, height);

            if (visualBrush.Visual is UIElement)
            {
                var uiElement = (UIElement)visualBrush.Visual;

                uiElement.Measure(size);
                uiElement.Arrange(new Rect(0, 0, width, height));
                //uiElement.UpdateLayout();
                //uiElement.InvalidateVisual();
            }

            var visualBrushHost = new Border // Rectangle
            {
                //StrokeThickness = 0,
                //Fill = Brushes.Red,
                SnapsToDevicePixels = true,
                Height = height,
                Width = width,
                BorderThickness = new Thickness(0),
                BorderBrush = null,
                Background = visualBrush // Fill
            };
            visualBrushHost.Measure(size);
            visualBrushHost.Arrange(new Rect(0, 0, width, height));
            visualBrushHost.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visualBrushHost);
            renderBitmap.Freeze();

            //    Clipboard.SetImage(renderBitmap);

            //PngBitmapEncoder png = new PngBitmapEncoder();
            //png.Frames.Add(BitmapFrame.Create(renderBitmap));
            //using (Stream stm = File.Create(filepath))
            //{
            //    png.Save(stm);
            //}

            if (grey)
            {
#if DEBUG
                Debugger.Break();
#endif

                var bmp = new FormatConvertedBitmap(renderBitmap, PixelFormats.Gray32Float, null, 0);
                bmp.Freeze();
                return bmp;
            }

            return renderBitmap; //renderBitmap.GetAsFrozen();
        }
    }
}