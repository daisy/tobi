using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using Tobi.Common.UI.XAML;

namespace Tobi.Common._UnusedCode
{
    [ValueConversion(typeof(String), typeof(FlowDocument))]
    public class StringXamlConverter : ValueConverterMarkupExtensionBase<StringXamlConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var flowDocument = new FlowDocument();
                var xaml = value.ToString();

                using (var stream = new MemoryStream((new ASCIIEncoding()).GetBytes(xaml)))
                {
                    var text = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                    text.Load(stream, DataFormats.Xaml);
                }

                return flowDocument;
            }
            return null;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var flowDocument = (FlowDocument)value;
                var range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                string xaml;

                using (var stream = new MemoryStream())
                {
                    range.Save(stream, TextDataFormat.Xaml.ToString());
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream))
                    {
                        xaml = reader.ReadToEnd();
                    }
                }

                return xaml;
            }
            return null;
        }
    }
}