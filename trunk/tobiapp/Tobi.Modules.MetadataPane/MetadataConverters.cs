using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Tobi.Plugin.Validator.Metadata;
using urakawa.metadata;
using System.Collections.ObjectModel;
using Tobi.Common.Validation;

namespace Tobi.Plugin.MetadataPane
{
    //all classes here represent value converters used by XAML

    public class IsNotRequiredOccurrenceConverter : IValueConverter
    {
        //return false if required
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return false;
            if (!(value is NotifyingMetadataItem))return false;

            NotifyingMetadataItem item = (NotifyingMetadataItem)value;
            ObservableCollection<NotifyingMetadataItem> metadatas = item.ParentCollection.Metadatas;

            if (item.Definition != null && item.Definition.Occurrence == MetadataOccurrence.Required)
            {
                //check for duplicates.  a required item can be removed if it is not the only one.
                List<NotifyingMetadataItem> results = metadatas.ToList().FindAll
                    (s => s.Name.ToLower() == item.Name.ToLower());
                if (results.Count > 1)
                    return true;
                return false;
            }
            return true;
        }
        
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    
    public class OccurrenceDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            MetadataDefinition item = (MetadataDefinition)value;
            if (item.Occurrence == MetadataOccurrence.Required)
                return "Required.  ";
            if (item.Occurrence == MetadataOccurrence.Recommended)
                return "Recommended. ";
            return "Optional. ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }

    //public class ErrorsToListConverter : IValueConverter
    //{
    //    //don't include errors about read-only metadata items
    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        //ObservableCollection<MetadataValidationError> errors = new ObservableCollection<MetadataValidationError>();
    //        ObservableCollection<string> errors = new ObservableCollection<string>();
    //        IEnumerable<ValidationItem> sourceList =
    //            (IEnumerable<ValidationItem>)value;
            
    //        foreach (ValidationItem error in sourceList)
    //        {
    //            if (!((MetadataValidationError)error).Definition.IsReadOnly)
    //                errors.Add(error.Message);
    //        }

    //        return errors;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException
    //            ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
    //    }
    //}

    public class DescriptiveErrorTextConverter : IMultiValueConverter
    {
        private const string NoErrors = "None";
        //Expected: NotifyingMetadataItem and list of ValidationItems
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (! (values[0] is NotifyingMetadataItem))
            {
                return null;
            }

            NotifyingMetadataItem metadata = (NotifyingMetadataItem)values[0];
            IEnumerable<ValidationItem> errors = (IEnumerable<ValidationItem>)values[1];

            //find the error for this metadata object
            MetadataValidationError error = 
                errors.Where(v => ((MetadataValidationError) v).Target == 
                    metadata.UrakawaMetadata).Cast<MetadataValidationError>().FirstOrDefault();
            
            if (error == null)
            {
                return NoErrors;
            }
            return error.Message;
        }
        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    
    public class LowerCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value is string) ? null : ((string) value).ToLower();
        }

        //this isn't converting back .. it's just making it lower case again
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value is string) ? null : ((string) value).ToLower();
        
        }
    }

    public class AvailableMetadataNamesConverter : IMultiValueConverter
    {
        //append values[1] to the list in values[0]
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }

    public class FullDescriptionConverter : IMultiValueConverter
    {
        //concatenate values[0] and values[1]
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return null;
            if (values[0] == null || values[1] == null) return null;
            if (!(values[0] is string) || !(values[1] is string))return null;

            return string.Format("{0}: {1}", (string) values[0], (string) values[1]);
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }

    public class PrimaryIdentifierConverter : IValueConverter
    {
        //return Visible if the item is a candidate for being the primary identifier
        //else return Hidden
        //value parameters: the metadata object, and the MetadataCollection
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return System.Windows.Visibility.Hidden;
            if (!(value is NotifyingMetadataItem)) return System.Windows.Visibility.Hidden;

            NotifyingMetadataItem item = (NotifyingMetadataItem)value;
            MetadataCollection metadatas = item.ParentCollection;

            if (item.IsPrimaryIdentifier || metadatas.IsCandidateForPrimaryIdentifier(item))
                return System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Hidden;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    
}
