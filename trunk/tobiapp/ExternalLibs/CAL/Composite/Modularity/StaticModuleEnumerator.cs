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
using System.Linq;

namespace Microsoft.Practices.Composite.Modularity
{
    /// <summary>
    /// Implements a <see cref="IModuleEnumerator"/> that enumerates through module metadata defined explicitly.
    /// </summary>
    public class StaticModuleEnumerator : IModuleEnumerator
    {
        readonly List<ModuleInfo> _modules = new List<ModuleInfo>();

        /// <summary>
        /// Gets a list of metadata information of the modules.
        /// </summary>
        /// <returns>An array of <see cref="ModuleInfo"/>.</returns>
        public ModuleInfo[] GetModules()
        {
            return _modules.ToArray();
        }

        /// <summary>
        /// Gets a list of metadata information of the modules that should be loaded at startup.
        /// </summary>
        /// <returns>An array of <see cref="ModuleInfo"/>.</returns>
        public ModuleInfo[] GetStartupLoadedModules()
        {
            return _modules.ToArray();
        }

        /// <summary>
        /// Gets the metadata information of a module by its name.
        /// </summary>
        /// <param name="moduleName">The module's name.</param>
        /// <returns>A <see cref="ModuleInfo"/> associated with the <paramref name="moduleName"/> parameter.</returns>
        public ModuleInfo GetModule(string moduleName)
        {
            return _modules.FirstOrDefault(moduleInfo => moduleInfo.ModuleName == moduleName);
        }

        /// <summary>
        /// Registers a module with the <see cref="StaticModuleEnumerator" />.
        /// </summary>
        /// <param name="moduleType">The module type. This class should implement <see cref="IModule"/>.</param>
        /// <param name="dependsOn">The names of the modules that this module depends on, if any.</param>
        /// <returns>The same instance of <see cref="StaticModuleEnumerator"/>.</returns>
        /// <remarks>The module name will be the Name of the type specified in <paramref name="moduleType"/>.</remarks>
        public StaticModuleEnumerator AddModule(Type moduleType, params string[] dependsOn)
        {
            ModuleInfo moduleInfo = new ModuleInfo(moduleType.Assembly.Location
                                                   , moduleType.FullName
                                                   , moduleType.Name
                                                   , dependsOn);
            _modules.Add(moduleInfo);
            return this;
        }
    }
}