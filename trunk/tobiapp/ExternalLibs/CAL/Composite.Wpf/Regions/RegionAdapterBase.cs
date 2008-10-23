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
using System.Globalization;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Composite.Wpf.Properties;

namespace Microsoft.Practices.Composite.Wpf.Regions
{
    /// <summary>
    /// Base class to facilitate the creation of <see cref="IRegionAdapter"/> implementations.
    /// </summary>
    /// <typeparam name="T">Type of object to adapt.</typeparam>
    public abstract class RegionAdapterBase<T> : IRegionAdapter where T : class
    {
        /// <summary>
        /// Adapts an object and binds it to a new <see cref="IRegion"/>.
        /// </summary>
        /// <param name="regionTarget">The object to adapt.</param>
        /// <returns>The new instance of <see cref="IRegion"/> that the <paramref name="regionTarget"/> is bound to.</returns>
        public IRegion Initialize(T regionTarget)
        {
            IRegion region = CreateRegion();
            AttachBehaviors(region, regionTarget);
            Adapt(region, regionTarget);
            return region;
        }

        /// <summary>
        /// Template method to attach new behaviors.
        /// </summary>
        /// <param name="region">The region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        /// <remarks>By default, this implementation attaches the <see cref="CollectionActiveAwareBehavior"/>.</remarks>
        protected virtual void AttachBehaviors(IRegion region, T regionTarget)
        {
            CollectionActiveAwareBehavior activeAwareBehavior = new CollectionActiveAwareBehavior(region.ActiveViews);
            activeAwareBehavior.Attach();
        }

        /// <summary>
        /// Template method to adapt the object to an <see cref="IRegion"/>.
        /// </summary>
        /// <param name="region">The new region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        protected abstract void Adapt(IRegion region, T regionTarget);

        /// <summary>
        /// Template method to create a new instance of <see cref="IRegion"/>
        /// that will be used to adapt the object.
        /// </summary>
        /// <returns>A new instance of <see cref="IRegion"/>.</returns>
        protected abstract IRegion CreateRegion();

        /// <summary>
        /// Adapts an object and binds it to a new <see cref="IRegion"/>.
        /// </summary>
        /// <param name="regionTarget">The object to adapt.</param>
        /// <returns>The new instance of <see cref="IRegion"/> that the <paramref name="regionTarget"/> is bound to.</returns>
        /// <remarks>This methods performs validation to check that <paramref name="regionTarget"/>
        /// is of type <typeparamref name="T"/>.</remarks>
        /// <exception cref="ArgumentNullException">When <paramref name="regionTarget"/> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">When <paramref name="regionTarget"/> is not of type <typeparamref name="T"/>.</exception>
        IRegion IRegionAdapter.Initialize(object regionTarget)
        {
            if (regionTarget == null)
                throw new ArgumentNullException("regionTarget");

            T castedObject = regionTarget as T;
            if (castedObject == null)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.AdapterInvalidTypeException, typeof(T).Name));

            return Initialize(castedObject);
        }
    }
}
