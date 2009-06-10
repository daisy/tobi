using System;
using System.Windows.Controls.Primitives;
using Microsoft.Practices.Composite.Logging;
using System.Windows;
using System.Windows.Controls;
using Tobi.Modules.MetadataPane;
using urakawa.metadata;
using System.Collections.Generic;

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

        private void Lookup_Button_Click(object sender, RoutedEventArgs e)
        {
            //TODO: report "not found"
            if (LookupField.Text == "")
                return;
            
            foreach (object obj in list.Items)
            {
                NotifyingMetadataItem metadata = (NotifyingMetadataItem) obj;
                if (metadata.Name.ToLower().Contains(LookupField.Text.ToLower()))
                {
                    list.SelectedItems.Add(obj);
                }
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

        private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }

       
    }

    //not in use right now
    public class ValueConverter : System.Windows.Data.IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string val = "abc";
            //throw new NotImplementedException();
            return val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}