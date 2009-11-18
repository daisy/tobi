using Microsoft.Practices.Composite.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Tobi.Modules.Validator.Metadata;

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

            ViewModel.Logger.Log("MetadataPaneView.ctor", Category.Debug, Priority.Medium);

            DataContext = ViewModel;

            InitializeComponent();
        }

        #endregion Construction

        private void Add_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddEmptyMetadata();
            ObservableCollection<NotifyingMetadataItem> metadataItems =
                ViewModel.MetadataCollection.Metadatas;
            if (metadataItems.Count > 0)
            {
                NotifyingMetadataItem metadata = metadataItems[metadataItems.Count - 1];
                CollectionViewSource cvs = (CollectionViewSource) this.FindResource("MetadatasCVS");
                cvs.View.MoveCurrentTo(metadata);
            }
            namesComboBox.Focus();
        }
        
        private void Remove_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
            NotifyingMetadataItem metadata = (NotifyingMetadataItem)cvs.View.CurrentItem;
            ViewModel.RemoveMetadata(metadata);
        }

        //select the corresponding metadata from the item double-clicked in the errors list
        private void errorsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (errorsList.SelectedItem != null && 
                errorsList.SelectedItem is MetadataValidationError)
            {
                MetadataValidationError error = (MetadataValidationError) errorsList.SelectedItem;
                if (error.ErrorType == MetadataErrorType.FormatError)
                {
                    NotifyingMetadataItem metadataItem = ViewModel.MetadataCollection.Find(error.Target);
                    CollectionViewSource cvs = (CollectionViewSource) this.FindResource("MetadatasCVS");
                    if (metadataItem != null) cvs.View.MoveCurrentTo(metadataItem);
                }
            }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
                NotifyingMetadataItem metadata = (NotifyingMetadataItem)cvs.View.CurrentItem;

                //checkbox visibility
                PrimaryIdentifierConverter primaryIdentifierConverter = new PrimaryIdentifierConverter();
                primaryIdentifierCheckBox.Visibility = 
                    (System.Windows.Visibility)primaryIdentifierConverter.Convert(metadata, null, null, null);
                
                //the remove button's enabled-ness
                IsNotRequiredOccurrenceConverter requiredOccurrenceConverter = new IsNotRequiredOccurrenceConverter();
                removeButton.IsEnabled = (bool)requiredOccurrenceConverter.Convert(metadata, null, null, null);
            
            }
        }

        private void MetadataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectionChanged();
        }
    }
    
}