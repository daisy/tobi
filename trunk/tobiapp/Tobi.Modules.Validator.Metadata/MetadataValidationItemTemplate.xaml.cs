using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using Tobi.Common.UI.XAML;
using Tobi.Common.Validation;
using urakawa.metadata;


namespace Tobi.Plugin.Validator.Metadata
{
    [Export(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MetadataValidationItemTemplate : ResourceDictionary
    {
        public MetadataValidationItemTemplate()
        {
            InitializeComponent();
        }

        private void OnLinkClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This should open the metadata editor");
        }
    }

    [ValueConversion(typeof(MetadataDataType), typeof(string))]
    public class DataTypeToStringConverter : ValueConverterMarkupExtensionBase<DataTypeToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is MetadataDataType)) return "";

            MetadataDataType item = (MetadataDataType)value;
            if (item == MetadataDataType.String)
                return "string";
            if (item == MetadataDataType.ClockValue)
                return "timestamp";
            if (item == MetadataDataType.Double)
                return "double (e.g. 10.56)";
            if (item == MetadataDataType.Date)
                return "date";
            if (item == MetadataDataType.FileUri)
                return "path to a file";
            if (item == MetadataDataType.Integer)
                return "integer";
            if (item == MetadataDataType.LanguageCode)
                return "language code";
            if (item == MetadataDataType.Number)
                return "number";

            return "";
        }
    }

    //TODO: this has been copy-pasted from the MetadataPane.  How to share?  Maybe keep it here and remove it from 
    //the pane (the pane is aware of the validator, but not vice versa)
    [ValueConversion(typeof(MetadataDefinition), typeof(string))]
    public class OccurrenceDescriptionConverter : ValueConverterMarkupExtensionBase<OccurrenceDescriptionConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            MetadataDefinition item = (MetadataDefinition)value;
            if (item.Occurrence == MetadataOccurrence.Required)
                return "Required.  ";
            if (item.Occurrence == MetadataOccurrence.Recommended)
                return "Recommended. ";
            return "Optional. ";
        }
    }

    
    [ValueConversion(typeof(bool), typeof(string))]
    public class IsRepeatableToStringConverter : ValueConverterMarkupExtensionBase<IsRepeatableToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isRepeatable = (bool)value;
            return isRepeatable ? "May be repeated" : "May not be repeated";
        }
    }

}
