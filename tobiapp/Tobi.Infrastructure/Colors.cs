using System;
using System.Windows.Media;

namespace Tobi.Infrastructure
{
    public class Colors
    {
        private System.Drawing.Color m_color;
        private Colors()
        {
            System.Drawing.Color color1 = System.Drawing.Color.FromArgb(0x780F68A6);
            Color color2 = (Color)ColorConverter.ConvertFromString("#FF0F68A6");
            Color color3 = Color.FromArgb(color1.A, color1.R, color1.G, color1.B);
            Console.Out.WriteLine("Color 1: [" + color1 + "]");
            Console.Out.WriteLine("Color 2: [" + color2 + "]");
            Console.Out.WriteLine("Color 3: [" + color3 + "]");
        }

        public const string Color1 = "#FF83BBF4";
        public const string Color2 = "#FF2873BE";

        public static Brush Brush1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F68A6"));
        public static Brush Brush2 = Brushes.White;
        public static Brush Brush3 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFCFCFC"));
    }
}
