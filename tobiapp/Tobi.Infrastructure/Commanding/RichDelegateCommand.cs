using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Tobi.Infrastructure.UI;

namespace Tobi.Infrastructure.Commanding
{
    ///<summary>
    /// Extension to <see cref="KeyGesture<T>"/> that supports a <see cref="MenuItem"/>
    /// (for example, to display a shortcut in <see cref="DelegateCommand"/>, next to the label),
    /// as well as a scalable icon (with 3 pre-determined sizes), and descriptions (text labels)
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class RichDelegateCommand<T> : DelegateCommand<T>, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            //PropertyChanged.Invoke(this, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        public static VisualBrush ConvertIconFormat(DrawingImage drawImage)
        {
            var image = new Image { Source = drawImage };
            return new VisualBrush(image);
        }

        private void init(string shortDescription, string longDescription, KeyGesture keyGesture, VisualBrush icon)
        {
            ShortDescription = (String.IsNullOrEmpty(shortDescription) ? "" : shortDescription);
            LongDescription = (String.IsNullOrEmpty(longDescription) ? "" : longDescription);
            KeyGesture = keyGesture;
            Icon = icon;
        }

        /*
        public RichDelegateCommand(String shortDescription, String longDescription,
                                KeyGesture keyGesture,
                                VisualBrush icon,
                                Action<T> executeMethod)
            : base(executeMethod)
        {
            init(shortDescription, longDescription, keyGesture, icon);
        }*/

