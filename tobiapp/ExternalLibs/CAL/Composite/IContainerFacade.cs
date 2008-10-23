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

namespace Microsoft.Practices.Composite
{
    /// <summary>
    /// Defines a simple Inversion of Control container façade
    /// to be used by the Composite Application Library.
    /// </summary>
    public interface IContainerFacade
    {
        /// <summary>
        /// Resolve an instance of the requested type from the container.
        /// </summary>
        /// <param name="type">The type of object to get from the container.</param>
        /// <returns>An instance of <paramref name="type"/>.</returns>
        object Resolve(Type type);

        /// <summary>
        /// Tries to resolve an instance of the requested type from the container.
        /// </summary>
        /// <param name="type">The type of object to get from the container.</param>
        /// <returns>
        /// An instance of <paramref name="type"/>. 
        /// If the type cannot be resolved it will return a <see langword="null" /> value.
        /// </returns>
        object TryResolve(Type type);
    }
}