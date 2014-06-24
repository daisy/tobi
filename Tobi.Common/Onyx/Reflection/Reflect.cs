//-----------------------------------------------------------------------------
// <copyright file="Reflect.cs" company="William E. Kempf">
//     Copyright (c) William E. Kempf.
// </copyright>
//-----------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Tobi.Common.Onyx.Reflection
{
    /// <summary>
    /// Performs static reflection.
    /// </summary>
    public static class Reflect
    {
        /// <summary>
        /// Gets a <see cref="MemberInfo"/> using static reflection.
        /// </summary>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="MemberInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        public static MemberInfo GetMember(Expression<Action> expression)
        {
            return GetMemberInfo(expression as LambdaExpression);
        }

        /// <summary>
        /// Gets a <see cref="MemberInfo"/> using static reflection.
        /// </summary>
        /// <typeparam name="T">The type of the member accessed in the <paramref name="expression"/>.</typeparam>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="MemberInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        public static MemberInfo GetMember<T>(Expression<Func<T>> expression)
        {
            return GetMemberInfo(expression as LambdaExpression);
        }

        /// <summary>
        /// Gets a <see cref="MethodInfo"/> using static reflection.
        /// </summary>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="MethodInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        public static MethodInfo GetMethod(Expression<Action> expression)
        {
            MethodInfo method = GetMember(expression) as MethodInfo;
            return method;
        }

        /// <summary>
        /// Gets a <see cref="PropertyInfo"/> using static reflection.
        /// </summary>
        /// <typeparam name="T">The type of the member accessed in the <paramref name="expression"/>.</typeparam>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="PropertyInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        public static PropertyInfo GetProperty<T>(Expression<Func<T>> expression)
        {
            PropertyInfo property = GetMember(expression) as PropertyInfo;
            return property;
        }

        /// <summary>
        /// Gets a <see cref="FieldInfo"/> using static reflection.
        /// </summary>
        /// <typeparam name="T">The type of the member accessed in the <paramref name="expression"/>.</typeparam>
        /// <param name="expression">An expression that uses member access.</param>
        /// <returns>
        /// A <see cref="FieldInfo"/> instance for the member accessed in the <paramref name="expression"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static FieldInfo GetField<T>(Expression<Func<T>> expression)
        {
            FieldInfo field = GetMember(expression) as FieldInfo;
            return field;
        }

        internal static MemberInfo GetMemberInfo(LambdaExpression lambda)
        {
            if (lambda == null)
            {
                throw new ArgumentNullException(
                    GetMember(() => lambda).Name);
            }

            MemberExpression memberExpression = null;
            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpression = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = lambda.Body as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.Call)
            {
                return ((MethodCallExpression)lambda.Body).Method;
            }

            return memberExpression.Member;
        }
    }
}