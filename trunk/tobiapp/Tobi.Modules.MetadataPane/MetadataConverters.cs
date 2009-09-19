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
                List<NotifyingMetadataItem> results = metadatas.ToList().FindAll(s => s.Name == item.Name);
                if (results.Count > 1)
                    return true;
                else
                    return false;
            }
            else return true;
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
    public class ErrorsToListConverter : IValueConverter
    {
        //don't include errors about read-only metadata items
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //ObservableCollection<MetadataValidationError> errors = new ObservableCollection<MetadataValidationError>();
            ObservableCollection<string> errors = new ObservableCollection<string>();
            ObservableCollection<MetadataValidationError> sourceList =
                (ObservableCollection<MetadataValidationError>)value;
            DescriptiveErrorTextConverter descriptiveConverter = new DescriptiveErrorTextConverter();

            foreach (MetadataValidationError error in sourceList)
            {
                if (error.Definition.IsReadOnly == false)
                    errors.Add((string)descriptiveConverter.Convert(error, null, null, null));
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
        private static string NoErrors = "None";
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return NoErrors;
            if (!(value is MetadataValidationError)) return NoErrors;

            MetadataValidationError error = (MetadataValidationError)value;
            string description = null;
            if (error is MetadataValidationFormatError)
            {
                description = string.Format("{0} must be {1}.",
                    error.Definition.Name.ToLower(),
                    ((MetadataValidationFormatError)error).Hint);
            }
            else if (error is MetadataValidationMissingItemError)
            {
                description = string.Format("Missing {0}", error.Definition.Name.ToLower());
            }
            else if (error is MetadataValidationDuplicateItemError)
            {
                description = string.Format("Duplicate of {0} not allowed.", error.Definition.Name.ToLower());
            }
            else
            {
                description = string.Format("Unspecified error in {0}.", error.Definition.Name.ToLower());
            }
            return description;
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
            else
                return System.Windows.Visibility.Hidden;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("The ConvertBack method is not implemented because this Converter should only be used in a one-way Binding.");
        }
    }
    
}
