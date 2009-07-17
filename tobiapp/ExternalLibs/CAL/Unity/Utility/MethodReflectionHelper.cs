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
using System.Reflection;
using Microsoft.Practices.ObjectBuilder2;

namespace Microsoft.Practices.Unity.Utility
{
    /// <summary>
    /// Helper class to wrap common reflection stuff dealing with
    /// methods.
    /// </summary>
    public class MethodReflectionHelper
    {
        private readonly MethodBase method;

        /// <summary>
        /// Create a new <see cref="MethodReflectionHelper"/> instance that
        /// lets us do more reflection stuff on that method.
        /// </summary>
        /// <param name="method"></param>
        public MethodReflectionHelper(MethodBase method)
        {
            this.method = method;
        }

        /// <summary>
        /// Returns true if any of the parameters of this method
        /// are open generics.
        /// </summary>
        public bool MethodHasOpenGenericParameters
        {
            get
            {
                return Sequence.Exists(GetParameterReflectors(),
                    delegate(ParameterReflectionHelper r) { return r.IsOpenGeneric; });
            }
        }

        /// <summary>
        /// Return the <see cref="System.Type"/> of each parameter for this
        /// method.
        /// </summary>
        /// <returns>Sequence of <see cref="System.Type"/> objects, one for
        /// each parameter in order.</returns>
        public IEnumerable<Type> ParameterTypes
        {
            get
            {
                foreach(ParameterInfo param in method.GetParameters())
                {
                    yield return param.ParameterType;
                }
                
            }
        }

        /// <summary>
        /// Given our set of generic type arguments, 
        /// </summary>
        /// <param name="genericTypeArguments"></param>
        /// <returns></returns>
        public Type[] GetClosedParameterTypes(Type[] genericTypeArguments)
        {
            return Sequence.ToArray(GetClosedParameterTypesSequence(genericTypeArguments));
        }

        private IEnumerable<ParameterReflectionHelper> GetParameterReflectors()
        {
            foreach(ParameterInfo pi in method.GetParameters())
            {
                yield return new ParameterReflectionHelper(pi);
            }
        }

        private IEnumerable<Type> GetClosedParameterTypesSequence(Type[] genericTypeArguments)
        {
            foreach(ParameterReflectionHelper r in GetParameterReflectors())
            {
                yield return r.GetClosedParameterType(genericTypeArguments);
            }
        }
    }
}
