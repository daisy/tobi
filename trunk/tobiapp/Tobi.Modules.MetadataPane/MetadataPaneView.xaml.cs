using System;
using System.Threading;
using System.Windows.Controls.Primitives;
using Microsoft.Practices.Composite.Logging;
using System.Windows;
using System.Windows.Controls;
using Tobi.Modules.MetadataPane;
using urakawa.metadata;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections.ObjectModel;
using urakawa.metadata.daisy;
using Microsoft.Windows.Controls;
namespace Tobi.Modules.MetadataPane
{
    /// <summary>
    /// Interaction logic for MetadataPaneView.xaml
    /// The backing ViewModel is injected in the constructor ("passive" view design pattern)
    /// </summary>
    public partial class MetadataPaneView : IMetadataPaneView
    {
        #region Construction

        public MetadataPaneViewModel ViewModel { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public MetadataPaneView(MetadataPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.SetView(this);

            ViewModel.Logger.Log("MetadataPaneView.ctor", Category.Debug, Priority.Medium);

            DataContext = ViewModel;

            InitializeComponent();
        }

        #endregion Construction


        private void Add_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddEmptyMetadata();
        }
        
        private void Remove_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            NotifyingMetadataItem selected = ViewModel.SelectedMetadata;
            ViewModel.RemoveMetadata(selected);
        }

        private void Validate_Metadata_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateMetadata();
        }

        
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is NotifyingMetadataItem)
                    ViewModel.SelectedMetadata = (NotifyingMetadataItem) e.AddedItems[0];
            }
            else
            {
                ViewModel.SelectedMetadata = null;
            }
           
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                ViewModel.SelectedMetadata.Name = (string)e.AddedItems[0];
        }

       
        private void MetadataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {   
            if (e.Row != null && e.Row.Item != null)
            {
                NotifyingMetadataItem item = (NotifyingMetadataItem) e.Row.Item;
                item.Validate();
            }
        }
    }
    public class OccurrenceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MetadataOccurrence occurrence = (MetadataOccurrence) value;
            if (occurrence == MetadataOccurrence.Required) return Visibility.Hidden;
            else return Visibility.Visible;
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
                (ObservableCollection<MetadataValidationError>) value;
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
            MetadataValidationError error = (MetadataValidationError) value;
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
}