using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Tobi.Common._UnusedCode
{
    public class ObservableWrappedCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private const string CountString = "Count";
        private const string IndexerName = "Item[]";

        private readonly Monitor m_Monitor;

        protected ICollection<T> InnerCollection { get; private set; }

        public ObservableWrappedCollection(ICollection<T> wrappedCollection)
        {
            m_Monitor = new Monitor();

            if (wrappedCollection == null)
            {
                throw new ArgumentNullException("wrappedCollection");
            }

            InnerCollection = wrappedCollection;
        }

        #region INotifyCollectionChanged

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                using (BlockReentrancy())
                {
                    CollectionChanged(this, e);
                }
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion INotifyCollectionChanged

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            return InnerCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable

        #region ICollection<T>

        public void Add(T item)
        {
            CheckReentrancy();
            InnerCollection.Add(item);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, InnerCollection.Count);
        }

        public void Clear()
        {
            CheckReentrancy();
            InnerCollection.Clear();
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionReset();
        }

        public bool Contains(T item)
        {
            return InnerCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            InnerCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            CheckReentrancy();
            bool result = InnerCollection.Remove(item);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            return result;
        }

        public int Count
        {
            get { return InnerCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return InnerCollection.IsReadOnly; }
        }

        #endregion ICollection<T>

        #region Monitor

        protected IDisposable BlockReentrancy()
        {
            m_Monitor.Enter();
            return m_Monitor;
        }

        protected void CheckReentrancy()
        {
            if ((m_Monitor.IsBusy && (CollectionChanged != null)) && (CollectionChanged.GetInvocationList().Length > 1))
            {
                throw new InvalidOperationException("Collection Reentrancy Not Allowed");
            }
        }

        [Serializable]
        private class Monitor : IDisposable
        {
            private int m_RefCount;

            public bool IsBusy
            {
                get { return m_RefCount > 0; }
            }

            public void Enter()
            {
                m_RefCount++;
            }

            #region IDisposable

            public void Dispose()
            {
                m_RefCount--;
            }

            #endregion
        }

        #endregion
    }
}