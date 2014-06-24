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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity.Properties;
using Microsoft.Practices.Unity.Utility;

namespace Microsoft.Practices.Unity
{
    /// <summary>
    /// Base class for <see cref="InjectionParameterValue"/> subclasses that let you specify that
    /// an instance of a generic type parameter should be resolved.
    /// </summary>
    public abstract class GenericParameterBase : InjectionParameterValue
    {
        private readonly string genericParameterName;
        private readonly bool isArray;
        private readonly string resolutionKey;

        /// <summary>
        /// Create a new <see cref="GenericParameter"/> instance that specifies
        /// that the given named generic parameter should be resolved.
        /// </summary>
        /// <param name="genericParameterName">The generic parameter name to resolve.</param>
        protected GenericParameterBase(string genericParameterName)
            : this(genericParameterName, null)
        { }

        /// <summary>
        /// Create a new <see cref="GenericParameter"/> instance that specifies
        /// that the given named generic parameter should be resolved.
        /// </summary>
        /// <param name="genericParameterName">The generic parameter name to resolve.</param>
        /// <param name="resolutionKey">name to use when looking up in the container.</param>
        protected GenericParameterBase(string genericParameterName, string resolutionKey)
        {
            Guard.ArgumentNotNull(genericParameterName, "genericParameterName");
            if (genericParameterName.EndsWith("[]", StringComparison.Ordinal) || genericParameterName.EndsWith("()", StringComparison.Ordinal))
            {
                this.genericParameterName = genericParameterName.Replace("[]", "").Replace("()", "");
                this.isArray = true;
            }
            else
            {
                this.genericParameterName = genericParameterName;
                this.isArray = false;
            }
            this.resolutionKey = resolutionKey;
        }

        /// <summary>
        /// Name for the type represented by this <see cref="InjectionParameterValue"/>.
        /// This may be an actual type name or a generic argument name.
        /// </summary>
        public override string ParameterTypeName
        {
            get { return genericParameterName; }
        }

        /// <summary>
        /// Test to see if this parameter value has a matching type for the given type.
        /// </summary>
        /// <param name="t">Type to check.</param>
        /// <returns>True if this parameter value is compatible with type <paramref name="t"/>,
        /// false if not.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods",
            Justification = "Validation done by Guard class")]
        public override bool MatchesType(Type t)
        {
            Guard.ArgumentNotNull(t, "t");
            if (!isArray)
            {
                return t.IsGenericParameter && t.Name == genericParameterName;
            }
            return t.IsArray && t.GetElementType().IsGenericParameter && t.GetElementType().Name == genericParameterName;
        }

        /// <summary>
        /// Return a <see cref="IDependencyResolverPolicy"/> instance that will
        /// return this types value for the parameter.
        /// </summary>
        /// <param name="typeToBuild">Type that contains the member that needs this parameter. Used
        /// to resolve open generic parameters.</param>
        /// <returns>The <see cref="IDependencyResolverPolicy"/>.</returns>
        public override IDependencyResolverPolicy GetResolverPolicy(Type typeToBuild)
        {
            GuardTypeToBuildIsGeneric(typeToBuild);
            GuardTypeToBuildHasMatchingGenericParameter(typeToBuild);
            Type typeToResolve = new ReflectionHelper(typeToBuild).GetNamedGenericParameter(genericParameterName);
            if (isArray)
            {
                typeToResolve = typeToResolve.MakeArrayType();
            }

            return DoGetResolverPolicy(typeToResolve, this.resolutionKey);
        }

        /// <summary>
        /// Return a <see cref="IDependencyResolverPolicy"/> instance that will
        /// return this types value for the parameter.
        /// </summary>
        /// <param name="typeToResolve">The actual type to resolve.</param>
        /// <param name="resolutionKey">The resolution key.</param>
        /// <returns>The <see cref="IDependencyResolverPolicy"/>.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "resolutionKey",
            Justification = "protected method parameter collides with private field - not an issue.")]
        protected abstract IDependencyResolverPolicy DoGetResolverPolicy(Type typeToResolve, string resolutionKey);

        private void GuardTypeToBuildIsGeneric(Type typeToBuild)
        {
            if (!typeToBuild.IsGenericType)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.NotAGenericType,
                        typeToBuild.Name,
                        genericParameterName));
            }
        }

        private void GuardTypeToBuildHasMatchingGenericParameter(Type typeToBuild)
        {
            foreach (Type genericParam in typeToBuild.GetGenericTypeDefinition().GetGenericArguments())
            {
                if (genericParam.Name == genericParameterName)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.NoMatchingGenericArgument,
                    typeToBuild.Name,
                    genericParameterName));
        }
    }
}
