using System;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Markup;

namespace Tobi.Common._UnusedCode
{
    public class DataBindingDebugConverter : MarkupExtension, IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
#if DEBUG
            Debugger.Break();
#endif
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
#if DEBUG
            Debugger.Break();
#endif
            return value;
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}