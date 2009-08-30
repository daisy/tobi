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

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
//            ViewModel.ValidateMetadata();
        }

        public ObservableCollection<string> AvailableMetadata
        {
            get
            {
                ObservableCollection<string> list = ViewModel.AvailableMetadata;

                //the available metadata list might not have our selection in it
                //if the selection is meant not to be duplicated
                //we need users to be able to have the current Name as an option
                CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
                if (cvs.View.CurrentItem != null)
                {
                    NotifyingMetadataItem selection = (NotifyingMetadataItem)cvs.View.CurrentItem;
                    if (selection.Name != "")
                    {
                        if (list.Contains(selection.Name) == false)
                            list.Insert(0, selection.Name);
                    }
                }
                return list;
            }
        }
    }
    
}