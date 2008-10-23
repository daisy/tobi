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
using System.Configuration;
using System.IO;

namespace Microsoft.Practices.Composite.Modularity
{
    /// <summary>
    /// Defines a store for the module metadata.
    /// </summary>
    public class ConfigurationStore
    {
        private readonly string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationStore"/>. It uses the Application's directory as the base for looking config files.
        /// </summary>
        public ConfigurationStore()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationStore"/>.
        /// </summary>
        /// <param name="baseDirectory">The directory from which to start searching for the 
        /// configuration files.</param>
        public ConfigurationStore(string baseDirectory)
        {
            _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baseDirectory ?? string.Empty);
        }

        /// <summary>
        /// Gets the module configuration data.
        /// </summary>
        /// <returns>A <see cref="ModulesConfigurationSection"/> instance.</returns>
        public virtual ModulesConfigurationSection RetrieveModuleConfigurationSection()
        {
            foreach (string fileName in Directory.GetFiles(_baseDirectory, "*.config", SearchOption.TopDirectoryOnly))
            {
                System.Configuration.Configuration configuration =
                    GetConfiguration(Path.Combine(_baseDirectory, fileName));

                ModulesConfigurationSection section = (ModulesConfigurationSection)configuration.GetSection("modules");

                if (section != null)
                {
                    return section;
                }
            }

            return null;
        }

        private static System.Configuration.Configuration GetConfiguration(string configFilePath)
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap { ExeConfigFilename = configFilePath };
            return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }
    }
}