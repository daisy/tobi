using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tobi.Common.UI
{
    /// <summary>
    /// Taken from here: http://joyfulwpf.blogspot.com/2009/05/watermarktextbox-by-inheriting-textbox.html
    /// </summary>
    public class WatermarkTextBox : TextBoxReadOnlyCaretVisible
    {
        static WatermarkTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkTextBox),
                                                     new FrameworkPropertyMetadata(typeof(WatermarkTextBox)));
        }


        //public override void OnApplyTemplate()
        //{
        //    base.OnApplyTemplate();

        //    Style style = (Style)Resources["WatermarkTextBoxStyle"];
        //    Style = style;
        //}



        public string Watermark
        {
            get { return (string)GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Watermark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            "Watermark",
            typeof(string),
            typeof(WatermarkTextBox),
            new UIPropertyMetadata(null));
    }
}