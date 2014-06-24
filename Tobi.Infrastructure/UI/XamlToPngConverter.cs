using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tobi.Infrastructure.UI
{
    public static class XamlToPngConverter
    {
        public static void Convert(Stream xamlInput, double width, double height, Stream pngOutput)
        {
            width = Math.Ceiling(width);
            height = Math.Ceiling(height);

            var pngCreationThread = new Thread(delegate()
          {
              var element = XamlReader.Load(xamlInput) as FrameworkElement;
              if (element == null)
              {
                  return;
              }

              var renderingSize = new Size(width, height);
              element.Measure(renderingSize);

              var renderingRectangle = new Rect(renderingSize);
              element.Arrange(renderingRectangle);

              var renderBitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

              var visual = new DrawingVisual();
              using (DrawingContext context = visual.RenderOpen())
              {
                  var brush = new VisualBrush(element);
                  Rect bounds = VisualTreeHelper.GetDescendantBounds(element);
                  context.DrawRectangle(brush, null, new Rect(new Point(), bounds.Size));
              }
              renderBitmap.Render(visual);

              try
              {
                  var enc = new PngBitmapEncoder();
                  enc.Frames.Add(BitmapFrame.Create(renderBitmap));
                  enc.Save(pngOutput);
              }
              catch (ObjectDisposedException)
              {
                  ; // ignore
              }
          }) {IsBackground = true};

            pngCreationThread.SetApartmentState(ApartmentState.STA);
            pngCreationThread.Start();

            pngCreationThread.Join();// wait
        }
    }
}
