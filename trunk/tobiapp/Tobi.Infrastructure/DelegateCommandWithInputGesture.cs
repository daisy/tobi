using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Composite.Presentation.Commands;
using Tobi.Infrastructure.UI;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Extension to <see cref="DelegateCommand<T>"/> that supports a <see cref="KeyGesture"/>
    /// (for example, to display a shortcut in <see cref="MenuItem"/>, next to the label)
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class DelegateCommandWithInputGesture<T> : DelegateCommand<T>
    {
        private void init(string shortDescription, string longDescription, KeyGesture keyGesture, VisualBrush icon)
        {
            ShortDescription = (String.IsNullOrEmpty(shortDescription) ? "" : shortDescription);
            LongDescription = (String.IsNullOrEmpty(longDescription) ? "" : longDescription);
            KeyGesture = keyGesture;
            Icon = icon;
        }

        public DelegateCommandWithInputGesture(String shortDescription, String longDescription,
                                KeyGesture keyGesture,
                                VisualBrush icon,
                                Action<T> executeMethod)
            : base(executeMethod)
        {
            init(shortDescription, longDescription, keyGesture, icon);
        }

        public DelegateCommandWithInputGesture(String shortDescription, String longDescription,
                            KeyGesture keyGesture,
                            VisualBrush icon,
                            Action<T> executeMethod,
                            Func<T, bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        {
            init(shortDescription, longDescription, keyGesture, icon);
        }

        public VisualBrush Icon
        {
            get;
            private set;
        }

        private Image m_IconSmall = null;
        public Image IconSmall
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                if (m_IconSmall == null)
                {
                    RenderTargetBitmap bitmap = RenderTargetBitmapImageSourceConverter.convert(Icon,
                                                                                               Sizes.IconWidth_Small,
                                                                                               Sizes.IconHeight_Small);
                    m_IconSmall = new GreyableImage
                                    {
                                        //Style = (Style)Application.Current.FindResource("ButtonImage"),
                                        Source = bitmap,
                                        Stretch = Stretch.Fill,
                                        SnapsToDevicePixels = false,
                                        Width = Sizes.IconWidth_Small,
                                        Height = Sizes.IconHeight_Small,
                                    };
                }
                return m_IconSmall;
            }
        }

        private Image m_IconMedium = null;
        public Image IconMedium
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                if (m_IconMedium == null)
                {
                    RenderTargetBitmap bitmap = RenderTargetBitmapImageSourceConverter.convert(Icon,
                                                                                               Sizes.IconWidth_Medium,
                                                                                               Sizes.IconHeight_Medium);
                    m_IconMedium = new GreyableImage
                                    {
                                        //Style = (Style)Application.Current.FindResource("ButtonImage"),
                                        Source = bitmap,
                                        Stretch = Stretch.Fill,
                                        SnapsToDevicePixels = false,
                                        Width = Sizes.IconWidth_Medium,
                                        Height = Sizes.IconHeight_Medium,
                                    };
                }
                return m_IconMedium;
            }
        }

        private Image m_IconLarge = null;
        public Image IconLarge
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }
                if (m_IconLarge == null)
                {
                    RenderTargetBitmap bitmap = RenderTargetBitmapImageSourceConverter.convert(Icon,
                                                                                               Sizes.IconWidth_Large,
                                                                                               Sizes.IconHeight_Large);
                    m_IconLarge = new GreyableImage
                                    {
                                        //Style = (Style)Application.Current.FindResource("ButtonImage"),
                                        Source = bitmap,
                                        Stretch = Stretch.Fill,
                                        SnapsToDevicePixels = false,
                                        Width = Sizes.IconWidth_Large,
                                        Height = Sizes.IconHeight_Large,
                                    };
                }
                return m_IconLarge;
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

        public KeyGesture KeyGesture
        {
            get;
            private set;
        }

        public string KeyGestureText
        {
            get
            {
                //return KeyGesture.DisplayString;
                return (KeyGesture == null ? "" : KeyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture)); //CultureInfo.InvariantCulture
            }
        }
    }
}
