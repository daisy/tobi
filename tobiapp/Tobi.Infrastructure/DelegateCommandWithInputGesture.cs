using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Wpf.Commands;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Extension to <see cref="DelegateCommand"/> that supports a <see cref="KeyGesture"/> (for example, to display in <see cref="MenuItem"/>)
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class DelegateCommandWithInputGesture<T> : DelegateCommand<T>
    {
        public DelegateCommandWithInputGesture(KeyGesture keyGesture, Action<T> executeMethod)
            : base(executeMethod)
        {
            KeyGesture = keyGesture;
        }

        public DelegateCommandWithInputGesture(KeyGesture keyGesture, Action<T> executeMethod, Func<T, bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        {
            KeyGesture = keyGesture;
        }
        public KeyGesture KeyGesture
        {
            get;
            set;
        }
        public string KeyGestureText
        {
            get
            {
                //return KeyGesture.DisplayString;
                return KeyGesture.GetDisplayStringForCulture(CultureInfo.InvariantCulture);
            }
        }
    }
}
