using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tobi.Common.UI;

namespace Tobi.Common.MVVM.Command
{
    public class RichDelegateCommand : DelegateCommand
    {
        public override string ToString()
        {
            return FullDescription;
        }

        public string FullDescription
        {
            get
            {
                return UserInterfaceStrings.EscapeMnemonic(ShortDescription) + ", " + KeyGestureText + ", " + UserInterfaceStrings.EscapeMnemonic(LongDescription);
            }
        }

        private List<ScalableGreyableImageProvider> m_IconProviders = new List<ScalableGreyableImageProvider>();

        public void SetIconProviderDrawScale(double scale)
        {
            if (!HasIcon)
            {
                return;
            }

            IconProvider.IconDrawScale = scale;

            foreach (var iconProvider in m_IconProviders)
            {
                iconProvider.IconDrawScale = scale;
            }
        }


        public void IconProviderDispose(Image image)
        {
            ScalableGreyableImageProvider iconProviderToDispose = null;

            foreach (var iconProvider in m_IconProviders)
            {
                if (iconProvider.HasIconSmall && iconProvider.IconSmall == image)
                {
                    iconProviderToDispose = iconProvider;
                    break;
                }
                if (iconProvider.HasIconMedium && iconProvider.IconMedium == image)
                {
                    iconProviderToDispose = iconProvider;
                    break;
                }
                if (iconProvider.HasIconLarge && iconProvider.IconLarge == image)
                {
                    iconProviderToDispose = iconProvider;
                    break;
                }
                if (iconProvider.HasIconXLarge && iconProvider.IconXLarge == image)
                {
                    iconProviderToDispose = iconProvider;
                    break;
                }
            }

            if (iconProviderToDispose != null)
            {
                m_IconProviders.Remove(iconProviderToDispose);
            }
        }

        public ScalableGreyableImageProvider IconProviderNotShared
        {
            get
            {
                if (!HasIcon)
                {
                    return null;
                }

                var iconProvider = new ScalableGreyableImageProvider(m_VisualBrush, IconProvider.IconDrawScale);
                m_IconProviders.Add(iconProvider);
                return iconProvider;
            }
        }

        public ScalableGreyableImageProvider IconProvider { get; private set; }

        private readonly VisualBrush m_VisualBrush;
        public bool HasIcon { get { return m_VisualBrush != null; } }

        public RichDelegateCommand(String shortDescription, String longDescription,
                                   KeyGesture keyGesture,
                                   VisualBrush icon,
                                   Action executeMethod,
                                   Func<bool> canExecuteMethod,
                                   ApplicationSettingsBase settingContainer, string settingName)
            : base(executeMethod, canExecuteMethod, false)
        {
            KeyGestureSettingContainer = settingContainer;
            KeyGestureSettingName = settingName;
            KeyGesture = keyGesture ?? RefreshKeyGestureSetting();

            ShortDescription = (String.IsNullOrEmpty(shortDescription) ? "" : shortDescription);
            LongDescription = (String.IsNullOrEmpty(longDescription) ? "" : longDescription);
            
            m_VisualBrush = icon;

            if (HasIcon)
            {
                // TODO: fetching the magnification level from the app resources breaks encapsulation !
                var scale = (Double)Application.Current.Resources["MagnificationLevel"];
                IconProvider = new ScalableGreyableImageProvider(m_VisualBrush, scale);
                //m_IconProviders.Add(IconProvider);
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

        public readonly string KeyGestureSettingName;
        public readonly ApplicationSettingsBase KeyGestureSettingContainer;

        public KeyGesture RefreshKeyGestureSetting()
        {
            if (KeyGestureSettingContainer == null
                || string.IsNullOrEmpty(KeyGestureSettingName)) return null;

            KeyGesture = (KeyGesture)KeyGestureSettingContainer[KeyGestureSettingName];

            return KeyGesture;
        }

        private KeyGesture m_KeyGesture;
        public KeyGesture KeyGesture
        {
            get { return m_KeyGesture; }
            set
            {
                m_KeyGesture = value;
                if (KeyBinding != null)
                {
                    KeyBinding.Gesture = m_KeyGesture;
                }
                FireDataChanged();
            }
        }

        private string m_KeyGestureText = "";
        public string KeyGestureText
        {
            set
            {
                m_KeyGestureText = value;
                FireDataChanged();
            }
            get
            {
                //CultureInfo.InvariantCulture
                //return KeyGesture.DisplayString;
                return (KeyGesture == null ? m_KeyGestureText : KeyGestureSinkBox.GetDisplayString(KeyGesture));
            }
        }

        public event EventHandler DataChanged;
        private void FireDataChanged()
        {
            EventHandler d = DataChanged;
            if (d != null) d(this, EventArgs.Empty);
        }
        public bool DataChangedHasHandlers
        {
            get
            {
                EventHandler d = DataChanged;
                return d != null;
            }
        }
    }
}
