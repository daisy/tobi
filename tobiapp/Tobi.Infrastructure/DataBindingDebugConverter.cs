using System;
using System.Diagnostics;
using System.Windows.Data;

namespace Tobi.Infrastructure
{
    public class DataBindingDebugConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }

        #endregion
    }
}
