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
using Microsoft.Practices.Composite.Properties;

namespace Microsoft.Practices.Composite.Modularity
{
    /// <summary>
    /// Defines the metadata that describes a module.
    /// </summary>
    [Serializable]
    public class ModuleInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ModuleInfo"/>.
        /// </summary>
        /// <param name="assemblyFile">The assembly file. Must be different than <see langword="null" />.</param>
        /// <param name="moduleType">The module's type.</param>
        /// <param name="moduleName">The module's name.</param>
        /// <param name="dependsOn">The names of the modules that this depends on.</param>
        public ModuleInfo(string assemblyFile, string moduleType, string moduleName, params string[] dependsOn)
            : this(assemblyFile, moduleType, moduleName, true, dependsOn)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ModuleInfo"/>.
        /// </summary>
        /// <param name="assemblyFile">The assembly file. Must be different than <see langword="null" />.</param>
        /// <param name="moduleType">The module's type.</param>
        /// <param name="moduleName">The module's name.</param>
        /// <param name="startupLoaded">Indicates whether this module should be loaded on startup.</param>
        /// <param name="dependsOn">The names of the modules that this depends on.</param>
        public ModuleInfo(string assemblyFile, string moduleType, string moduleName, bool startupLoaded, params string[] dependsOn)
        {
            if (string.IsNullOrEmpty(assemblyFile))
                throw new ArgumentException(Resources.StringCannotBeNullOrEmpty, "assemblyFile");

            if (string.IsNullOrEmpty(moduleType))
                throw new ArgumentException(Resources.StringCannotBeNullOrEmpty, "moduleType");

            AssemblyFile = assemblyFile;
            ModuleType = moduleType;
            ModuleName = moduleName;
            StartupLoaded = startupLoaded;
            DependsOn = dependsOn != null ? new List<string>(dependsOn) : new List<string>();
        }

        /// <summary>
        /// Gets the assembly file where the module is located.
        /// </summary>
        /// <value>The assembly file where the module is located.</value>
        public string AssemblyFile { get; private set; }

        /// <summary>
        /// Gets the type of the module.
        /// </summary>
        /// <value>The type of the module.</value>
        public string ModuleType { get; private set; }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        /// <value>The name of the module.</value>
        public string ModuleName { get; private set; }

        /// <summary>
        /// Gets the list of modules that this module depends upon.
        /// </summary>
        /// <value>The list of modules that this module depends upon.</value>
        public IList<string> DependsOn { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the module should be loaded at startup. 
        /// </summary>
        /// <value>A <see langword="bool"/> value indicating whether the module should be loaded at startup.</value>
        public bool StartupLoaded { get; private set; }
    }
}
