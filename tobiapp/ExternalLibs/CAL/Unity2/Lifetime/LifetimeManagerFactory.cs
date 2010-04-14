﻿//===============================================================================
// Microsoft patterns & practices
// Unity Application Block
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System;
using Microsoft.Practices.ObjectBuilder2;

namespace Microsoft.Practices.Unity
{
    /// <summary>
    /// An implementation of <see cref="ILifetimeFactoryPolicy"/> that
    /// creates instances of the type of the given Lifetime Manager
    /// by resolving them through the container.
    /// </summary>
    public class LifetimeManagerFactory : ILifetimeFactoryPolicy
    {
        private readonly ExtensionContext containerContext;

        /// <summary>
        /// Create a new <see cref="LifetimeManagerFactory"/> that will
        /// return instances of the given type, creating them by
        /// resolving through the container.
        /// </summary>
        /// <param name="containerContext">Container to resolve with.</param>
        /// <param name="lifetimeType">Type of LifetimeManager to create.</param>
        public LifetimeManagerFactory(ExtensionContext containerContext, Type lifetimeType)
        {
            this.containerContext = containerContext;
            LifetimeType = lifetimeType;
        }

        /// <summary>
        /// Create a new instance of <see cref="ILifetimePolicy"/>.
        /// </summary>
        /// <returns>The new instance.</returns>
        public ILifetimePolicy CreateLifetimePolicy()
        {
            var lifetime = (LifetimeManager)containerContext.Container.Resolve(LifetimeType);
            if(lifetime is IDisposable)
            {
                containerContext.Lifetime.Add(lifetime);
            }
            lifetime.InUse = true;
            return lifetime;
        }

        /// <summary>
        /// The type of Lifetime manager that will be created by this factory.
        /// </summary>
        public Type LifetimeType { get; private set; }
    }
}
