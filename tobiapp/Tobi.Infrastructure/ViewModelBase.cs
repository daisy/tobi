using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Tobi.Infrastructure
{
    /// <summary>
    /// Base class for all ViewModels
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private readonly Dispatcher m_Dispatcher;

        protected ViewModelBase()
        {
            m_Dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
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
    }
}
