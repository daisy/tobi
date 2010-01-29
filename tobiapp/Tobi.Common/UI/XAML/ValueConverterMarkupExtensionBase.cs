using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Tobi.Common.UI.XAML
{
    public abstract class ValueConverterMarkupExtensionBase<T> :
        MarkupExtension,
        IValueConverter,
        IMultiValueConverter
        where T : class, new()
    {
        public ValueConverterMarkupExtensionBase()
        {
        }

        public static T SingletonInstance
        {
            get
            {
                return NestedSingletonInstanceProvider.SingletonInstance;
            }
        }

        class NestedSingletonInstanceProvider
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static NestedSingletonInstanceProvider()
            {
            }

            internal static readonly T SingletonInstance = new T();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return SingletonInstance;
        }

        #region IValueConverter Members

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMultiValueConverter Members

        public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
