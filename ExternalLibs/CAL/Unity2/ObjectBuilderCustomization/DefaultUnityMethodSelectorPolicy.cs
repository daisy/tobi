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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ObjectBuilder2;

namespace Microsoft.Practices.Unity.ObjectBuilder
{
    /// <summary>
    /// An implementation of <see cref="IMethodSelectorPolicy"/> that is aware
    /// of the build keys used by the Unity container.
    /// </summary>
    public class DefaultUnityMethodSelectorPolicy : MethodSelectorPolicyBase<InjectionMethodAttribute>
    {
        /// <summary>
        /// Create a <see cref="IDependencyResolverPolicy"/> instance for the given
        /// <see cref="ParameterInfo"/>.
        /// </summary>
        /// <param name="parameter">Parameter to create the resolver for.</param>
        /// <returns>The resolver object.</returns>
        protected override IDependencyResolverPolicy CreateResolver(ParameterInfo parameter)
        {
            var attributes = parameter.GetCustomAttributes(false)
                .OfType<DependencyResolutionAttribute>()
                .ToList();

            if(attributes.Count > 0)
            {
                return attributes[0].CreateResolver(parameter.ParameterType);
            }

            return new NamedTypeDependencyResolverPolicy(parameter.ParameterType, null);
        }
    }
}
