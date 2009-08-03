using System;
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

        //fake data model interaction to test notification bindings
        private void Fake_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CreateFakeData();
        }
        private void Data_Model_Report_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(ViewModel.GetDebugStringForMetaData());
        }
        private void Lookup_Button_Click(object sender, RoutedEventArgs e)
        {
            /*if (LookupField.Text == "")
            {
                ViewModel.StatusText = "Not found";
                return;
            }

            int count = 0;
            list.SelectedItems.Clear();
            foreach (object obj in list.Items)
            {
                NotifyingMetadataItem metadata = (NotifyingMetadataItem) obj;
                if (metadata.Name.ToLower().Contains(LookupField.Text.ToLower()))
                {
                    count++;
                    list.SelectedItems.Add(obj);
                }
            }
            if (count == 0)
            {
                ViewModel.StatusText = "Not found";
            }
            else if (count == 1)
            {
                ViewModel.StatusText = "1 match found";
            }
            else
            {
                ViewModel.StatusText = string.Format("{0} matches found", count.ToString());
            }*/
        }
        private void Remove_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {/*
            while (list.SelectedItems.Count > 0)
            {   
                NotifyingMetadataItem metadata = (NotifyingMetadataItem)list.SelectedItem;
                ViewModel.RemoveMetadata(metadata);
            }
          * */
        }

        private void Validate_Metadata_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateMetadata();
        }

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
            //ViewModel.RefreshDataTemplateSelectors();
        }


        public object SelectedMetadata
        {
            get
            {
                return (object)GetValue(SelectedMetadataProperty);
            }
            set
            {
                SetValue(SelectedMetadataProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedMetadataProperty =
        DependencyProperty.Register("SelectedMetadata", typeof(object), typeof(MetadataPaneView),
                new UIPropertyMetadata(null));

        public string SelectedMetadataDescription
        {
            get
            {
                return (string)GetValue(SelectedMetadataDescriptionProperty);
            }
            set
            {
                SetValue(SelectedMetadataDescriptionProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedMetadataDescriptionProperty =
        DependencyProperty.Register("SelectedMetadataDescription", typeof(string), typeof(MetadataPaneView),
                new UIPropertyMetadata(null));

        /// <summary>
        /// based on the existing metadata, return a list of metadata fields available
        /// for addition
        /// </summary>
        public ObservableCollection<string> AvailableMetadata
        {
            get
            {
                ObservableCollection<string> availableMetadata = ViewModel.GetAvailableMetadata();

                //the available metadata list might not have our selection in it
                //if the selection is meant not to be duplicated
                //we need users to be able to have the current Name as an option
                if (SelectedMetadata != null)
                {
                    NotifyingMetadataItem selection = (NotifyingMetadataItem)SelectedMetadata;
                    if (selection.Name != "")
                    {
                        if (availableMetadata.Contains(selection.Name) == false)
                            availableMetadata.Insert(0, selection.Name);
                    }
                }
                return availableMetadata;
            }
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ((ComboBox)sender).ItemsSource = AvailableMetadata;
        }



    }
}