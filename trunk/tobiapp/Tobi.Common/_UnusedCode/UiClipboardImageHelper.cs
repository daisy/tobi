using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tobi.Common._UnusedCode
{
    public class UiClipboardImageHelper
    {
        public static bool PlaceElementOnClipboard(UIElement element)
        {
            bool success = true;
            Bitmap gdiBitmap = null;
            MemoryStream metafileStream = null;

            var wpfBitmap = MakeRenderTargetBitmap(element);
            try
            {
                gdiBitmap = MakeSystemDrawingBitmap(wpfBitmap);
                metafileStream = MakeMetafileStream(gdiBitmap);

                var dataObj = new DataObject();
                dataObj.SetData(DataFormats.Bitmap, gdiBitmap);
                dataObj.SetData(DataFormats.EnhancedMetafile, metafileStream);
                Clipboard.SetDataObject(dataObj, true);
            }
            catch
            { success = false; }
            finally
            {
                if (gdiBitmap != null)
                { gdiBitmap.Dispose(); }
                if (metafileStream != null)
                { metafileStream.Dispose(); }
            }

            return success;
        }

        private static RenderTargetBitmap MakeRenderTargetBitmap(UIElement element)
        {
            element.Measure(new System.Windows.Size(double.PositiveInfinity,
                                                    double.PositiveInfinity));
            element.Arrange(new Rect(new System.Windows.Point(0, 0),
                                     element.DesiredSize));
            var rtb = new RenderTargetBitmap(
                (int)Math.Ceiling(element.RenderSize.Width),
                (int)Math.Ceiling(element.RenderSize.Height),
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);
            return rtb;
        }

        private static Bitmap MakeSystemDrawingBitmap(BitmapSource wpfBitmap)
        {
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(wpfBitmap));
            var stream = new MemoryStream();
            encoder.Save(stream);

            var gdiBitmap = new Bitmap(stream);
            stream.Close();
            stream.Dispose();

            return gdiBitmap;
        }

        private static MemoryStream MakeMetafileStream(Image image)
        {
            Graphics graphics = null;
            Metafile metafile = null;
            var stream = new MemoryStream();
            try
            {
                using (graphics = Graphics.FromImage(image))
                {
                    var hdc = graphics.GetHdc();
                    metafile = new Metafile(stream, hdc);
                    graphics.ReleaseHdc(hdc);
                }
                using (graphics = Graphics.FromImage(metafile))
                { graphics.DrawImage(image, 0, 0); }
            }
            finally
            {
                if (graphics != null)
                { graphics.Dispose(); }
                if (metafile != null)
                { metafile.Dispose(); }
            }
            return stream;
        }
    }
}