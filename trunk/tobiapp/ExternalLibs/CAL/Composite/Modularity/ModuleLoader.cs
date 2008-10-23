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
using System.Reflection;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Properties;

namespace Microsoft.Practices.Composite.Modularity
{
    /// <summary>
    /// Implements the <see cref="IModuleLoader"/> interface. Handles initialization of a set of modules based on types.
    /// </summary>
    public class ModuleLoader : IModuleLoader
    {
        private readonly IDictionary<string, Type> initializedModules = new Dictionary<string, Type>();
        private readonly IDictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

        private readonly IContainerFacade containerFacade;
        private readonly ILoggerFacade loggerFacade;

        /// <summary>
        /// Initializes a new instance of <see cref="ModuleLoader"/>.
        /// </summary>
        /// <param name="containerFacade">The container that will be used to resolve the modules by specifying its type.</param>
        /// <param name="loggerFacade">The logger to use.</param>
        public ModuleLoader(IContainerFacade containerFacade, ILoggerFacade loggerFacade)
        {
            if (containerFacade == null)
                throw new ArgumentNullException("containerFacade");

            if (loggerFacade == null)
                throw new ArgumentNullException("loggerFacade");

            this.containerFacade = containerFacade;
            this.loggerFacade = loggerFacade;
        }

        /// <summary>
        /// Gets the loaded assemblies. This property is used for testability and it is not intended to be used by inheritors.
        /// </summary>
        /// <value>A <seealso cref="IDictionary{TKey,TValue}"/> with the loaded asemblies.</value>
        protected IDictionary<string, Assembly> LoadedAssemblies
        {
            get { return loadedAssemblies; }
        }

        /// <summary>
        /// Initialize the specified list of modules.
        /// </summary>
        /// <param name="moduleInfos">The list of modules to initialize.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void Initialize(ModuleInfo[] moduleInfos)
        {
            List<ModuleInfo> modules = GetModulesLoadOrder(moduleInfos);

            IEnumerable<ModuleInfo> newModules = LoadAssembliesAndTypes(modules);

            foreach (ModuleInfo moduleInfo in newModules)
            {
                Type type = initializedModules[moduleInfo.ModuleType];

                try
                {
                    IModule module = CreateModule(type);
                    module.Initialize();
                }
                catch (Exception e)
                {
                    HandleModuleLoadError(moduleInfo, type.Assembly.FullName, e);
                }
            }
        }

        /// <summary>
        /// Uses the container to resolve a new <see cref="IModule"/> by specifying its <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type to resolve. This type must implement <see cref="IModule"/>.</param>
        /// <returns>A new instance of <paramref name="type"/>.</returns>
        protected virtual IModule CreateModule(Type type)
        {
            return (IModule)containerFacade.Resolve(type);
        }

        private static List<ModuleInfo> GetModulesLoadOrder(IEnumerable<ModuleInfo> moduleInfoEnumerator)
        {
            Dictionary<string, ModuleInfo> indexedInfo = new Dictionary<string, ModuleInfo>();
            ModuleDependencySolver solver = new ModuleDependencySolver();
            List<ModuleInfo> result = new List<ModuleInfo>();

            foreach (ModuleInfo data in moduleInfoEnumerator)
            {
                if (indexedInfo.ContainsKey(data.ModuleName))
                    throw new ModuleLoadException(String.Format(CultureInfo.CurrentCulture,
                                                                Resources.DuplicatedModule, data.ModuleName));

                indexedInfo.Add(data.ModuleName, data);
                solver.AddModule(data.ModuleName);

                foreach (string dependency in data.DependsOn)
                    solver.AddDependency(data.ModuleName, dependency);
            }

            if (solver.ModuleCount > 0)
            {
                string[] loadOrder = solver.Solve();

                for (int i = 0; i < loadOrder.Length; i++)
                    result.Add(indexedInfo[loadOrder[i]]);
            }

            return result;
        }

        private IEnumerable<ModuleInfo> LoadAssembliesAndTypes(IEnumerable<ModuleInfo> modules)
        {
            var newModules = new List<ModuleInfo>();

            foreach (ModuleInfo module in modules)
            {
                if (!initializedModules.ContainsKey(module.ModuleType))
                {
                    Assembly assembly = LoadAssembly(module);

                    Type type = assembly.GetType(module.ModuleType);

                    initializedModules.Add(module.ModuleType, type);
                    newModules.Add(module);
                }
            }
            return newModules;
        }

        /// <summary>
        /// Loads an assembly based on the information in the specified <see cref="ModuleInfo"/>.
        /// </summary>
        /// <param name="moduleInfo">The information that describes what assembly to load.</param>
        /// <returns>The loaded assembly.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        protected virtual Assembly LoadAssembly(ModuleInfo moduleInfo)
        {
            GuardLegalAssemblyFile(moduleInfo);
            string assemblyFile = moduleInfo.AssemblyFile;

            if (String.IsNullOrEmpty(assemblyFile))
                throw new ArgumentException(
                    (string.Format(CultureInfo.CurrentCulture, Resources.StringCannotBeNullOrEmpty, "assemblyFile")));

            assemblyFile = GetModulePath(assemblyFile);

            FileInfo file = new FileInfo(assemblyFile);
            Assembly assembly;

            if (loadedAssemblies.ContainsKey(file.Name))
                return loadedAssemblies[file.Name];

            try
            {
                assembly = Assembly.LoadFrom(file.FullName);

                loadedAssemblies.Add(file.Name, assembly);
            }
            catch (Exception ex)
            {
                throw new ModuleLoadException(null, assemblyFile, ex.Message, ex);
            }

            return assembly;
        }

        private static string GetModulePath(string assemblyFile)
        {
            if (String.IsNullOrEmpty(assemblyFile))
                throw new ArgumentNullException("assemblyFile");

            if (Path.IsPathRooted(assemblyFile) == false)
                assemblyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyFile);

            return assemblyFile;
        }

        private static void GuardLegalAssemblyFile(ModuleInfo moduleInfo)
        {
            if (moduleInfo == null)
                throw new ArgumentNullException("moduleInfo");

            string assemblyFilePath = GetModulePath(moduleInfo.AssemblyFile);

            if (File.Exists(assemblyFilePath) == false)
            {
                throw new ModuleLoadException(
                    string.Format(CultureInfo.CurrentCulture,
                                  Resources.ModuleNotFound, assemblyFilePath));
            }
        }

        /// <summary>
        /// Handles any exception ocurred in the module loading process,
        /// logs the error using the <seealso cref="ILoggerFacade"/> and throws a <seealso cref="ModuleLoadException"/>.
        /// This method can be overriden to provide a different behavior. 
        /// </summary>
        /// <param name="moduleInfo">The module metadata where the error happenened.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="exception">The exception thrown that is the cause of the current error.</param>
        /// <exception cref="ModuleLoadException"></exception>
        public virtual void HandleModuleLoadError(ModuleInfo moduleInfo, string assemblyName, Exception exception)
        {
            Exception moduleException = new ModuleLoadException(moduleInfo.ModuleName, assemblyName, exception.Message,
                                                                exception);

            loggerFacade.Log(moduleException.Message, Category.Exception, Priority.High);

            throw moduleException;
        }
    }
}
