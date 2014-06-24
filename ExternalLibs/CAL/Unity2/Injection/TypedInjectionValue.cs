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
using Microsoft.Practices.Unity.Utility;

namespace Microsoft.Practices.Unity
{
    /// <summary>
    /// A base class for implementing <see cref="InjectionParameterValue"/> classes
    /// that deal in explicit types.
    /// </summary>
    public abstract class TypedInjectionValue : InjectionParameterValue
    {
        private readonly ReflectionHelper parameterReflector;

        /// <summary>
        /// Create a new <see cref="TypedInjectionValue"/> that exposes
        /// information about the given <paramref name="parameterType"/>.
        /// </summary>
        /// <param name="parameterType">Type of the parameter.</param>
        protected TypedInjectionValue(Type parameterType)
        {
            parameterReflector = new ReflectionHelper(parameterType);
        }

        /// <summary>
        /// The type of parameter this object represents.
        /// </summary>
        public virtual Type ParameterType
        {
            get { return parameterReflector.Type; }
        }


        /// <summary>
        /// Name for the type represented by this <see cref="InjectionParameterValue"/>.
        /// This may be an actual type name or a generic argument name.
        /// </summary>
        public override string ParameterTypeName
        {
            get { return parameterReflector.Type.Name; }
        }

        /// <summary>
        /// Test to see if this parameter value has a matching type for the given type.
        /// </summary>
        /// <param name="t">Type to check.</param>
        /// <returns>True if this parameter value is compatible with type <paramref name="t"/>,
        /// false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods",
            Justification="Validation done by Guard class.")]
        public override bool MatchesType(Type t)
        {
            Guard.ArgumentNotNull(t, "t");
            ReflectionHelper candidateReflector = new ReflectionHelper(t);
            if (candidateReflector.IsOpenGeneric && parameterReflector.IsOpenGeneric)
            {
                return candidateReflector.Type.GetGenericTypeDefinition() ==
                       parameterReflector.Type.GetGenericTypeDefinition();
            }

            return t.IsAssignableFrom(parameterReflector.Type);
        }
    }
}
