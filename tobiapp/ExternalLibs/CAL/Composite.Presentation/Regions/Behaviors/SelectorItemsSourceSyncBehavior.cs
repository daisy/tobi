//===================================================================================
// Microsoft patterns & practices
// Composite Application Guidance for Windows Presentation Foundation and Silverlight
//===================================================================================
// Copyright (c) Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===================================================================================
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//===================================================================================
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Practices.Composite.Presentation.Properties;
using Microsoft.Practices.Composite.Regions;

namespace Microsoft.Practices.Composite.Presentation.Regions.Behaviors
{
    /// <summary>
    /// Defines the attached behavior that keeps the items of the <see cref="Selector"/> host control in synchronization with the <see cref="IRegion"/>.
    /// </summary>
    public class SelectorItemsSourceSyncBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        /// <summary>
        /// Name that identifies the SelectorItemsSourceSyncBehavior behavior in a collection of RegionsBehaviors. 
        /// </summary>
        public static readonly string BehaviorKey = "SelectorItemsSourceSyncBehavior";
        private bool updating;
        private Selector hostControl;

        /// <summary>
        /// Gets or sets the <see cref="DependencyObject"/> that the <see cref="IRegion"/> is attached to.
        /// </summary>
        /// <value>
        /// A <see cref="DependencyObject"/> that the <see cref="IRegion"/> is attached to.
        /// </value>
        /// <remarks>For this behavior, the host control must always be a <see cref="Selector"/> or an inherited class.</remarks>
        public DependencyObject HostControl
        {
            get
            {
                return this.hostControl;
            }

            set
            {
                this.hostControl = value as Selector;
            }
        }

        /// <summary>
        /// Starts to monitor the <see cref="IRegion"/> to keep it in synch with the items of the <see cref="HostControl"/>.
        /// </summary>
        protected override void OnAttach()
        {
            bool itemsSourceIsSet = this.hostControl.ItemsSource != null;
#if !SILVERLIGHT
            itemsSourceIsSet = itemsSourceIsSet || (BindingOperations.GetBinding(this.hostControl, ItemsControl.ItemsSourceProperty) != null);
#endif
            if (itemsSourceIsSet)
            {
                throw new InvalidOperationException(Resources.ItemsControlHasItemsSourceException);
            }

            this.SynchronizeItems();

            this.hostControl.SelectionChanged += this.HostControlSelectionChanged;
            this.Region.ActiveViews.CollectionChanged += this.ActiveViews_CollectionChanged;
            this.Region.Views.CollectionChanged += this.Views_CollectionChanged;
        }

        private void Views_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.updating)
            {
                return;
            }

            try
            {
                this.updating = true;

                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (object newItem in e.NewItems)
                    {
                        this.hostControl.Items.Add(newItem);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (object oldItem in e.OldItems)
                    {
                        this.hostControl.Items.Remove(oldItem);
                    }
                }
            }
            finally
            {
                this.updating = false;
            }
        }

        private void SynchronizeItems()
        {
            List<object> existingItems = new List<object>();

            // Control must be empty before "Binding" to a region
            foreach (object childItem in this.hostControl.Items)
            {
                existingItems.Add(childItem);
            }

            foreach (object view in this.Region.Views)
            {
                this.hostControl.Items.Add(view);
            }

            foreach (object existingItem in existingItems)
            {
                this.Region.Add(existingItem);
            }
        }


        private void ActiveViews_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.updating)
            {
                return;
            }

            try
            {
                this.updating = true;

                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    this.hostControl.SelectedItem = e.NewItems[0];
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove &&
                         e.OldItems.Contains(this.hostControl.SelectedItem))
                {
                    this.hostControl.SelectedItem = null;
                }
            }
            finally
            {
                this.updating = false;
            }
        }

        private void HostControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.updating)
            {
                return;
            }

            try
            {
                this.updating = true;

                object source;
#if SILVERLIGHT
    // e.OriginalSource == null, that's why we use sender.
                    source = sender;
#else
                source = e.OriginalSource;
#endif

                if (source == sender)
                {
                    foreach (object item in e.RemovedItems)
                    {
                        // check if the view is in both Views and ActiveViews collections (there may be out of sync)
                        if (this.Region.Views.Contains(item) && this.Region.ActiveViews.Contains(item))
                        {
                            this.Region.Deactivate(item);
                        }
                    }

                    foreach (object item in e.AddedItems)
                    {
                        this.Region.Activate(item);
                    }
                }
            }
            finally
            {
                this.updating = false;
            }
        }
    }
}
