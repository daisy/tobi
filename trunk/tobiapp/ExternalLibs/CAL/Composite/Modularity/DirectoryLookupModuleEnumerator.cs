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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Policy;
using Microsoft.Practices.Composite.Properties;

namespace Microsoft.Practices.Composite.Modularity
{
    /// <summary>
    /// Implements a <see cref="IModuleEnumerator"/> that gets the module metadata by examining all the assemblies located in a specified path.
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand)]
    [SecurityPermission(SecurityAction.InheritanceDemand)]
    public class DirectoryLookupModuleEnumerator : IModuleEnumerator
    {
        private readonly string path;
        private IEnumerable<ModuleInfo> _modules;

        /// <summary>
        /// Initializes a new instance of <see cref="DirectoryLookupModuleEnumerator"/>.
        /// </summary>
        /// <param name="path">The path to look for assemblies with module metadata.</param>
        public DirectoryLookupModuleEnumerator(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.StringCannotBeNullOrEmpty, "path"));

            if (!Directory.Exists(path))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.DirectoryNotFound, path), "path");

            this.path = path;
        }

        /// <summary>
        /// Gets a list of metadata information of the modules in the specified path.
        /// </summary>
        /// <returns>An array of <see cref="ModuleInfo"/>.</returns>
        public ModuleInfo[] GetModules()
        {
            EnsureModulesDiscovered();
            return _modules.ToArray();
        }

        /// <summary>
        /// Gets a list of metadata information of the modules that should be loaded at startup in the specified path.
        /// </summary>
        /// <returns>An array of <see cref="ModuleInfo"/>.</returns>
        public ModuleInfo[] GetStartupLoadedModules()
        {
            EnsureModulesDiscovered();
            return _modules.Where(moduleInfo => moduleInfo.StartupLoaded == true).ToArray();
        }

        /// <summary>
        /// Gets the metadata information of a module by its name in the specified path.
        /// </summary>
        /// <param name="moduleName">The module's name.</param>
        /// <returns>A <see cref="ModuleInfo"/> associated with the <paramref name="moduleName"/> parameter.</returns>
        public ModuleInfo GetModule(string moduleName)
        {
            EnsureModulesDiscovered();
            return _modules.FirstOrDefault(moduleInfo => moduleInfo.ModuleName == moduleName);

        }

        private void EnsureModulesDiscovered()
        {
            if (_modules == null)
            {
                AppDomain childDomain = BuildChildDomain(AppDomain.CurrentDomain);

                try
                {
                    List<string> loadedAssemblies = new List<string>();

                    var assemblies = (
                        from Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                        where !(assembly is System.Reflection.Emit.AssemblyBuilder)
                            && !String.IsNullOrEmpty(assembly.Location)
                        select assembly.Location
                        );

                    loadedAssemblies.AddRange(assemblies);

                    Type loaderType = typeof(InnerModuleInfoLoader);

                    if (loaderType.Assembly != null)
                    {
                        var loader =
                            (InnerModuleInfoLoader)childDomain.CreateInstanceFrom(loaderType.Assembly.Location, loaderType.FullName).Unwrap();
                        loader.LoadAssemblies(loadedAssemblies);
                        _modules = loader.GetModuleInfos(path);
                    }
                }
                finally
                {
                    AppDomain.Unload(childDomain);
                }
            }
        }

        /// <summary>
        /// Creates a new child domain and copies the evidence from a parent domain.
        /// </summary>
        /// <param name="parentDomain">The parent domain.</param>
        /// <returns>The new child domain.</returns>
        /// <remarks>
        /// Grabs the <paramref name="parentDomain"/> evidence and uses it to construct the new
        /// <see cref="AppDomain"/> because in a ClickOnce execution environment, creating an
        /// <see cref="AppDomain"/> will by default pick up the partial trust environment of 
        /// the AppLaunch.exe, which was the root executable. The AppLaunch.exe does a 
        /// create domain and applies the evidence from the ClickOnce manifests to 
        /// create the domain that the application is actually executing in. This will 
        /// need to be Full Trust for Composite Application Library applications.
        /// </remarks>
        protected virtual AppDomain BuildChildDomain(AppDomain parentDomain)
        {
            Evidence evidence = new Evidence(parentDomain.Evidence);
            AppDomainSetup setup = parentDomain.SetupInformation;
            return AppDomain.CreateDomain("DiscoveryRegion", evidence, setup);
        }

        class InnerModuleInfoLoader : MarshalByRefObject
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            internal ModuleInfo[] GetModuleInfos(string path)
            {
                DirectoryInfo directory = new DirectoryInfo(path);

                ResolveEventHandler resolveEventHandler =
                    delegate(object sender, ResolveEventArgs args)
                    {
                        Assembly loadedAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(
                                asm => string.Equals(asm.FullName, args.Name, StringComparison.InvariantCultureIgnoreCase));
                        if (loadedAssembly != null)
                        {
                            return loadedAssembly;
                        }
                        AssemblyName assemblyName = new AssemblyName(args.Name);
                        string dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + ".dll");
                        if (File.Exists(dependentAssemblyFilename))
                        {
                            return Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
                        }
                        return Assembly.ReflectionOnlyLoad(args.Name);
                    };

                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveEventHandler;

                Assembly moduleReflectionOnlyAssembly =
                    AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().First(
                        asm => asm.FullName == typeof(IModule).Assembly.FullName);
                Type IModuleType = moduleReflectionOnlyAssembly.GetType(typeof(IModule).FullName);

                Assembly[] alreadyLoadedAssemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();
                var modules = directory.GetFiles("*.dll")
                    .Where(file => alreadyLoadedAssemblies
                        .FirstOrDefault(assembly => String.Compare(Path.GetFileName(assembly.Location), file.Name, StringComparison.OrdinalIgnoreCase) == 0) == null)
                    .SelectMany(file => Assembly.ReflectionOnlyLoadFrom(file.FullName)
                                            .GetExportedTypes()
                                            .Where(IModuleType.IsAssignableFrom)
                                            .Where(t => t != IModuleType)
                                            .Select(type => CreateModuleInfo(type)));

                var array = modules.ToArray();
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveEventHandler;
                return array;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            internal void LoadAssemblies(IEnumerable<string> assemblies)
            {
                foreach (string assemblyPath in assemblies)
                {
                    Assembly.ReflectionOnlyLoadFrom(assemblyPath);
                }
            }

            static ModuleInfo CreateModuleInfo(Type type)
            {
                string moduleName = type.Name;
                List<string> dependsOn = new List<string>();
                bool startupLoaded = true;
                var moduleAttribute = CustomAttributeData.GetCustomAttributes(type).FirstOrDefault(cad => cad.Constructor.DeclaringType.FullName == typeof(ModuleAttribute).FullName);

                if (moduleAttribute != null)
                {
                    foreach (CustomAttributeNamedArgument argument in moduleAttribute.NamedArguments)
                    {
                        string argumentName = argument.MemberInfo.Name;
                        if (argumentName == "ModuleName")
                            moduleName = (string)argument.TypedValue.Value;
                        else if (argumentName == "StartupLoaded")
                            startupLoaded = (bool)argument.TypedValue.Value;
                    }
                }

                var moduleDependencyAttributes = CustomAttributeData.GetCustomAttributes(type).Where(cad => cad.Constructor.DeclaringType.FullName == typeof(ModuleDependencyAttribute).FullName);
                foreach (CustomAttributeData cad in moduleDependencyAttributes)
                {
                    dependsOn.Add((string)cad.ConstructorArguments[0].Value);
                }

                return new ModuleInfo(type.Assembly.Location, type.FullName, moduleName, startupLoaded, dependsOn.ToArray());
            }
        }
    }
}
