using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    /// Class used to have an image that is able to be gray when the control is not enabled.
    /// Author: Thomas LEBRUN (http://blogs.developpeur.org/tom)
    /// </summary>
    public class AutoGreyableImage : Image
    {
        public BindingBase SourceBindingColor
        {
            get;
            set;
        }

        public BindingBase SourceBindingGrey
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoGreyableImage"/> class.
        /// </summary>
        static AutoGreyableImage()
        {
            // Override the metadata of the IsEnabled property.
            IsEnabledProperty.OverrideMetadata(typeof(AutoGreyableImage),
                   new FrameworkPropertyMetadata(true,
                       new PropertyChangedCallback(OnAutoGreyScaleImageIsEnabledPropertyChanged)));
        }

        /// <summary>
        /// Called when [auto grey scale image is enabled property changed].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAutoGreyScaleImageIsEnabledPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var autoGreyScaleImg = source as AutoGreyableImage;
            if (autoGreyScaleImg == null)
            {
                return;
            }
            var isEnable = Convert.ToBoolean(args.NewValue);

            if (!isEnable)
            {
                if (autoGreyScaleImg.SourceBindingGrey != null)
                {
                    var expr = autoGreyScaleImg.SetBinding(SourceProperty, autoGreyScaleImg.SourceBindingGrey);
                }
                else
                {
                    BitmapSource bitmapImage = null;
                    
                    if (autoGreyScaleImg.Source is FormatConvertedBitmap)
                    {
                        // Already grey !
                        return;
                    }
                    else if (autoGreyScaleImg.Source is BitmapSource)
                    {
                        bitmapImage = (BitmapSource)autoGreyScaleImg.Source;
                    }
                    else // trying string 
                    {
                        bitmapImage = new BitmapImage(new Uri(autoGreyScaleImg.Source.ToString()));
                    }

                    autoGreyScaleImg.Source = new FormatConvertedBitmap(bitmapImage,
                                                    PixelFormats.Gray32Float, null, 0);
                }

                // Create Opacity Mask for greyscale image as FormatConvertedBitmap does not keep transparency info
                autoGreyScaleImg.OpacityMask = new ImageBrush(((FormatConvertedBitmap)autoGreyScaleImg.Source).Source); //equivalent to new ImageBrush(bitmapImage)
                autoGreyScaleImg.OpacityMask.Opacity = 0.4;
            }
            else
            {
                if (autoGreyScaleImg.SourceBindingColor != null)
                {
                    var expr = autoGreyScaleImg.SetBinding(SourceProperty, autoGreyScaleImg.SourceBindingColor);
                }
                else
                {
                    if (autoGreyScaleImg.Source is FormatConvertedBitmap)
                    {
                        autoGreyScaleImg.Source = ((FormatConvertedBitmap)autoGreyScaleImg.Source).Source;
                    }
                    else if (autoGreyScaleImg.Source is BitmapSource)
                    {
                        // Should be full color already.
                        return;
                    }
                }

                // Reset the Opcity Mask
                autoGreyScaleImg.OpacityMask = null;
            }
        }
    }
}
