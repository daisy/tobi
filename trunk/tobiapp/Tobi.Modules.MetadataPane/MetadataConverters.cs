using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Tobi.Common.UI.XAML;
using Tobi.Plugin.Validator.Metadata;
using System.Collections.ObjectModel;
using Tobi.Common.Validation;

namespace Tobi.Plugin.MetadataPane
{
    [ValueConversion(typeof(object), typeof(string))]
    public class DescriptiveErrorTextConverter : ValueConverterMarkupExtensionBase<DescriptiveErrorTextConverter>
    {
        //Expected: NotifyingMetadataItem and list of ValidationItems
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (! (values[0] is NotifyingMetadataItem))
            {
                return null;
            }

            NotifyingMetadataItem metadata = (NotifyingMetadataItem)values[0];
            IEnumerable<ValidationItem> errors = (IEnumerable<ValidationItem>)values[1];

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

    [ValueConversion(typeof(string), typeof(string))]
    public class LowerCaseConverter : ValueConverterMarkupExtensionBase<LowerCaseConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value is string) ? null : ((string) value).ToLower();
        }

        //this isn't converting back .. it's just making it lower case again
        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value is string) ? null : ((string) value).ToLower();
        
        }
    }


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

            ObservableCollection<string> list = (ObservableCollection<string>)values[0];
            string newItem = (string) values[1];

            bool found = list.Any(s => s.ToLower() == newItem.ToLower());
            if (!found) list.Insert(0, newItem.ToLower());

            return list.OrderBy(s => s.ToLower());
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class FullDescriptionConverter : ValueConverterMarkupExtensionBase<FullDescriptionConverter>
    {
        //concatenate values[0] and values[1]
        public override object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return null;
            if (values[0] == null || values[1] == null) return null;
            if (!(values[0] is string) || !(values[1] is string))return null;

            return string.Format("{0}: {1}", (string) values[0], (string) values[1]);
        }
    }
    
    [ValueConversion(typeof(NotifyingMetadataItem), typeof(System.Windows.Visibility))]
    public class PrimaryIdentifierConverter : ValueConverterMarkupExtensionBase<PrimaryIdentifierConverter>
    {
        //return Visible if the item is a candidate for being the primary identifier
        //else return Hidden
        //value parameters: the metadata object, and the MetadataCollection
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return System.Windows.Visibility.Hidden;
            if (!(value is NotifyingMetadataItem)) return System.Windows.Visibility.Hidden;

            NotifyingMetadataItem item = (NotifyingMetadataItem)value;
            MetadataCollection metadatas = item.ParentCollection;

            if (item.IsPrimaryIdentifier || metadatas.IsCandidateForPrimaryIdentifier(item))
                return System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Hidden;
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

    
}
