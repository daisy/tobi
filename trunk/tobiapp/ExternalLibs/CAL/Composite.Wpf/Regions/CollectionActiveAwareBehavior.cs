//===============================================================================
// Microsoft patterns & practices
// Composite Application Guidance for Windows Presentation Foundation
//===============================================================================
// Copyright (c) Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//===============================================================================

using System;
using System.Collections.Specialized;

namespace Microsoft.Practices.Composite.Wpf.Regions
{
    /// <summary>
    /// Behavior that monitors an <see cref="INotifyCollectionChanged"/> object and 
    /// changes the value for the <see cref="IActiveAware.IsActive"/> property when
    /// an object that implements <see cref="IActiveAware"/> gets added or removed 
    /// from the collection.
    /// </summary>
    public class CollectionActiveAwareBehavior
    {
        private readonly WeakReference _collection;

        /// <summary>
        /// Initializes a new instance of <see cref="CollectionActiveAwareBehavior"/>.
        /// </summary>
        /// <param name="collection">The collection to monitor.</param>
        /// <remarks>This instance will keep a <see cref="WeakReference"/> to the
        /// <paramref name="collection"/>, so the collection can be garbage collected.</remarks>
        public CollectionActiveAwareBehavior(INotifyCollectionChanged collection)
        {
            _collection = new WeakReference(collection);
        }

        /// <summary>
        /// Attaches the behavior to the <see cref="INotifyCollectionChanged"/>.
        /// </summary>
        public void Attach()
        {
            INotifyCollectionChanged collection = GetCollection();
            if (collection != null)
                collection.CollectionChanged += OnCollectionChanged;
        }

        /// <summary>
        /// Detaches the behavior from the <see cref="INotifyCollectionChanged"/>.
        /// </summary>
        public void Detach()
        {
            INotifyCollectionChanged collection = GetCollection();
            if (collection != null)
                collection.CollectionChanged -= OnCollectionChanged;
        }

        static void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (object item in e.NewItems)
                {
                    IActiveAware activeAware = item as IActiveAware;
                    if (activeAware != null)
                        activeAware.IsActive = true;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (object item in e.OldItems)
                {
                    IActiveAware activeAware = item as IActiveAware;
                    if (activeAware != null)
                        activeAware.IsActive = false;
                }
            }

            // handle other action values (reset, etc)?
        }

        private INotifyCollectionChanged GetCollection()
        {
            return _collection.Target as INotifyCollectionChanged;
        }
    }
}
