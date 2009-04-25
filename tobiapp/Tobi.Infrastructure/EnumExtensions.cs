using System;
using System.Linq;
using System.Collections.Generic;

namespace Tobi.Infrastructure
{
    /// <summary>
    /// Contains extension method on the <see cref="System.Type"/> allowing to represent
    /// an enumeration members as a sequence.
    /// 
    /// Type enumType = typeof(UriPartial);
    /// lbxValues.ItemsSource = enumType.Values();
    /// lbxNames.ItemsSource = enumType.Names();
    /// lbxDictionary.ItemsSource = enumType.Dictionary();
    /// 
    /// Other pure-XAML solution:
    /// 
    /// <ObjectDataProvider MethodName=”GetValues”
    ///             ObjectType=”{x:Type sys:Enum}”
    ///             x:Key=”DayOfWeekValues”>
    ///     <ObjectDataProvider.MethodParameters>
    ///         <x:Type TypeName=”sys:DayOfWeek” />
    ///     </ObjectDataProvider.MethodParameters>
    /// </ObjectDataProvider>
    /// 
    /// <ComboBox x:Name=”cboDayOfWeek”
    ///             ItemsSource=”{Binding Source={StaticResource DayOfWeekValues}}” />
    /// 
    /// public enum DrinkType
    /// {
    /// [Description("le Cafe")] Coffee = 0,
    /// [Description("le The")] Tea,
    /// [Description("Burbon")] Burbon
    /// };
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Get values of the specified enumeration type.
        /// </summary>
        /// <param name="type">Extendee type.</param>
        /// <returns>The sequence of values of the enumeration specified.</returns>
        public static IEnumerable<object> Values(this Type type)
        {
            Type enumType = Nullable.GetUnderlyingType(type) ?? type;
            if (!enumType.IsEnum)
                yield break;

            foreach (object item in Enum.GetValues(enumType))
                yield return Convert.ChangeType(item, Enum.GetUnderlyingType(enumType));
        }

        /// <summary>
        /// Get names of the specified enumeration type.
        /// </summary>
        /// <param name="type">Extendee type.</param>
        /// <returns>The sequence of names of the enumeration specified.</returns>
        public static IEnumerable<string> Names(this Type type)
        {
            Type enumType = Nullable.GetUnderlyingType(type) ?? type;
            if (!enumType.IsEnum)
                yield break;

            foreach (string item in Enum.GetNames(enumType))
                yield return item;
        }

        /// <summary>
        /// Get name/value pairs of the specified enumeration type.
        /// </summary>
        /// <param name="type">Extendee type.</param>
        /// <returns>The sequence of name/value pairs of the enumeration specified.</returns>
        public static IEnumerable<KeyValuePair<string, object>> Dictionary(this Type type)
        {
            Type enumType = Nullable.GetUnderlyingType(type) ?? type;
            if (!enumType.IsEnum)
                yield break;

            IEnumerable<KeyValuePair<string, object>> dictionary =
                from value in Enum.GetValues(enumType).Cast<object>()
                select new KeyValuePair<string, object>(Enum.GetName(enumType, value)
                    , Convert.ChangeType(value, Enum.GetUnderlyingType(enumType)));

            foreach (KeyValuePair<string, object> item in dictionary)
                yield return item;
        }
    }
}
