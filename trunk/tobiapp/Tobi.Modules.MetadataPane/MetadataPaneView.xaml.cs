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

        //fake data model interaction to test notification bindings
        private void Fake_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CreateFakeData();
        }
        private void Data_Model_Report_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(ViewModel.GetDebugStringForMetaData());
        }
        
        private bool findNext()
        {
            /*
            DataGridCellInfo selectedCell = null;
            bool startingFromSelection = false;
            if (MetadataGrid.SelectedCells.Count > 0)
            {
                selectedCell = MetadataGrid.SelectedCells[0];
                startingFromSelection = true;
            }
            MetadataGrid.SelectedCells.Clear();

            if (selection != null)
            {
                while (enumerator.Current != selection)
                {
                    if (!enumerator.MoveNext())
                    {
                        //go to the beginning of the collection
                        enumerator.Reset();
                        enumerator.MoveNext();
                        startingFromSelection = false;
                        break;
                    }
                }
            }
            else
            {
                //go to the beginning of the collection
                enumerator.Reset();
                enumerator.MoveNext();
                startingFromSelection = false;
            }
            if (startingFromSelection)
            {
                if (!enumerator.MoveNext())
                {
                    enumerator.Reset();
                    enumerator.MoveNext();
                }
            }

            bool found = false;
            do
            {
                if (enumerator.Current.Name.ToLower().Contains(LookupField.Text.ToLower()))
                {
                    found = true;
                    ViewModel.SelectedMetadata = enumerator.Current;
                    break;
                }
            } while (enumerator.MoveNext());

            //restore the selection
            if (!found) ViewModel.SelectedMetadata = selection;
            return found;
            */
            return false;
        }
        private bool findPrevious()
        {
            return false;
        }
        private void Lookup_Button_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (LookupField.Text == "")
            {
                MessageBox.Show(string.Format("Please enter all or part of a metadata field name."));
                return;
            }
            
            if (findNext() == false)
            {
                MessageBox.Show(string.Format("{0} not found", LookupField.Text));
            }
             * */
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

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
            
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

        private void DockPanel_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            /*
            //search for the next occurrence of the lookup text
            if (e.Key == System.Windows.Input.Key.F3)
            {
                if (LookupField.Text == "")
                {
                    MessageBox.Show(string.Format("Please enter all or part of a metadata field name."));
                    return;
                }
                if (findNext() == false)
                    MessageBox.Show("No more occurrences found");
            }*/
        }



    }
}