using System;
using System.Collections.Generic;

namespace Tobi.Infrastructure
{
    public class WeakReferenceCollection<T> : ICollection<T>
    {
        private List<WeakReference> items = new List<WeakReference>();

        public WeakReferenceCollection()
        {
        }

        // Cleans out items that have been garbage collected
        internal void CleanupStorage()
        {
            int current = 0;
            while (current < this.items.Count)
            {
                if (this.GetItem(current) == null)
                {
                    this.items.RemoveAt(current);
                }
                else
                {
                    current++;
                }
            }
        }

        internal T GetItem(int index)
        {
            WeakReference weakref = this.items[index];
            if ((weakref != null) && (weakref.IsAlive))
            {
                return (T)weakref.Target;
            }
            return default(T);
        }


        #region ICollection<T> Members

        // Assume for now that Add is not that common and so we can
        // afford the expense of always calling CleanupStorage
        public void Add(T item)
        {
            this.CleanupStorage();
            this.items.Add(new WeakReference(item));
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < this.items.Count; i++)
            {
                if (item.Equals(this.GetItem(i)))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < this.items.Count; i++)
            {
                array[i + arrayIndex] = this.GetItem(i);
            }
        }

        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            for (int i = 0; i < this.items.Count; i++)
            {
                if (item.Equals(this.GetItem(i)))
                {
                    this.items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new WeakReferenceCollectionEnumerator<T>(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new WeakReferenceCollectionEnumerator<T>(this);
        }

        #endregion
    }

    struct WeakReferenceCollectionEnumerator<T> : IEnumerator<T>
    {
        private WeakReferenceCollection<T> contents;
        private int currentIndex;

        public WeakReferenceCollectionEnumerator(WeakReferenceCollection<T> collection)
        {
            this.contents = collection;
            this.currentIndex = -1;
        }
        #region IEnumerator<T> Members

        public T Current
        {
            get { return this.contents.GetItem(this.currentIndex); }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.contents = null;
            this.currentIndex = -1;
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return this.contents.GetItem(this.currentIndex); }
        }

        public bool MoveNext()
        {
            while (currentIndex + 1 < this.contents.Count)
            {
                currentIndex++;
                if (this.contents.GetItem(currentIndex) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            this.currentIndex = -1;
        }

        #endregion
    }
}
