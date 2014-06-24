using System;
using System.Diagnostics;
using Tobi.Common.UI.XAML;

namespace Tobi.Common._UnusedCode
{
    public class DataBindingDebugConverter : ValueConverterMarkupExtensionBase<DataBindingDebugConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
#if DEBUG
            Debugger.Break();
#endif
            return value;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
#if DEBUG
            Debugger.Break();
#endif
            return value;
        }

        #endregion
    }
}