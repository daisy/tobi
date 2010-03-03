using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
#if !DEBUG
        protected
#endif
            set;
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
                if (m_KeyGesture == value
                    || m_KeyGesture != null && value != null
                    && KeyGestureString.AreEqual(m_KeyGesture, value)) return;

                m_KeyGesture = value;

                if (m_KeyBinding != null)
                {
                    KeyBinding.Gesture = m_KeyGesture;
                    FireDataChanged();
                }
            }
        }

        //private string m_KeyGestureText = "";
        public string KeyGestureText
        {
            //set
            //{
            //    m_KeyGestureText = value;
            //    FireDataChanged();
            //}
            get
            {
                //CultureInfo.InvariantCulture
                //return KeyGesture.DisplayString;

                //return (KeyGesture == null ? m_KeyGestureText : KeyGestureString.GetDisplayString(KeyGesture));
                return (KeyGesture == null ? null :
                    KeyGestureStringConverter.Convert(KeyGesture)
                    //KeyGestureString.GetDisplayString(KeyGesture)
                    );
            }
        }

        //1 [NonSerializable]
        private EventHandler m_DataChanged;
        public event EventHandler DataChanged
        {
            //[MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                m_DataChanged = (EventHandler)Delegate.Combine(m_DataChanged, value);
            }

            //[MethodImpl(MethodImplOptions.Synchronized)]
            remove
            {
                m_DataChanged = (EventHandler)Delegate.Remove(m_DataChanged, value);
            }
        }

        private List<WeakReference<EventHandler<EventArgs>>> m_DataChangedChangedHandlers;
        public event EventHandler<EventArgs> DataChanged_WEAK
        {
            //[MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                WeakReferencedEventHandlerHelper.AddWeakReferenceHandler<EventArgs>(ref m_DataChangedChangedHandlers, value, 2);
            }

            //[MethodImpl(MethodImplOptions.Synchronized)]
            remove
            {
                WeakReferencedEventHandlerHelper.RemoveWeakReferenceHandler<EventArgs>(m_DataChangedChangedHandlers, value);
            }
        }

        private void FireDataChanged()
        {
            if (m_DataChanged != null) m_DataChanged(this, EventArgs.Empty);

            //EventHandler d = DataChanged;
            //if (d != null) d(this, EventArgs.Empty);
        }

        private void FireDataChanged_WEAK()
        {
            WeakReferencedEventHandlerHelper.CallWeakReferenceHandlers_WithDispatchCheck<EventArgs>(m_DataChangedChangedHandlers, this, EventArgs.Empty);
        }

        public bool DataChangedHasHandlers
        {
            get
            {
                return m_DataChanged != null;

                //EventHandler d = DataChanged;
                //return d != null;
            }
        }

        public bool DataChangedHasHandlers_WEAK
        {
            get
            {
                return m_DataChangedChangedHandlers.Count > 0;
            }
        }
    }
}
