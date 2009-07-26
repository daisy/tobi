using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Unity;

namespace Tobi.Common.MVVM
{
    /// <summary>
    /// Base class for all ViewModels
    /// </summary>
    public class ViewModelBase : PropertyChangedNotifyBase, IDisposable
    {
        protected Dispatcher Dispatcher { get; private set; }
        public IUnityContainer Container { get; private set; }

        protected ViewModelBase(IUnityContainer container)
        {
            Container = container;
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