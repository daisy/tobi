using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Tobi.Common.UI.XAML;
using Tobi.Plugin.Validator.Metadata;
using System.Collections.ObjectModel;
using Tobi.Common.Validation;

namespace Tobi.Plugin.MetadataPane
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : ValueConverterMarkupExtensionBase<StringToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value is string)
                       ? Visibility.Visible
                       : string.IsNullOrEmpty((string) value) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class DescriptiveErrorTextConverter : ValueConverterMarkupExtensionBase<DescriptiveErrorTextConverter>
    {
        //Expected: NotifyingMetadataItem and list of ValidationItems
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] == null || values[1] == null) return "";
            if (!(values[0] is NotifyingMetadataItem) || !(values[1] is IEnumerable<ValidationItem>)) return "";

            NotifyingMetadataItem metadata = (NotifyingMetadataItem)values[0];
            IEnumerable<ValidationItem> errors = (IEnumerable<ValidationItem>)values[1];

            return GetErrorText(metadata, errors);
        }

        public static string GetErrorText(NotifyingMetadataItem metadata, IEnumerable<ValidationItem> errors)
        {
            //find the error for this metadata object
            ValidationItem error =
                errors.Where(v =>
                    (v is AbstractMetadataValidationErrorWithTarget)
                    &&
                    (v as AbstractMetadataValidationErrorWithTarget).Target ==
                    metadata.UrakawaMetadata).FirstOrDefault();

            if (error == null)
            {
                return "";
            }
            return error.Message;
        }
    }

    //[ValueConversion(typeof(string), typeof(string))]
    //public class LowerCaseConverter : ValueConverterMarkupExtensionBase<LowerCaseConverter>
    //{
    //    public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        return !(value is string) ? null : ((string) value).ToLower();
    //    }

    //    //this isn't converting back .. it's just making it lower case again
    //    public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        return !(value is string) ? null : ((string) value).ToLower();
        
    //    }
    //}


    [ValueConversion(typeof(object), typeof(IEnumerable))]
    public class AvailableMetadataNamesConverter : ValueConverterMarkupExtensionBase<AvailableMetadataNamesConverter>
    {
        //append values[1] to the list in values[0]
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return null;
            if (values[0] == null || values[1] == null) return null;
            if (!(values[0] is ObservableCollection<string>) || !(values[1] is string))
                return null;

            var list = (ObservableCollection<string>)values[0];
            string newItem = (string) values[1];

            bool found = list.Any(s => s.Equals(newItem, StringComparison.Ordinal)); //OrdinalIgnoreCase
            if (!found)
            {
                list.Insert(0, newItem);
            }

            return list.OrderBy(s => s);
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    /*public class FullDescriptionConverter : ValueConverterMarkupExtensionBase<FullDescriptionConverter>
    {
        //concatenate values[0] and values[1]
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return null;
            if (values[0] == null || values[1] == null) return null;
            if (!(values[0] is string) || !(values[1] is string))return null;

            return string.Format("{0}: {1}", (string) values[0], (string) values[1]);
        }
    }*/
    
    [ValueConversion(typeof(NotifyingMetadataItem), typeof(System.Windows.Visibility))]
    public class PrimaryIdentifierConverter : ValueConverterMarkupExtensionBase<PrimaryIdentifierConverter>
    {
        //return Visible if the item is a candidate for being the primary identifier
        //else return Hidden
        //value parameters: the metadata object, and the MetadataCollection
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return System.Windows.Visibility.Collapsed;
            if (!(value is NotifyingMetadataItem)) return Visibility.Collapsed;

            NotifyingMetadataItem item = (NotifyingMetadataItem)value;
            MetadataCollection metadatas = item.ParentCollection;

            if (item.IsPrimaryIdentifier || metadatas.IsCandidateForPrimaryIdentifier(item))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }
    }


    [ValueConversion(typeof(NotifyingMetadataItem), typeof(bool))]
    public class ValidityConverter : ValueConverterMarkupExtensionBase<ValidityConverter>
    {
        //return true if valid
        //Expected: NotifyingMetadataItem and list of ValidationItems
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] == null || values[1] == null) return true;
            if (!(values[0] is NotifyingMetadataItem) || !(values[1] is IEnumerable<ValidationItem>)) 
                return true;

            NotifyingMetadataItem metadataItem = (NotifyingMetadataItem)values[0];
            IEnumerable<ValidationItem> validationItems = (IEnumerable<ValidationItem>) values[1];
            
            foreach (ValidationItem item in validationItems)
            {
                if (item is AbstractMetadataValidationErrorWithTarget)
                {
                    if ((item as AbstractMetadataValidationErrorWithTarget).Target == metadataItem.UrakawaMetadata)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    [ValueConversion(typeof(NotifyingMetadataItem), typeof(string))]
    public class FullSummaryConverter : ValueConverterMarkupExtensionBase<FullSummaryConverter>
    {
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] == null || values[1] == null) return "";
            if (!(values[0] is NotifyingMetadataItem) || !(values[1] is IEnumerable<ValidationItem> )) return "";

            NotifyingMetadataItem item = values[0] as NotifyingMetadataItem;
            IEnumerable<ValidationItem> errors = values[1] as IEnumerable<ValidationItem>;

            string error = DescriptiveErrorTextConverter.GetErrorText(item, errors);
            if (string.IsNullOrEmpty(error)) error = Tobi_Plugin_MetadataPane_Lang.NoErrors;
            else error = string.Format(Tobi_Plugin_MetadataPane_Lang.ErrorItem, error);

            string primaryId = "";
            if (item.IsPrimaryIdentifier) primaryId = Tobi_Plugin_MetadataPane_Lang.IsPrimaryIdentifier;

            string canEditDelete = Tobi_Plugin_MetadataPane_Lang.Delete_Tooltip2;
            if (item.CanEditOrDelete) canEditDelete = Tobi_Plugin_MetadataPane_Lang.Delete_Tooltip;
            
            string synonyms = "";
            if (item.Definition.Synonyms != null && item.Definition.Synonyms.Count > 0)
            {
                synonyms = string.Format(Tobi_Plugin_MetadataPane_Lang.Synonyms, 
                    string.Join(",", item.Definition.Synonyms.ToArray()));
            }

            //name = content.  errors. primary id. can delete.  definition. synonyms.  occurrence.
            string retval = string.Format(Tobi_Plugin_MetadataPane_Lang.CompleteSummary, 
                item.Name, item.Content, error, primaryId, canEditDelete, item.Definition.Description, synonyms, 
                MetadataUtilities.OccurrenceToString(item.Definition));
            return retval;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    
}
