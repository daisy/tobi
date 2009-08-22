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
            if (ViewModel.Metadatas.Count > 0)
            {
                MetadataGrid.SelectedItem = ViewModel.Metadatas[ViewModel.Metadatas.Count - 1];
                MetadataGrid.ScrollIntoView(MetadataGrid.SelectedItem);
                //TODO: put the user in "edit" mode so they see a combo box
            }
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
            {
                ViewModel.SelectedMetadata.Name = (string)e.AddedItems[0];
                ViewModel.SelectedMetadata.Validate();
            }
        }

       
        private void MetadataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {   
            if (e.Row != null && e.Row.Item != null)
            {
                NotifyingMetadataItem item = (NotifyingMetadataItem) e.Row.Item;
                item.Validate();
            }
        }

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateMetadata();
        }

        private void Whats_In_The_View_Model_Button_Click(object sender, RoutedEventArgs e)
        {
            string metas = ViewModel.GetViewModelDebugStringForMetaData();
            MessageBox.Show(metas);
        }

        private void Whats_In_The_Data_Model_Button_Click(object sender, RoutedEventArgs e)
        {
            string metas = ViewModel.GetDataModelDebugStringForMetaData();
            MessageBox.Show(metas);
        }
    }
    
}