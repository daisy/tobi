using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using urakawa.metadata.daisy;
using System.Collections.ObjectModel;

namespace Tobi.Modules.MetadataPane
{
    //all classes here represent value converters used by XAML

    public class IsNotRequiredOccurrenceConverter : IMultiValueConverter
    {
        //return false if required
        //value parameters: the metadata object, and the collection of all metadata objects
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return false;
            if (values[0] == null || values[1] == null) return false;
            if (!(values[0] is NotifyingMetadataItem) || !(values[1] is MetadataCollection))
                return false;

            NotifyingMetadataItem item = (NotifyingMetadataItem)values[0];
            MetadataCollection metadataCollection = (MetadataCollection) values[1];

            if (item.Definition != null && item.Definition.Occurrence == MetadataOccurrence.Required)
            {
                //check for duplicates.  a required item can be removed if it is not the only one.
                List<NotifyingMetadataItem> results = 
                    metadataCollection.Metadatas.ToList().FindAll(s => s.Name == item.Name);
                if (results.Count > 1)
                    return true;
                else
                    return false;
            }
            else return true;
        }
        
        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
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
            else if (item.Occurrence == MetadataOccurrence.Recommended)
                return "Recommended. ";
            else
                return "Optional. ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    public class RemoveReadOnlyErrorsConverter : IValueConverter
    {
        //don't include errors about read-only metadata items
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<MetadataValidationError> errors = new ObservableCollection<MetadataValidationError>();
            ObservableCollection<MetadataValidationError> sourceList =
                (ObservableCollection<MetadataValidationError>)value;
            foreach (MetadataValidationError error in sourceList)
            {
                if (error.Definition.IsReadOnly == false)
                    errors.Add(error);
            }

            return errors;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    public class DescriptiveErrorTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            MetadataValidationError error = (MetadataValidationError)value;
            string description = null;
            if (error is MetadataValidationFormatError)
            {
                description = string.Format("{0} must be {1}.",
                    error.Definition.Name,
                    ((MetadataValidationFormatError)error).Hint);
            }
            else if (error is MetadataValidationMissingItemError)
            {
                description = string.Format("Missing {0}", error.Definition.Name);
            }
            else if (error is MetadataValidationDuplicateItemError)
            {
                description = string.Format("Duplicate of {0} not allowed.", error.Definition.Name);
            }
            else
            {
                description = string.Format("Unspecified error in {0}.", error.Definition.Name);
            }
            return description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    public class ValidationStatusTextConverter : IValueConverter
    {
        private static string NoErrorsFound = "All metadata is valid.";
        private static string ErrorsFound = "Please correct the following errors:";
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || (int)value == 0)
                return NoErrorsFound;
            else
                return ErrorsFound;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }

    public class LowerCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is string)) return null;
            return ((string) value).ToLower();
        }

        //this isn't converting back .. it's just making it lower case again
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is string)) return null;
            return ((string)value).ToLower();
            /*
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
             * */
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

            bool found = false;
            foreach (string s in list)
            {
                if (s.ToLower() == newItem.ToLower())
                {
                    found = true;
                    break;
                }
            }
            if (!found) list.Insert(0, newItem.ToLower());

            return list;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    
}
