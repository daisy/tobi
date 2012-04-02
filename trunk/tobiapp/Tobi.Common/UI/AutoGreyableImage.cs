using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using Tobi.Common.UI.XAML;
using urakawa.data;

namespace Tobi.Common.UI
{
    public static class BitmapSourceGC
    {
        private static readonly DependencyProperty GCTrackerProperty =
            DependencyProperty.RegisterAttached("GCTracker", typeof(GCTracker), typeof(BitmapSourceGC),
                                                new PropertyMetadata(null));

        public static void Manage(BitmapSource bitmap)
        {
            if (bitmap.IsFrozen) return;

            int stride = bitmap.PixelWidth * ((bitmap.Format.BitsPerPixel + 7) / 8);
            int height = bitmap.PixelHeight;

            bitmap.SetValue(GCTrackerProperty, new GCTracker((long)stride * height));
        }

        private sealed class GCTracker
        {
            private readonly long m_bytes;
            public GCTracker(long bytes)
            {
                m_bytes = bytes;
                if (m_bytes > 0)
                {
                    GC.AddMemoryPressure(m_bytes);
                }
                else
                {
                    GC.SuppressFinalize(this);
                }
            }
            ~GCTracker()
            {
                GC.RemoveMemoryPressure(m_bytes);
            }
        }
    }

    [ValueConversion(typeof(string), typeof(ImageSource))]
    public class PathToImageSourceConverter : ValueConverterMarkupExtensionBase<PathToImageSourceConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Object) && targetType != typeof(ImageSource))
                throw new InvalidOperationException("The target must be Object or ImageSource !");

            if (value == null) return null;

            var path = value as string;
            ImageSource imageSource = AutoGreyableImage.GetSVGOrBitmapImageSource(path);
            return imageSource;
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

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

            BitmapSourceGC.Manage(renderBitmap);
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

        public static ImageSource GetSVGOrBitmapImageSource(string filepath)
        {
            string localpath = FileDataProvider.EnsureLocalFilePathDownloadTempDirectory(filepath);

            if (localpath == null && !File.Exists(localpath))
            {
                return null;
            }

            ImageSource imageSource = null;

            string ext = Path.GetExtension(localpath);
            if (string.Equals(ext, DataProviderFactory.IMAGE_SVG_EXTENSION, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, DataProviderFactory.IMAGE_SVGZ_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    SvgImageExtension svgImageExt = new SvgImageExtension(localpath);
                    svgImageExt.TextAsGeometry = true;
                    svgImageExt.OptimizePath = true;
                    svgImageExt.IncludeRuntime = true;

                    imageSource = (DrawingImage)svgImageExt.ProvideValue(null);
                }
                catch (Exception e1)
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    //new BitmapImage(new Uri(path, UriKind.Absolute));
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(localpath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.None;
                    bitmap.EndInit();

                    imageSource = bitmap;
                }
                catch (Exception e2)
                {
                    return null;
                }



                /*
                 * stream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                 * 
                 * 
                if (fullImagePath.EndsWith(DataProviderFactory.IMAGE_JPG_EXTENSION) || fullImagePath.EndsWith(DataProviderFactory.IMAGE_JPEG_EXTENSION))
                {
                    BitmapDecoder dec = new JpegBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    image.Source = dec.Frames[0];
                }
                else if (fullImagePath.EndsWith(DataProviderFactory.IMAGE_PNG_EXTENSION))
                {
                    BitmapDecoder dec = new PngBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    image.Source = dec.Frames[0];
                }
                else if (fullImagePath.EndsWith(DataProviderFactory.IMAGE_BMP_EXTENSION))
                {
                    BitmapDecoder dec = new BmpBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    image.Source = dec.Frames[0];
                }
                else if (fullImagePath.EndsWith(DataProviderFactory.IMAGE_GIF_EXTENSION))
                {
                    BitmapDecoder dec = new GifBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    image.Source = dec.Frames[0];
                }
                 */
            }

            return imageSource;
        }
    }
}
