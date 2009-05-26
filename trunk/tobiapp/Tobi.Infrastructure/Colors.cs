using System;
using System.Windows.Media;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Application-wide predefined colors
    ///</summary>
    public static class Colors
    {
        // System colors: http://www.informit.com/articles/article.aspx?p=25357&seqNum=4

        private static System.Drawing.Color _color;
        static Colors()
        {
            System.Drawing.Color color1 = System.Drawing.Color.FromArgb(0x780F68A6);
            Color color2 = (Color)ColorConverter.ConvertFromString("#FF0F68A6");
            Color color3 = Color.FromArgb(color1.A, color1.R, color1.G, color1.B);
            Console.Out.WriteLine("Color 1: [" + color1 + "]");
            Console.Out.WriteLine("Color 2: [" + color2 + "]");
            Console.Out.WriteLine("Color 3: [" + color3 + "]");
        }

        public readonly static Color Color1 = (Color)ColorConverter.ConvertFromString("#FF83BBF4");
        public readonly static Color Color2 = (Color)ColorConverter.ConvertFromString("#FF2873BE");

        public readonly static Brush Brush1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F68A6"));
        public readonly static Brush Brush2 = Brushes.White;
        public readonly static Brush Brush3 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFCFCFC"));
    }
}
