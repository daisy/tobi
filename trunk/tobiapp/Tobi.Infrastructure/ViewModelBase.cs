using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Tobi.Infrastructure.Onyx.Reflection;

namespace Tobi.Infrastructure
{
    /// <summary>
    /// Base class for all ViewModels
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        protected Dispatcher Dispatcher { get; private set; }

        protected ViewModelBase()
        {
            ThrowOnInvalidPropertyName = false;
            Dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;
        }

        #region Debug
#if DEBUG
        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            //TypeDescriptor.GetProperties(this)[propertyName] == null
            if (!string.IsNullOrEmpty(propertyName) && GetType().GetProperty(propertyName) == null)
            {
                string msg = String.Format("Invalid property name ! ({0})", propertyName);

                if (ThrowOnInvalidPropertyName)
                {
                throw new ArgumentException(msg, Reflect.GetField(() => propertyName).Name);
                }

                Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        ~ViewModelBase()
        {
            string msg = string.Format("Finalized: ({0})", GetType().Name);
            Debug.WriteLine(msg);
        }

#endif // DEBUG
        #endregion Debug


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
            VerifyPropertyName(propertyName);

            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            OnPropertyChanged(Reflect.GetProperty(expression).Name);
        }

        #endregion INotifyPropertyChanged

        #region IDisposable

        public void Dispose()
        {
#if DEBUG
            string msg = string.Format("Disposing: ({0})", GetType().Name);
            Debug.WriteLine(msg);
#endif
            Disposing();
        }

        protected virtual void Disposing()
        {
        }

        #endregion IDisposable
    }
}
