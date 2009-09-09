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
using Tobi.Common.MVVM;

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
            ObservableCollection<NotifyingMetadataItem> metadataItems =
                ViewModel.MetadataCollection.Metadatas;
            if (metadataItems.Count > 0)
            {
                NotifyingMetadataItem metadata = metadataItems[metadataItems.Count - 1];
                CollectionViewSource cvs = (CollectionViewSource) this.FindResource("MetadatasCVS");
                cvs.View.MoveCurrentTo(metadata);
            }
        }
        
        private void Remove_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
            NotifyingMetadataItem metadata = (NotifyingMetadataItem)cvs.View.CurrentItem;
            ViewModel.RemoveMetadata(metadata);
        }

        
        private void Validate_Metadata_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateMetadata();
            
        }

     
        //select the corresponding metadata from the item double-clicked in the errors list
        private void errorsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (errorsList.SelectedItem != null && 
                errorsList.SelectedItem is MetadataValidationFormatError)
            {
                MetadataValidationFormatError error = (MetadataValidationFormatError) errorsList.SelectedItem;
                NotifyingMetadataItem metadataItem = ViewModel.MetadataCollection.Find(error.Metadata);
                CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
                if (metadataItem != null) cvs.View.MoveCurrentTo(metadataItem);
            }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                string metaname = (string)e.AddedItems[0];
                CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
                if (cvs.View.CurrentItem != null)
                    ((NotifyingMetadataItem)cvs.View.CurrentItem).Name = metaname;
            }
        }

        private void MetadataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectionChanged();
        }
    }
    
}