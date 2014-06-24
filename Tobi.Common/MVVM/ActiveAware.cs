using System;
using Microsoft.Practices.Composite;

namespace Tobi.Common.MVVM
{
    public class ActiveAware : IActiveAware
    {
        private readonly object LOCK = new object();
        private bool m_IsActive = true;
        private EventHandler m_IsActiveChanged;

        #region IActiveAware members

        /// <summary>
        /// Fired if the <see cref="IsActive"/> property changes.
        /// </summary>
        public event EventHandler IsActiveChanged
        {
            add
            {
                lock (LOCK)
                {
                    m_IsActiveChanged += value;
                }
            }
            remove
            {
                lock (LOCK)
                {
                    m_IsActiveChanged -= value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        /// <value><see langword="true" /> if the object is active; otherwise <see langword="false" />.</value>
        public bool IsActive
        {
            get { return m_IsActive; }
            set
            {
                if (m_IsActive != value)
                {
                    m_IsActive = value;
                    OnIsActiveChanged();
                }
            }
        }

        public void RaiseIsActiveChanged()
        {
            OnIsActiveChanged();
        }

        /// <summary>
        /// This raises the <see cref="IsActiveChanged"/> event.
        /// </summary>
        protected virtual void OnIsActiveChanged()
        {
            EventHandler isActiveChangedHandler;
            lock (LOCK)
            {
                isActiveChangedHandler = m_IsActiveChanged;
            }
            if (isActiveChangedHandler != null)
            {
                isActiveChangedHandler(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
