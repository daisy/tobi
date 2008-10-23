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
using Microsoft.Practices.Unity;

namespace Microsoft.Practices.Composite.UnityExtensions
{
    /// <summary>
    /// Defines a <seealso cref="IUnityContainer"/> adapter for
    /// the <see cref="IContainerFacade"/> interface
    /// to be used by the Composite Application Library.
    /// </summary>
    public class UnityContainerAdapter : IContainerFacade
    {
        private readonly IUnityContainer _unityContainer;

        /// <summary>
        /// Initializes a new instance of <see cref="UnityContainerAdapter"/>.
        /// </summary>
        /// <param name="unityContainer">The <seealso cref="IUnityContainer"/> that will be used
        /// by the <see cref="Resolve"/> and <see cref="TryResolve"/> methods.</param>
        public UnityContainerAdapter(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        /// <summary>
        /// Resolve an instance of the requested type from the container.
        /// </summary>
        /// <param name="type">The type of object to get from the container.</param>
        /// <returns>An instance of <paramref name="type"/>.</returns>
        /// <exception cref="ResolutionFailedException"><paramref name="type"/> cannot be resolved by the container.</exception>
        public object Resolve(Type type)
        {
            return _unityContainer.Resolve(type);
        }

        /// <summary>
        /// Tries to resolve an instance of the requested type from the container.
        /// </summary>
        /// <param name="type">The type of object to get from the container.</param>
        /// <returns>
        /// An instance of <paramref name="type"/>. 
        /// If the type cannot be resolved it will return a <see langword="null"/> value.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public object TryResolve(Type type)
        {
            object resolved;

            try
            {
                resolved = Resolve(type);
            }
            catch
            {
                resolved = null;
            }

            return resolved;
        }
    }
}