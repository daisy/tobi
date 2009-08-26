using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tobi.Common.UI;

namespace Tobi.Common.MVVM.Command
{
    ///<summary>
    /// Extension to <see cref="KeyGesture<T>"/> that supports a <see cref="MenuItem"/>
    /// (for example, to display a shortcut in <see cref="DelegateCommand"/>, next to the label),
    /// as well as a scalable icon (with 3 pre-determined sizes), and descriptions (text labels)
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class RichDelegateCommand<T> : DelegateCommand<T>
    {
        public string FullDescription
        {
            get
            {
                return UserInterfaceStrings.EscapeMnemonic(ShortDescription) + ", " + KeyGestureText + ", " + UserInterfaceStrings.EscapeMnemonic(LongDescription);
            }
        }

        public ScalableGreyableImageProvider IconProvider { get; private set; }

        public RichDelegateCommand(String shortDescription, String longDescription,
                                   KeyGesture keyGesture,
                                   VisualBrush icon,
                                   Action<T> executeMethod,
                                   Predicate<T> canExecuteMethod)
            : base(executeMethod, canExecuteMethod, false)
        {
            ShortDescription = (String.IsNullOrEmpty(shortDescription) ? "" : shortDescription);
            LongDescription = (String.IsNullOrEmpty(longDescription) ? "" : longDescription);
            KeyGesture = keyGesture;
            IconProvider = new ScalableGreyableImageProvider(icon);
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
    }
}
