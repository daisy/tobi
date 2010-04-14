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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity.Properties;

namespace Microsoft.Practices.ObjectBuilder2
{
    /// <summary>
    /// Base class that provides an implementation of <see cref="IConstructorSelectorPolicy"/>
    /// which lets you override how the parameter resolvers are created.
    /// </summary>
    public abstract class ConstructorSelectorPolicyBase<TInjectionConstructorMarkerAttribute> : IConstructorSelectorPolicy
        where TInjectionConstructorMarkerAttribute : Attribute
    {
        /// <summary>
        /// Choose the constructor to call for the given type.
        /// </summary>
        /// <param name="context">Current build context</param>
        /// <param name="resolverPolicyDestination">The <see cref='IPolicyList'/> to add any
        /// generated resolver objects into.</param>
        /// <returns>The chosen constructor.</returns>
        public SelectedConstructor SelectConstructor(IBuilderContext context, IPolicyList resolverPolicyDestination)
        {
            Type typeToConstruct = context.BuildKey.Type;
            ConstructorInfo ctor = FindInjectionConstructor(typeToConstruct) ?? FindLongestConstructor(typeToConstruct);
            if (ctor != null)
            {
                return CreateSelectedConstructor(context, resolverPolicyDestination, ctor);
            }
            return null;
        }

        private SelectedConstructor CreateSelectedConstructor(IBuilderContext context, IPolicyList resolverPolicyDestination, ConstructorInfo ctor)
        {
            SelectedConstructor result = new SelectedConstructor(ctor);
            
            foreach(ParameterInfo param in ctor.GetParameters())
            {
                string key = Guid.NewGuid().ToString();
                IDependencyResolverPolicy policy = CreateResolver(param);
                resolverPolicyDestination.Set<IDependencyResolverPolicy>(policy, key);
                DependencyResolverTrackerPolicy.TrackKey(resolverPolicyDestination,
                    context.BuildKey,
                    key);
                result.AddParameterKey(key);
            }
            return result;
        }

        /// <summary>
        /// Create a <see cref="IDependencyResolverPolicy"/> instance for the given
        /// <see cref="ParameterInfo"/>.
        /// </summary>
        /// <param name="parameter">Parameter to create the resolver for.</param>
        /// <returns>The resolver object.</returns>
        protected abstract IDependencyResolverPolicy CreateResolver(ParameterInfo parameter);

        private static ConstructorInfo FindInjectionConstructor(Type typeToConstruct)
        {
            ConstructorInfo[] injectionConstructors = typeToConstruct.GetConstructors()
                .Where(ctor => ctor.IsDefined(
                    typeof (TInjectionConstructorMarkerAttribute),
                    true))
                .ToArray();

            switch(injectionConstructors.Length)
            {
                case 0:
                    return null;

                case 1:
                    return injectionConstructors[0];

                default:
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.MultipleInjectionConstructors,
                            typeToConstruct.Name));
            }
        }

        private static ConstructorInfo FindLongestConstructor(Type typeToConstruct)
        {
            ConstructorInfo[] constructors = typeToConstruct.GetConstructors();
            Array.Sort(constructors, new ConstructorLengthComparer());

            switch(constructors.Length)
            {
                case 0:
                    return null;

                case 1:
                    return constructors[0];

                default:
                    int paramLength = constructors[0].GetParameters().Length;
                    if(constructors[1].GetParameters().Length == paramLength)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.AmbiguousInjectionConstructor,
                                typeToConstruct.Name,
                                paramLength));
                    }
                    return constructors[0];
            }
        }

        private class ConstructorLengthComparer : IComparer<ConstructorInfo>
        {
            ///<summary>
            ///Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            ///</summary>
            ///
            ///<returns>
            ///Value Condition Less than zerox is less than y.Zerox equals y.Greater than zerox is greater than y.
            ///</returns>
            ///
            ///<param name="y">The second object to compare.</param>
            ///<param name="x">The first object to compare.</param>
            public int Compare(ConstructorInfo x, ConstructorInfo y)
            {
                return y.GetParameters().Length - x.GetParameters().Length;
            }
        }
    }
}
