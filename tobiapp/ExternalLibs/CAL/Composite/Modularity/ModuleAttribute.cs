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

namespace Microsoft.Practices.Composite.Modularity
{
    /// <summary>
    /// Indicates that the class should be considered a named module using the
    /// provided module name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ModuleAttribute : Attribute
    {
        private bool _startupLoaded = true;

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        /// <value>The name of the module.</value>
        public string ModuleName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module should be loaded at startup. 
        /// </summary>
        /// When <see langword="true"/> (default value), it indicates that this module should be loaded at startup. 
        /// Otherwise you should explicitly load this module on demand.
        /// <value>A <see cref="bool"/> value.</value>
        public bool StartupLoaded
        {
            get { return _startupLoaded; }
            set { _startupLoaded = value; }
        }
    }
}
