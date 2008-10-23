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
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Composite.Wpf.Properties;

namespace Microsoft.Practices.Composite.Wpf.Regions
{
    /// <summary>
    /// Adapter that creates a new <see cref="AllActiveRegion"/> and binds all
    /// the views to the adapted <see cref="ItemsControl"/>. 
    /// </summary>
    public class ItemsControlRegionAdapter : RegionAdapterBase<ItemsControl>
    {
        /// <summary>
        /// Adapts an <see cref="ItemsControl"/> to an <see cref="IRegion"/>.
        /// </summary>
        /// <param name="region">The new region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        protected override void Adapt(IRegion region, ItemsControl regionTarget)
        {
            if (regionTarget.ItemsSource != null || (BindingOperations.GetBinding(regionTarget, ItemsControl.ItemsSourceProperty) != null))
                throw new InvalidOperationException(Resources.ItemsControlHasItemsSourceException);

            //If control has child items, move them to the region and then bind control to region. Can't set ItemsSource if child items exist.
            if (regionTarget.Items.Count > 0)
            {
                foreach (object childItem in regionTarget.Items)
                {
                    region.Add(childItem);
                }
                //Control must be empty before setting ItemsSource
                regionTarget.Items.Clear();
            }
            regionTarget.ItemsSource = region.Views;
        }

        /// <summary>
        /// Creates a new instance of <see cref="AllActiveRegion"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="AllActiveRegion"/>.</returns>
        protected override IRegion CreateRegion()
        {
            return new AllActiveRegion();
        }
    }
}