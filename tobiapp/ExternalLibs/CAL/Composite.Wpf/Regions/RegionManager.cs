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
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Composite.Wpf.Properties;

namespace Microsoft.Practices.Composite.Wpf.Regions
{
    /// <summary>
    /// This class is responsible for maintaining a collection of regions and attaching regions to controls. 
    /// </summary>
    /// <remarks>
    /// This class supplies the attached properties that can be used for simple region creation from XAML.
    /// It finds an adapter mapped to a WPF control and associates a new region to that control by calling
    /// <see cref="AttachNewRegion"/> automatically.
    /// </remarks>
    public class RegionManager : IRegionManager
    {
        #region Static properties (for XAML support)

        /// <summary>
        /// Identifies the RegionName attached property.
        /// </summary>
        /// <remarks>
        /// When a control has both the <see cref="RegionNameProperty"/> and
        /// <see cref="RegionManagerProperty"/> attached properties set to
        /// a value different than <see langword="null" /> and there is a
        /// <see cref="IRegionAdapter"/> mapping registered for the control, it
        /// will create and adapt a new region for that control, and register it
        /// in the <see cref="IRegionManager"/> with the specified region name.
        /// </remarks>
        public static readonly DependencyProperty RegionNameProperty = DependencyProperty.RegisterAttached(
            "RegionName",
            typeof(string),
            typeof(RegionManager),
            new PropertyMetadata(OnSetRegionNameCallback));

        /// <summary>
        /// Sets the <see cref="RegionNameProperty"/> attached property.
        /// </summary>
        /// <param name="regionTarget">The object to adapt. This is typically a container (i.e a control).</param>
        /// <param name="regionName">The name of the region to register.</param>
        public static void SetRegionName(DependencyObject regionTarget, string regionName)
        {
            regionTarget.SetValue(RegionNameProperty, regionName);
        }

        /// <summary>
        /// Gets the value for the <see cref="RegionNameProperty"/> attached property.
        /// </summary>
        /// <param name="regionTarget">The object to adapt. This is typically a container (i.e a control).</param>
        /// <returns>The name of the region that should be created when 
        /// <see cref="RegionManagerProperty"/> is also set in this element.</returns>
        public static string GetRegionName(DependencyObject regionTarget)
        {
            return regionTarget.GetValue(RegionNameProperty) as string;
        }

        private static void OnSetRegionNameCallback(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            if (element != null)
            {
                IRegionManager regionManager = element.GetValue(RegionManagerProperty) as IRegionManager;
                if (regionManager != null)
                {
                    string oldRegionName = args.OldValue as string;
                    if (oldRegionName != null)
                    {
                        regionManager.Regions.Remove(oldRegionName);
                    }

                    string newRegionName = args.NewValue as string;
                    if (newRegionName != null)
                    {
                        regionManager.AttachNewRegion(element, newRegionName);
                    }
                }
            }
        }

        /// <summary>
        /// Identifies the RegionManager attached property.
        /// </summary>
        /// <remarks>
        /// When a control has both the <see cref="RegionNameProperty"/> and
        /// <see cref="RegionManagerProperty"/> attached properties set to
        /// a value different than <see langword="null" /> and there is a
        /// <see cref="IRegionAdapter"/> mapping registered for the control, it
        /// will create and adapt a new region for that control, and register it
        /// in the <see cref="IRegionManager"/> with the specified region name.
        /// </remarks>
        public static readonly DependencyProperty RegionManagerProperty =
            DependencyProperty.RegisterAttached("RegionManager", typeof(IRegionManager), typeof(RegionManager),
                                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSetRegionManagerCallback));

        /// <summary>
        /// Gets the value of the <see cref="RegionNameProperty"/> attached property.
        /// </summary>
        /// <param name="target">The target element.</param>
        /// <returns>The <see cref="IRegionManager"/> attached to the <paramref name="target"/> element.</returns>
        public static IRegionManager GetRegionManager(DependencyObject target)
        {
            return (IRegionManager)target.GetValue(RegionManagerProperty);
        }

        /// <summary>
        /// Sets the <see cref="RegionManagerProperty"/> attached property.
        /// </summary>
        /// <param name="target">The target element.</param>
        /// <param name="value">The value.</param>
        public static void SetRegionManager(DependencyObject target, IRegionManager value)
        {
            target.SetValue(RegionManagerProperty, value);
        }

        private static void OnSetRegionManagerCallback(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            if (element != null)
            {
                string regionName = element.GetValue(RegionNameProperty) as string;
                if (regionName != null)
                {
                    IRegionManager oldRegionManager = args.OldValue as IRegionManager;
                    if (oldRegionManager != null)
                    {
                        oldRegionManager.Regions.Remove(regionName);
                    }

                    IRegionManager newRegionManager = args.NewValue as IRegionManager;
                    if (newRegionManager != null)
                    {
                        newRegionManager.AttachNewRegion(element, regionName);
                    }
                }
            }
        }

        #endregion

        private readonly RegionAdapterMappings regionAdapterMappings;
        private readonly IDictionary<string, IRegion> _regions;

        /// <summary>
        /// Initializes a new instance of <see cref="RegionManager"/>.
        /// </summary>
        public RegionManager()
        {
            _regions = new RegionsDictionary(this);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RegionManager"/>.
        /// </summary>
        /// <param name="mappings">The <see cref="RegionAdapterMappings"/> that
        /// will be used when calling <see cref="AttachNewRegion"/> explicitly
        /// or by creating regions by using attached properties through XAML.
        /// </param>
        public RegionManager(RegionAdapterMappings mappings)
            : this()
        {
            this.regionAdapterMappings = mappings;
        }

        /// <summary>
        /// Gets a dictionary of <see cref="IRegion"/> that identify each region by name. 
        /// You can use this dictionary to add or remove regions to the current region manager.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> with all the registered regions.</value>
        public IDictionary<string, IRegion> Regions
        {
            get { return _regions; }
        }

        /// <summary>
        /// Attaches a region to an object and adds it to the region manager.
        /// </summary>
        /// <param name="regionTarget">The object to adapt. This is typically a container (i.e a control).</param>
        /// <param name="regionName">The name of the region to register.</param>
        /// <exception cref="ArgumentException">When regions collection already has a region registered using <paramref name="regionName"/>.</exception>
        public void AttachNewRegion(object regionTarget, string regionName)
        {
            if (Regions.ContainsKey(regionName))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.RegionNameExistsException, regionName));

            IRegionAdapter regionAdapter = regionAdapterMappings.GetMapping(regionTarget.GetType());
            IRegion region = regionAdapter.Initialize(regionTarget);

            Regions.Add(regionName, region);
        }

        /// <summary>
        /// Creates a new region manager.
        /// </summary>
        /// <returns>A new region manager that can be used as a different scope from the current region manager.</returns>
        public IRegionManager CreateRegionManager()
        {
            return new RegionManager(this.regionAdapterMappings);
        }

        class RegionsDictionary : Dictionary<string, IRegion>, IDictionary<string, IRegion>
        {
            private readonly IRegionManager regionManager;

            internal RegionsDictionary(IRegionManager regionManager)
            {
                this.regionManager = regionManager;
            }

            void IDictionary<string, IRegion>.Add(string key, IRegion value)
            {
                base.Add(key, value);
                value.RegionManager = regionManager;
            }

            bool IDictionary<string, IRegion>.Remove(string key)
            {
                bool removed = false;
                if (this.ContainsKey(key))
                {
                    IRegion region = this[key];
                    removed = base.Remove(key);
                    region.RegionManager = null;
                }

                return removed;
            }
        }
    }
}