        public RichDelegateCommand(String shortDescription, String longDescription,
                                   KeyGesture keyGesture,
                                   VisualBrush icon,
                                   Action<T> executeMethod,
                                   Func<T, bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        {
            init(shortDescription, longDescription, keyGesture, icon);
        }

        private VisualBrush m_Icon = null;
        public VisualBrush Icon
        {
            get { return m_Icon; }
            private set
            {
                if (m_Icon != value)
                {
                    m_Icon = value;
                    OnPropertyChanged("Icon");
                }
            }
        }

        private double m_IconHeight_Small = Sizes.IconHeight_Small;
        public double IconHeight_Small
        {
            get { return m_IconHeight_Small; }
            private set
            {
                if (m_IconHeight_Small != value)
                {
                    m_IconHeight_Small = value;
                    OnPropertyChanged("IconHeight_Small");
                }
            }
        }
        private double m_IconWidth_Small = Sizes.IconWidth_Small;
        public double IconWidth_Small
        {
            get { return m_IconWidth_Small; }
            private set
            {
                if (m_IconWidth_Small != value)
                {
                    m_IconWidth_Small = value;
                    OnPropertyChanged("IconWidth_Small");
                }
            }
        }
        //private Image m_IconSmall = null;
        public Image IconSmall
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                Image m_IconSmall = null;
                if (m_IconSmall == null)
                {
                    m_IconSmall = createImage(0);
                    assignMultiBinding(m_IconSmall, "Small");
                }
                return m_IconSmall;
            }
        }


        private double m_IconHeight_Medium = Sizes.IconHeight_Medium;
        public double IconHeight_Medium
        {
            get { return m_IconHeight_Medium; }
            private set
            {
                if (m_IconHeight_Medium != value)
                {
                    m_IconHeight_Medium = value;
                    OnPropertyChanged("IconHeight_Medium");
                }
            }
        }
        private double m_IconWidth_Medium = Sizes.IconWidth_Medium;
        public double IconWidth_Medium
        {
            get { return m_IconWidth_Medium; }
            private set
            {
                if (m_IconWidth_Medium != value)
                {
                    m_IconWidth_Medium = value;
                    OnPropertyChanged("IconWidth_Medium");
                }
            }
        }
        //private Image m_IconMedium = null;
        public Image IconMedium
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                Image m_IconMedium = null;
                if (m_IconMedium == null)
                {
                    m_IconMedium = createImage(1);
                    assignMultiBinding(m_IconMedium, "Medium");
                }
                return m_IconMedium;
            }
        }

        private double m_IconHeight_Large = Sizes.IconHeight_Large;
        public double IconHeight_Large
        {
            get { return m_IconHeight_Large; }
            private set
            {
                if (m_IconHeight_Large != value)
                {
                    m_IconHeight_Large = value;
                    OnPropertyChanged("IconHeight_Large");
                }
            }
        }
        private double m_IconWidth_Large = Sizes.IconWidth_Large;
        public double IconWidth_Large
        {
            get { return m_IconWidth_Large; }
            private set
            {
                if (m_IconWidth_Large != value)
                {
                    m_IconWidth_Large = value;
                    OnPropertyChanged("IconWidth_Large");
                }
            }
        }
        //private Image m_IconLarge = null;
        public Image IconLarge
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                Image m_IconLarge = null;
                if (m_IconLarge == null)
                {
                    m_IconLarge = createImage(2);
                    assignMultiBinding(m_IconLarge, "Large");
                }
                return m_IconLarge;
            }
        }

        private double m_IconHeight_XLarge = Sizes.IconHeight_XLarge;
        public double IconHeight_XLarge
        {
            get { return m_IconHeight_XLarge; }
            private set
            {
                if (m_IconHeight_XLarge != value)
                {
                    m_IconHeight_XLarge = value;
                    OnPropertyChanged("IconHeight_XLarge");
                }
            }
        }
        private double m_IconWidth_XLarge = Sizes.IconWidth_XLarge;
        public double IconWidth_XLarge
        {
            get { return m_IconWidth_XLarge; }
            private set
            {
                if (m_IconWidth_XLarge != value)
                {
                    m_IconWidth_XLarge = value;
                    OnPropertyChanged("IconWidth_XLarge");
                }
            }
        }
        //private Image m_IconXLarge = null;
        public Image IconXLarge
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                Image m_IconXLarge = null;
                if (m_IconXLarge == null)
                {
                    m_IconXLarge = createImage(3);
                    assignMultiBinding(m_IconXLarge, "XLarge");
                }
                return m_IconXLarge;
            }
        }
        public String ShortDescription
        {
            get;
            private set;
        }

        public String LongDescription
        {
            get;
            private set;
        }

        private KeyBinding m_KeyBinding = null;
        public KeyBinding KeyBinding
        {
            get
            {
                if (KeyGesture == null)
                {
                    return null;
                }
                if (m_KeyBinding == null)
                {
                    m_KeyBinding = new KeyBinding(this, KeyGesture);
                }
                return m_KeyBinding;
            }
        }

        public KeyGesture KeyGesture
        {
            get;
            private set;
        }

        private string m_KeyGestureText = "";
        public string KeyGestureText
        {
            set
            {
                m_KeyGestureText = value;
            }
            get
            {
                //CultureInfo.InvariantCulture
                //return KeyGesture.DisplayString;
                return (KeyGesture == null ? m_KeyGestureText :
                    KeyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture)
                    .Replace("Oem4", "[")
                    .Replace("Oem6", "]")
                    );
            }
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

                    m_IconHeight_Small = Sizes.IconHeight_Small * m_IconDrawScale;
                    OnPropertyChanged("IconHeight_Small");

                    m_IconWidth_Small = Sizes.IconWidth_Small * m_IconDrawScale;
                    OnPropertyChanged("IconWidth_Small");

                    m_IconHeight_Medium = Sizes.IconHeight_Medium * m_IconDrawScale;
                    OnPropertyChanged("IconHeight_Medium");

                    m_IconWidth_Medium = Sizes.IconWidth_Medium * m_IconDrawScale;
                    OnPropertyChanged("IconWidth_Medium");

                    m_IconHeight_Large = Sizes.IconHeight_Large * m_IconDrawScale;
                    OnPropertyChanged("IconHeight_Large");

                    m_IconWidth_Large = Sizes.IconWidth_Large * m_IconDrawScale;
                    OnPropertyChanged("IconWidth_Large");

                    /*
                    bool canExec = ((ICommand)this).CanExecute(null);
                    
                    if (m_IconSmall != null)
                    {
                        bool dirty = false;
                        var imageSource  = m_IconSmall.Source as RenderTargetBitmap;
                        if (imageSource != null)
                        {
                            dirty = imageSource.Width != IconWidth_Small
                                    ||
                                    imageSource.Height != IconHeight_Small;
                        }
                        if (!canExec || dirty)
                        {
                            m_IconSmall.InvalidateProperty(Image.SourceProperty);
                            //assignMultiBinding(m_IconSmall, "Small");
                        }
                    }
                    if (m_IconMedium != null)
                    {
                        bool dirty = false;
                        var imageSource = m_IconMedium.Source as RenderTargetBitmap;
                        if (imageSource != null)
                        {
                            dirty = imageSource.Width != IconWidth_Medium
                                    ||
                                    imageSource.Height != IconHeight_Medium;
                        }
                        if (!canExec || dirty)
                        {
                            m_IconMedium.InvalidateProperty(Image.SourceProperty);
                            //assignMultiBinding(m_IconMedium, "Medium");
                        }
                    }
                    if (m_IconLarge != null)
                    {
                        bool dirty = false;
                        var imageSource = m_IconLarge.Source as RenderTargetBitmap;
                        if (imageSource != null)
                        {
                            dirty = imageSource.Width != IconWidth_Large
                                    ||
                                    imageSource.Height != IconHeight_Large;
                        }
                        if (!canExec || dirty)
                        {
                            m_IconLarge.InvalidateProperty(Image.SourceProperty);
                            //assignMultiBinding(m_IconLarge, "Large");
                        }
                    }*/
                }
            }
        }

        private void assignMultiBinding(FrameworkElement image, string size)
        {
            var bindingMulti = new MultiBinding { Converter = new RenderTargetBitmapImageSourceConverter() };

            var bindingVisualBrush = new Binding
                                         {
                                             Mode = BindingMode.OneWay,
                                             Source = this,
                                             Path = new PropertyPath("Icon")
                                             //Path = new PropertyPath(IconProperty)
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
                ((AutoGreyableImage)image).SetColorAndGreySourceBindings(bindingMulti, bind);
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
        private Image createImage(int size)
        {
            Image image = new AutoGreyableImage
                              {
                                  Stretch = Stretch.Fill,
                                  SnapsToDevicePixels = true,
                                  Width = (size == 0 ? Sizes.IconWidth_Small : (size == 1 ? Sizes.IconWidth_Medium : (size == 2 ? Sizes.IconWidth_Large : Sizes.IconWidth_XLarge))),
                                  Height = (size == 0 ? Sizes.IconHeight_Small : (size == 1 ? Sizes.IconHeight_Medium : (size == 2 ? Sizes.IconHeight_Large : Sizes.IconHeight_XLarge)))
                              };

            image.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
            image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            return image;
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
            DependencyProperty.Register("Icon", typeof(VisualBrush), typeof(RichDelegateCommand<T>),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        //new PropertyMetadata(new PropertyChangedCallback(OnZoomValueChanged)))

        public VisualBrush Icon
        {
            get { return (VisualBrush)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }*/
    }
}