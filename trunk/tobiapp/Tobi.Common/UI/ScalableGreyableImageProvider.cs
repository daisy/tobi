using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Tobi.Common.MVVM;
using Tobi.Common.UI.XAML;

namespace Tobi.Common.UI
{
    public class ScalableGreyableImageProvider : PropertyChangedNotifyBase
    {
        public static VisualBrush ConvertIconFormat(DrawingImage drawImage)
        {
            var image = new Image { Source = drawImage };
            return new VisualBrush(image);
        }

        public ScalableGreyableImageProvider(VisualBrush icon)
        {
            IconVisualBrush = icon;
        }

        private double m_IconDrawScale = 1;
        public double IconDrawScale
        {
            get
            {
                return m_IconDrawScale;
            }
            set
            {
                if (m_IconDrawScale != value)
                {
                    m_IconDrawScale = value;
                    
                    //InvalidateIconsCache();

                    if (m_IconSmall != null)
                    {
                        updateSource(m_IconSmall);
                    }
                    if (m_IconMedium != null)
                    {
                        updateSource(m_IconMedium);
                    }
                    if (m_IconLarge != null)
                    {
                        updateSource(m_IconLarge);
                    }
                    if (m_IconXLarge != null)
                    {
                        updateSource(m_IconXLarge);
                    }

                    OnPropertyChanged(() => IconDrawScale);
                }
            }
        }

        public void InvalidateIconsCache()
        {
            m_IconSmall = null;
            m_IconMedium = null;
            m_IconLarge = null;
            m_IconXLarge = null;
        }

        public Thickness IconMargin_Small { get; set; }
        public Thickness IconMargin_Medium { get; set; }
        public Thickness IconMargin_Large { get; set; }
        public Thickness IconMargin_XLarge { get; set; }

        private void updateSource(Image image)
        {
            image.Source = RenderTargetBitmapImageSourceConverter.convert(
                IconVisualBrush,
                image.Width * IconDrawScale, image.Height * IconDrawScale,
                !image.IsEnabled);
        }

        private Image createImage(int size)
        {
            var image = new AutoGreyableImage
            {
                IsEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin =
                    (size == 0
                         ? IconMargin_Small
                         : (size == 1
                                ? IconMargin_Medium
                                : (size == 2 ? IconMargin_Large : IconMargin_XLarge))),
                Stretch = Stretch.UniformToFill,
                SnapsToDevicePixels = true,
                Width =
                    (size == 0
                         ? Sizes.IconWidth_Small
                         : (size == 1
                                ? Sizes.IconWidth_Medium
                                : (size == 2 ? Sizes.IconWidth_Large : Sizes.IconWidth_XLarge))),
                Height =
                    (size == 0
                         ? Sizes.IconHeight_Small
                         : (size == 1
                                ? Sizes.IconHeight_Medium
                                : (size == 2 ? Sizes.IconHeight_Large : Sizes.IconHeight_XLarge)))
            };

            image.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
            image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.Fant);

            updateSource(image);

            return image;


            //string sizeStr = (size == 0
            //                      ? "Small"
            //                      : (size == 1
            //                             ? "Medium"
            //                             : (size == 2 ? "Large" : "XLarge")));
            //var binding = new Binding
            //                  {
            //                      Mode = BindingMode.OneWay,
            //                      Source = this,
            //                      Path = new PropertyPath("Icon" + sizeStr),
            //                      Converter = new RenderTargetBitmapImageSourceConverter(),
            //                      ConverterParameter = false
            //                  };

            //var expr = image.SetBinding(Image.SourceProperty, binding);

            //Binding bind = cloneBinding(binding);
            //bind.ConverterParameter = true;
            //image.SourceBindingColor = binding;
            //image.SourceBindingGrey = bind;

        }

        private void assignMultiBinding(FrameworkElement image, string size)
        {
            var bindingMulti = new MultiBinding
            {
                Converter = new RenderTargetBitmapImageSourceConverter()
            };

            var bindingVisualBrush = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = this,
                Path = new PropertyPath("IconVisualBrush")
            };
            bindingMulti.Bindings.Add(bindingVisualBrush);

