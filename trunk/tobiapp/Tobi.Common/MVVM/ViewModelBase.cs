using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Tobi.Common.MVVM
{
    /// <summary>
    /// Base class for all ViewModels
    /// </summary>
    public class ViewModelBase : PropertyChangedNotifyBase, IDisposable
    {
        protected Dispatcher Dispatcher { get; private set; }

        protected ViewModelBase()
        {
            // Note: ViewModels must be created on the UI thread.
            Dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;
        }

        #region Debug
#if DEBUG
        
        ~ViewModelBase()
        {
            string msg = string.Format("Finalized: ({0})", GetType().Name);
            Debug.WriteLine(msg);
        }

#endif // DEBUG
        #endregion Debug

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