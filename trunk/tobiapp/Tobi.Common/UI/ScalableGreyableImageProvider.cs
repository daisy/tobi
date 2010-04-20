using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tobi.Common.MVVM;

namespace Tobi.Common.UI
{
    public class ScalableGreyableImageProvider : PropertyChangedNotifyBase
    {
        public static VisualBrush ConvertIconFormat(DrawingImage drawImage)
        {
            var image = new Image { Source = drawImage };
            return new VisualBrush(image);
        }

        public ScalableGreyableImageProvider(VisualBrush icon, double iconDrawScale)
        {
            IconVisualBrush = icon;
            IconDrawScale = iconDrawScale;
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
#if NET40
                        m_IconSmall.CacheMode = null;
#endif
                        updateSource(m_IconSmall);
                    }
                    if (m_IconMedium != null)
                    {
#if NET40
                        m_IconMedium.CacheMode = null;
#endif
                        updateSource(m_IconMedium);
                    }
                    if (m_IconLarge != null)
                    {
#if NET40
                        m_IconLarge.CacheMode = null;
#endif
                        updateSource(m_IconLarge);
                    }
                    if (m_IconXLarge != null)
                    {
#if NET40
                        m_IconXLarge.CacheMode = null;
#endif
                        updateSource(m_IconXLarge);
                    }

                    RaisePropertyChanged(() => IconDrawScale);
                }
            }
        }

        public Thickness IconMargin_Small { get; set; }
        public Thickness IconMargin_Medium { get; set; }
        public Thickness IconMargin_Large { get; set; }
        public Thickness IconMargin_XLarge { get; set; }

        private void updateSource(AutoGreyableImage image)
        {
            image.InitializeFromVectorGraphics(
                IconVisualBrush,
                image.Width * IconDrawScale, image.Height * IconDrawScale);

#if NET40
            if (image.CacheMode == null)
            {
                image.CacheMode = new BitmapCache
                {
                    RenderAtScale = IconDrawScale,
                    EnableClearType = true,
                    SnapsToDevicePixels = true
                };

                //var bitmapCacheBrush = new BitmapCacheBrush
                //{
                //    AutoLayoutContent = false,
                //    Target = image,
                //    BitmapCache = new BitmapCache
                //    {
                //        RenderAtScale = 0.3,
                //        EnableClearType = false,
                //        SnapsToDevicePixels = false
                //    }
                //};
                //var imageTooltip = new Canvas
                //{
                //    Width = image.Width * bitmapCacheBrush.BitmapCache.RenderAtScale,
                //    Height = image.Height * bitmapCacheBrush.BitmapCache.RenderAtScale,
                //    Background = bitmapCacheBrush
                //};
                //host.ToolTip = imageTooltip;
            }
#endif
        }

        private AutoGreyableImage createImage(int size)
        {
            var image = new AutoGreyableImage
            {
                IsEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,

                Margin =
                    (size == 0
                         ? IconMargin_Small
                         : (size == 1
                                ? IconMargin_Medium
                                : (size == 2 ? IconMargin_Large : IconMargin_XLarge))),

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

            updateSource(image);

            image.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
            image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.Fant);
            image.SetValue(RenderOptions.CachingHintProperty, CachingHint.Unspecified);

            //image.SetValue(RenderOptions.CacheInvalidationThresholdMinimumProperty, 1);
            //image.SetValue(RenderOptions.CacheInvalidationThresholdMaximumProperty, 1);


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

        private VisualBrush m_IconVisualBrush;
        public VisualBrush IconVisualBrush
        {
            get { return m_IconVisualBrush; }
            private set
            {
                if (m_IconVisualBrush != value)
                {
                    m_IconVisualBrush = value;

                    m_IconVisualBrush.Opacity = 1.0;
                    m_IconVisualBrush.Stretch = Stretch.Uniform;
                    m_IconVisualBrush.TileMode = TileMode.None;
                    m_IconVisualBrush.AutoLayoutContent = false;
                    m_IconVisualBrush.AlignmentX = AlignmentX.Center;
                    m_IconVisualBrush.AlignmentY = AlignmentY.Center;

                    m_IconVisualBrush.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    m_IconVisualBrush.Viewbox = new Rect(0, 0, 1, 1);

                    m_IconVisualBrush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
                    m_IconVisualBrush.Viewport = new Rect(0, 0, 1, 1);

                    RaisePropertyChanged(() => IconVisualBrush);
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

        public bool HasIconSmall { get { return m_IconSmall != null; } }
        public bool HasIconMedium { get { return m_IconMedium != null; } }
        public bool HasIconLarge { get { return m_IconLarge != null; } }
        public bool HasIconXLarge { get { return m_IconXLarge != null; } }


        [NotifyDependsOn("IconWidth_Small")]
        [NotifyDependsOn("IconHeight_Small")]
        [NotifyDependsOn("IconVisualBrush")]
        public Image IconSmall
        {
            get
            {
                if (IconVisualBrush == null)
                {
                    //Debugger.Break();
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
        private AutoGreyableImage m_IconSmall;

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
                    //Debugger.Break();
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
        private AutoGreyableImage m_IconMedium;

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
                    //Debugger.Break();
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
        private AutoGreyableImage m_IconLarge;

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
                    //Debugger.Break();
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
        private AutoGreyableImage m_IconXLarge;



        //public void InvalidateIconsCache()
        //{
        //    m_IconSmall = null;
        //    m_IconMedium = null;
        //    m_IconLarge = null;
        //    m_IconXLarge = null;
        //}
    }
}



/*
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
}*/


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