            var bindingWidth = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = this,
                Path = new PropertyPath("IconWidth_" + size)
            };
            bindingMulti.Bindings.Add(bindingWidth);

            var bindingHeight = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = this,
                Path = new PropertyPath("IconHeight_" + size)
            };
            bindingMulti.Bindings.Add(bindingHeight);

            bindingMulti.ConverterParameter = false;

            var expr = image.SetBinding(Image.SourceProperty, bindingMulti);

            if (image is AutoGreyableImage)
            {
                MultiBinding bind = cloneBinding(bindingMulti);
                bind.ConverterParameter = true;
                ((AutoGreyableImage) image).SourceBindingColor = bindingMulti;
                ((AutoGreyableImage) image).SourceBindingGrey = bind;
            }
        }

        private MultiBinding cloneBinding(MultiBinding binding)
        {
            var multiBind = new MultiBinding
            {
                Converter = binding.Converter,
                ConverterParameter = binding.ConverterParameter
            };
            foreach (Binding bind in binding.Bindings)
            {
                var newBind = new Binding();
                newBind.Mode = bind.Mode;
                newBind.Source = bind.Source;
                newBind.Path = bind.Path;
                multiBind.Bindings.Add(newBind);
            }
            return multiBind;
        }

        private VisualBrush m_IconVisualBrush = null;
        public VisualBrush IconVisualBrush
        {
            get { return m_IconVisualBrush; }
            private set
            {
                if (m_IconVisualBrush != value)
                {
                    m_IconVisualBrush = value;
                    OnPropertyChanged(() => IconVisualBrush);
                }
            }
        }

        [NotifyDependsOn("IconDrawScale")]
        public double IconHeight_Small
        {
            get { return Sizes.IconHeight_Small * IconDrawScale; }
        }

        [NotifyDependsOn("IconDrawScale")]
        public double IconWidth_Small
        {
            get { return Sizes.IconWidth_Small * IconDrawScale; }
        }

        [NotifyDependsOn("IconWidth_Small")]
        [NotifyDependsOn("IconHeight_Small")]
        [NotifyDependsOn("IconVisualBrush")]
        public Image IconSmall
        {
            get
            {
                if (IconVisualBrush == null)
                {
                    return null;
                }
                if (m_IconSmall == null)
                {
                    m_IconSmall = createImage(0);
                    //assignMultiBinding(iconLarge, "Small");
                }
                return m_IconSmall;
            }
        }
        private Image m_IconSmall = null;

        [NotifyDependsOn("IconDrawScale")]
        public double IconHeight_Medium
        {
            get { return Sizes.IconHeight_Medium * IconDrawScale; }
        }

        [NotifyDependsOn("IconDrawScale")]
        public double IconWidth_Medium
        {
            get { return Sizes.IconWidth_Medium * IconDrawScale; }
        }

        [NotifyDependsOn("IconWidth_Medium")]
        [NotifyDependsOn("IconHeight_Medium")]
        [NotifyDependsOn("IconVisualBrush")]
        public Image IconMedium
        {
            get
            {
                if (IconVisualBrush == null)
                {
                    return null;
                }
                if (m_IconMedium == null)
                {
                    m_IconMedium = createImage(1);
                    //assignMultiBinding(iconLarge, "Medium");
                }
                return m_IconMedium;
            }
        }
        private Image m_IconMedium = null;

        [NotifyDependsOn("IconDrawScale")]
        public double IconHeight_Large
        {
            get { return Sizes.IconHeight_Large * IconDrawScale; }
        }

        [NotifyDependsOn("IconDrawScale")]
        public double IconWidth_Large
        {
            get { return Sizes.IconWidth_Large * IconDrawScale; }
        }

        [NotifyDependsOn("IconWidth_Large")]
        [NotifyDependsOn("IconHeight_Large")]
        [NotifyDependsOn("IconVisualBrush")]
        public Image IconLarge
        {
            get
            {
                if (IconVisualBrush == null)
                {
                    return null;
                }
                if (m_IconLarge == null)
                {
                    m_IconLarge = createImage(2);
                    //assignMultiBinding(iconLarge, "Large");
                }
                return m_IconLarge;
            }
        }
        private Image m_IconLarge = null;

        [NotifyDependsOn("IconDrawScale")]
        public double IconHeight_XLarge
        {
            get { return Sizes.IconHeight_XLarge * IconDrawScale; }
        }

        [NotifyDependsOn("IconDrawScale")]
        public double IconWidth_XLarge
        {
            get { return Sizes.IconWidth_XLarge * IconDrawScale; }
        }

        [NotifyDependsOn("IconWidth_XLarge")]
        [NotifyDependsOn("IconHeight_XLarge")]
        [NotifyDependsOn("IconVisualBrush")]
        public Image IconXLarge
        {
            get
            {
                if (IconVisualBrush == null)
                {
                    return null;
                }
                if (m_IconXLarge == null)
                {
                    m_IconXLarge = createImage(3);
                    //assignMultiBinding(iconLarge, "XLarge");
                }
                return m_IconXLarge;
            }
        }
        private Image m_IconXLarge = null;
    }
}




/*
var bindingWidth2 = new Binding
{
    Mode = BindingMode.OneWay,
    Source = this,
    Path = new PropertyPath("IconWidth_Small")
};
m_IconSmall.SetBinding(FrameworkElement.WidthProperty, bindingWidth2);


var bindingHeight2 = new Binding
{
    Mode = BindingMode.OneWay,
    Source = this,
    Path = new PropertyPath("IconHeight_Small")
};
m_IconSmall.SetBinding(FrameworkElement.HeightProperty, bindingHeight2);
 */

/*
public static readonly DependencyProperty IconProperty =
    DependencyProperty.Register("IconVisualBrush", typeof(VisualBrush), typeof(RichDelegateCommand<T>),
    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
//new PropertyMetadata(new PropertyChangedCallback(OnZoomValueChanged)))

public VisualBrush IconVisualBrush
{
    get { return (VisualBrush)GetValue(IconProperty); }
    set { SetValue(IconProperty, value); }
}*/