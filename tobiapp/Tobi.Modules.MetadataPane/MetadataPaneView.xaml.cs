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

            list.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnHeaderResize), true);
        }

        #endregion Construction

        private double m_CheckBoxColumnWidth = -1.0;

        private void OnHeaderResize(object sender, DragDeltaEventArgs e)
        {
            var thumb = e.OriginalSource as Thumb;
            if (thumb == null) return;
            
            var header = thumb.TemplatedParent as GridViewColumnHeader;
            if (header == null) return;
            
            var view = list.View as GridView;
            if (view == null) return;

            // If user tries to resize checkbox column, reset the width to fixed
            if (view.Columns[0] == header.Column)
            {
                if (m_CheckBoxColumnWidth == -1.0)
                {
                    m_CheckBoxColumnWidth = header.Column.ActualWidth;
                }
                header.Column.Width = m_CheckBoxColumnWidth;
                e.Handled = true;
            }
        }

        private void AllSelectionChanged(object sender, RoutedEventArgs e)
        {
            var chkBox = sender as CheckBox;
            if (chkBox != null)
            {
                bool check = chkBox.IsChecked.Value;

                if (check)
                {
                    list.SelectAll();
                }
                else
                {
                    list.UnselectAll();
                }
            }
        }

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
            string data = "";
            foreach (Metadata m in ViewModel.Project.GetPresentation(0).ListOfMetadata)
            {
                data += string.Format("{0} = {1}\n", m.Name, m.Content);
            }
            MessageBox.Show(data);
        }
        private void Lookup_Button_Click(object sender, RoutedEventArgs e)
        {
            if (LookupField.Text == "")
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
            }
        }
        private void Remove_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            while (list.SelectedItems.Count > 0)
            {   
                NotifyingMetadataItem metadata = (NotifyingMetadataItem)list.SelectedItem;
                ViewModel.RemoveMetadata(metadata);
            }
        }

        
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //after the data templates get refreshed, this function gets triggered again
            if (e.AddedItems.Count == 0) return;
            NotifyingMetadataItem metadata = (NotifyingMetadataItem) list.SelectedItem;
            string name = (string) e.AddedItems[0];
            if (metadata != null) metadata.Name = name;
            //revalidate
            //ViewModel.RefreshDataTemplateSelectors();
            
        }

        private void Validate_Metadata_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateMetadata();
        }

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshDataTemplateSelectors();
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedMetadata != list.SelectedItem)
            {
                SelectedMetadata = list.SelectedItem;
                ObservableCollection<string> l = AvailableMetadata;
            
            }
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
                    NotifyingMetadataItem selection = (NotifyingMetadataItem) SelectedMetadata;
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
            ((ComboBox) sender).ItemsSource = AvailableMetadata;
        }

    }

    public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            bool param = bool.Parse(parameter as string);
            bool val = (bool)value;

            return val == param ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
        
}