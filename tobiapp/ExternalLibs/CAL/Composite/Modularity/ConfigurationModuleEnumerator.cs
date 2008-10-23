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
    /// Implements a <see cref="IModuleEnumerator"/> that gets the modules metadata from
    /// configuration files.
    /// </summary>
    public class ConfigurationModuleEnumerator : IModuleEnumerator
    {
        private IList<ModuleInfo> _modules;
        private readonly ConfigurationStore _store;

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationModuleEnumerator"/>.
        /// </summary>
        public ConfigurationModuleEnumerator()
            : this(new ConfigurationStore())
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationModuleEnumerator"/>.
        /// </summary>
        /// <param name="store">The <see cref="ConfigurationStore"/> to use to get the module configuration.</param>
        public ConfigurationModuleEnumerator(ConfigurationStore store)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            this._store = store;
        }

        /// <summary>
        /// Gets a list of metadata information of the modules specified in the configuration files.
        /// </summary>
        /// <returns>An array of <see cref="ModuleInfo"/>.</returns>
        public ModuleInfo[] GetModules()
        {
            EnsureModulesDiscovered();
            return _modules.ToArray();
        }

        private void EnsureModulesDiscovered()
        {
            if (_modules == null)
            {
                _modules = new List<ModuleInfo>();

                ModulesConfigurationSection section = _store.RetrieveModuleConfigurationSection();

                if (section != null)
                {
                    foreach (ModuleConfigurationElement element in section.Modules)
                    {
                        IList<string> dependencies = new List<string>();

                        if (element.Dependencies.Count > 0)
                        {
                            foreach (ModuleDependencyConfigurationElement dependency in element.Dependencies)
                            {
                                dependencies.Add(dependency.ModuleName);
                            }
                        }

                        ModuleInfo moduleInfo = new ModuleInfo(element.AssemblyFile, element.ModuleType,
                                                               element.ModuleName,
                                                               element.StartupLoaded, dependencies.ToArray());
                        _modules.Add(moduleInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of metadata information of the modules that should be loaded at startup specified in the configuration files.
        /// </summary>
        /// <returns>An array of <see cref="ModuleInfo"/>.</returns>
        public ModuleInfo[] GetStartupLoadedModules()
        {
            EnsureModulesDiscovered();
            return _modules.Where(moduleInfo => moduleInfo.StartupLoaded == true).ToArray();
        }

        /// <summary>
        /// Gets the metadata information of a module by its name specified in the configuration files.
        /// </summary>
        /// <param name="moduleName">The module's name.</param>
        /// <returns>A <see cref="ModuleInfo"/> associated with the <paramref name="moduleName"/> parameter.</returns>
        public ModuleInfo GetModule(string moduleName)
        {
            EnsureModulesDiscovered();
            return _modules.FirstOrDefault(moduleInfo => moduleInfo.ModuleName == moduleName);
        }
    }
}