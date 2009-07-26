//-----------------------------------------------------------------------------
// <copyright file="ReflectOn.cs" company="William E. Kempf">
//     Copyright (c) William E. Kempf.
// </copyright>
//-----------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Tobi.Common.Onyx.Reflection
{
    /// <summary>
    /// Performs static reflection on a specified type.
    /// </summary>
    public static class ReflectOn<T>
    {
        /// <summary>
        /// Gets a <see cref="MemberInfo"/> using static reflection.
        /// </summary>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="MemberInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static MemberInfo GetMember(Expression<Action<T>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(Reflect.GetMember(() => expression).Name);
            }

            return Reflect.GetMemberInfo(expression as LambdaExpression);
        }

        /// <summary>
        /// Gets a <see cref="MemberInfo"/> using static reflection.
        /// </summary>
        /// <typeparam name="TResult">The type of the member accessed in the <paramref name="expression"/>.</typeparam>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="MemberInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static MemberInfo GetMember<TResult>(Expression<Func<T, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(Reflect.GetMember(() => expression).Name);
            }

            return Reflect.GetMemberInfo(expression as LambdaExpression);
        }

        /// <summary>
        /// Gets a <see cref="MethodInfo"/> using static reflection.
        /// </summary>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="MethodInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static MethodInfo GetMethod(Expression<Action<T>> expression)
        {
            MethodInfo method = GetMember(expression) as MethodInfo;
            if (method == null)
            {
                throw new ArgumentException(
                    "Not a method call expression",
                    Reflect.GetMember(() => expression).Name);
            }

            return method;
        }

        /// <summary>
        /// Gets a <see cref="PropertyInfo"/> using static reflection.
        /// </summary>
        /// <typeparam name="TResult">The type of the member accessed in the <paramref name="expression"/>.</typeparam>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="PropertyInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        public static PropertyInfo GetProperty<TResult>(Expression<Func<T, TResult>> expression)
        {
            PropertyInfo property = GetMember(expression) as PropertyInfo;
            if (property == null)
            {
                throw new ArgumentException(
                    "Not a property expression", Reflect.GetMember(() => expression).Name);
            }

            return property;
        }

        /// <summary>
        /// Gets a <see cref="FieldInfo"/> using static reflection.
        /// </summary>
        /// <typeparam name="TResult">The type of the member accessed in the <paramref name="expression"/>.</typeparam>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="FieldInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static FieldInfo GetField<TResult>(Expression<Func<T, TResult>> expression)
        {
            FieldInfo field = GetMember(expression) as FieldInfo;
            if (field == null)
            {
                throw new ArgumentException(
                    "Not a field expression", Reflect.GetMember(() => expression).Name);
            }

            return field;
        }
    }
}