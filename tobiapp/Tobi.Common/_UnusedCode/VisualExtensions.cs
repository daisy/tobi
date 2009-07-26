using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Tobi.Common._UnusedCode
{
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x = 0;
        public int y = 0;
    }

    /// <summary>
    /// Extension Methods for the System.Windows.Media.Visual Class
    /// </summary>
    public static class VisualExtensions
    {
        [DllImport("User32", EntryPoint = "ClientToScreen", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int ClientToScreen(IntPtr hWnd, [In, Out] POINT pt);

        public static Point TransformToScreen(this Visual relativeTo, Point point)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(relativeTo) as HwndSource;
            if (hwndSource == null)
            {
                return new Point();
            }
            Visual root = hwndSource.RootVisual;

            // Translate the point from the visual to the root.

            GeneralTransform transformToRoot = relativeTo.TransformToAncestor(root);

            Point pointRoot = transformToRoot.Transform(point);

            // Transform the point from the root to client coordinates.

            Matrix m = Matrix.Identity;

            Transform transform = VisualTreeHelper.GetTransform(root);

            if (transform != null)
            {
                m = Matrix.Multiply(m, transform.Value);
            }

            Vector offset = VisualTreeHelper.GetOffset(root);

            m.Translate(offset.X, offset.Y);

            Point pointClient = m.Transform(pointRoot);

            // Convert from “device-independent pixels” into pixels.

            pointClient = hwndSource.CompositionTarget.TransformToDevice.Transform(pointClient);

            POINT pointClientPixels = new POINT();

            pointClientPixels.x = (0 < pointClient.X) ? (int)(pointClient.X + 0.5) : (int)(pointClient.X - 0.5);

            pointClientPixels.y = (0 < pointClient.Y) ? (int)(pointClient.Y + 0.5) : (int)(pointClient.Y - 0.5);

            // Transform the point into screen coordinates.

            POINT pointScreenPixels = pointClientPixels;

            ClientToScreen(hwndSource.Handle, pointScreenPixels);

            return new Point(pointScreenPixels.x, pointScreenPixels.y);
        }

        /// <summary>
        /// Returns the contents of a WPF Visual as a Bitmap in PNG format.
        /// </summary>
        /// <param name="visual">A WPF Visual.</param>
        /// <returns>A GDI+ System.Drawing.Bitmap.</returns>
        public static Bitmap PngBitmap(this Visual visual)
        {
            var height = (int) (double) visual.GetValue(FrameworkElement.ActualWidthProperty);
            var width = (int) (double) visual.GetValue(FrameworkElement.ActualHeightProperty);


            RenderTargetBitmap rtb;

            PresentationSource ps = PresentationSource.FromVisual(visual);
            if (ps != null)
            {
                Matrix m = ps.CompositionTarget.TransformToDevice;

                // One WPF device independent logical unit is 1/96 of an inch
                double dpiX = 96.0*m.M11;
                double dpiY = 96.0*m.M22;

                rtb = new RenderTargetBitmap(
                    (int)(height * m.M11),
                    (int)(width * m.M11),
                    dpiX,
                    dpiY,
                    PixelFormats.Pbgra32);
            }
            else
            {
                rtb = new RenderTargetBitmap(
                    height,
                    width,
                    96,
                    96,
                    PixelFormats.Pbgra32);
            }
        
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