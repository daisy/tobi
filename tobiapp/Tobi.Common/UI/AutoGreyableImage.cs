using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tobi.Common.UI
{
    /// <summary>
    /// Heavily modified from the code by: Thomas LEBRUN (http://blogs.developpeur.org/tom)
    /// </summary>
    public class AutoGreyableImage : Image
    {
        static AutoGreyableImage()
        {
            IsEnabledProperty.OverrideMetadata(typeof(AutoGreyableImage),
                   new FrameworkPropertyMetadata(true,
                       new PropertyChangedCallback(OnIsEnabledChanged)));
        }

        private static void OnIsEnabledChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var autoGreyScaleImg = source as AutoGreyableImage;
            if (autoGreyScaleImg == null)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            //var isEnable = Convert.ToBoolean(args.NewValue);
            var isEnable = (bool)args.NewValue;
            autoGreyScaleImg.SetGrey(!isEnable);
        }

        public FormatConvertedBitmap CachedFormatConvertedBitmap;
        public ImageBrush CachedOpacityMask;

        public void SetGrey(bool grey)
        {
            if (grey)
            {
                Source = CachedFormatConvertedBitmap;
                OpacityMask = CachedOpacityMask;
            }
            else
            {
                Source = CachedFormatConvertedBitmap.Source;
                OpacityMask = null;
            }
        }

        public void InitializeFromVectorGraphics(VisualBrush visualBrush, double width, double height) //, Boolean grey)
        {
            RenderTargetBitmap renderTargetImage = CreateFromVectorGraphics(visualBrush, width, height);

            CachedFormatConvertedBitmap = new FormatConvertedBitmap(renderTargetImage, PixelFormats.Gray32Float, null, 0);
            CachedFormatConvertedBitmap.Freeze();

            CachedOpacityMask = new ImageBrush(renderTargetImage);
            CachedOpacityMask.Opacity = 0.4;
            CachedOpacityMask.Freeze();
            
            SetGrey(!IsEnabled);
        }

        public static RenderTargetBitmap CreateFromVectorGraphics(VisualBrush visualBrush, double width, double height) //, Boolean grey)
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

            //            if (grey)
            //            {
            //#if DEBUG
            //                Debugger.Break();
            //#endif

            //                var bmp = new FormatConvertedBitmap(renderBitmap, PixelFormats.Gray32Float, null, 0);
            //                bmp.Freeze();
            //                return bmp;
            //            }

            return renderBitmap; //renderBitmap.GetAsFrozen();
        }
    }
}
