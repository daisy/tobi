using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    /// Extension Methods for the System.Windows.Media.Visual Class
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Returns the contents of a WPF Visual as a Bitmap in PNG format.
        /// </summary>
        /// <param name="visual">A WPF Visual.</param>
        /// <returns>A GDI+ System.Drawing.Bitmap.</returns>
        public static Bitmap PngBitmap(this Visual visual)
        {
            var height = (int)(double)visual.GetValue(FrameworkElement.ActualWidthProperty);
            var width = (int)(double)visual.GetValue(FrameworkElement.ActualHeightProperty);

            var rtb = new RenderTargetBitmap(
                    height,
                    width,
                    96,
                    96,
                    PixelFormats.Default);

            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            var stream = new MemoryStream();
            encoder.Save(stream);

            var bmp = new Bitmap(stream);
            stream.Close();

            return bmp;
        }

        /// <summary>
        /// Returns the contents of a WPF Visual as a BitmapSource, e.g.
        /// for binding to an Image control.
        /// </summary>
        /// <param name="visual">A WPF Visual.</param>
        /// <returns>A set of pixels.</returns>
        public static BitmapSource BitmapSource(this Visual visual)
        {
            Bitmap bmp = visual.PngBitmap();
            IntPtr hBitmap = bmp.GetHbitmap();
            BitmapSizeOptions sizeOptions = BitmapSizeOptions.FromEmptyOptions();
            return Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                sizeOptions);
        }
    }
}