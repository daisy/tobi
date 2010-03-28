using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
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
            var obj = sender as Hyperlink;
            ((ValidationItem)obj.DataContext).TakeAction();
        }
    }

    [ValueConversion(typeof(MetadataDataType), typeof(string))]
    public class DataTypeToStringConverter : ValueConverterMarkupExtensionBase<DataTypeToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is MetadataDataType)) return "";
            return MetadataValidationError.DataTypeToString((MetadataDataType)value);
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
            return MetadataValidationError.OccurrenceToString(value as MetadataDefinition);
        }
    }

    
    [ValueConversion(typeof(bool), typeof(string))]
    public class IsRepeatableToStringConverter : ValueConverterMarkupExtensionBase<IsRepeatableToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isRepeatable = (bool)value;
            return MetadataValidationError.RepeatableToString(isRepeatable);
        }
    }

}
