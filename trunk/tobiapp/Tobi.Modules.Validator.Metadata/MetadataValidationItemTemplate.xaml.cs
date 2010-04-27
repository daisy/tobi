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

        private void OnEditLinkClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as Hyperlink;
            ((ValidationItem)obj.DataContext).TakeAction();
        }

        private void OnAddLinkClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as Hyperlink;
            ValidationItem err = (ValidationItem)obj.DataContext;
            err.TakeAction();
        }
    }

    [ValueConversion(typeof(MetadataDataType), typeof(string))]
    public class DataTypeToStringConverter : ValueConverterMarkupExtensionBase<DataTypeToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is MetadataDataType)) return "";
            return MetadataUtilities.DataTypeToString((MetadataDataType)value);
        }
    }

    [ValueConversion(typeof(MetadataDefinition), typeof(string))]
    public class OccurrenceDescriptionConverter : ValueConverterMarkupExtensionBase<OccurrenceDescriptionConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            return MetadataUtilities.OccurrenceToString(value as MetadataDefinition);
        }
    }

    
    [ValueConversion(typeof(bool), typeof(string))]
    public class IsRepeatableToStringConverter : ValueConverterMarkupExtensionBase<IsRepeatableToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isRepeatable = (bool)value;
            return MetadataUtilities.RepeatableToString(isRepeatable);
        }
    }

    [ValueConversion(typeof(MetadataDefinition), typeof(string))]
    public class DefinitionSynonymsListConverter : ValueConverterMarkupExtensionBase<DefinitionSynonymsListConverter>
    {
        //return a comma-delimited string of metadata synonyms
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is MetadataDefinition)) return "";

            MetadataDefinition definition = value as MetadataDefinition;

            if (definition.Synonyms != null && definition.Synonyms.Count > 0)
            {
                return string.Join(",", definition.Synonyms.ToArray());
            }
            return "";
        }
    }

    [ValueConversion(typeof(MetadataDefinition), typeof(Visibility))]
    public class DefinitionSynonymsListVisibilityConverter : ValueConverterMarkupExtensionBase<DefinitionSynonymsListVisibilityConverter>
    {
        //return visible if this definition has synonyms
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Visibility.Hidden;
            if (!(value is MetadataDefinition)) return Visibility.Hidden;

            MetadataDefinition definition = value as MetadataDefinition;

            return definition.Synonyms != null && definition.Synonyms.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
