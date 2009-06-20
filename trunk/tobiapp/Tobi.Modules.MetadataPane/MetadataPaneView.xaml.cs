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
            //TODO: should we clear the selection first?
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
                ViewModel.StatusText = "Not found";
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
            ViewModel.RefreshDataTemplateSelectors();
        
            
            
            //TODO: force a source update (as in the LostFocus event) here for
            //the corresponding text box.  but how to find it? 
            //idea from MS not working; rowPresenter is always null
            //http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/fbd03126-ab8e-45fa-8b3b-f2baae35af87/
            GridViewRowPresenter rowPresenter = ((ComboBox) sender).Tag as GridViewRowPresenter;
            if (rowPresenter != null)
            {
                TextBox textBox = GetFrameworkElementByName<TextBox>(rowPresenter, "textBox");
                if (textBox != null)
                {
                    BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);
                    if (be != null) be.UpdateSource();
                }
            }
        }

        private void Validate_Metadata_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateMetadata();
        }

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshDataTemplateSelectors();
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            BindingExpression be = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
            if (be != null) be.UpdateSource();
        }

        //from 
        //http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/fbd03126-ab8e-45fa-8b3b-f2baae35af87/
        public T GetFrameworkElementByName<T>(FrameworkElement referenceElement, String name) where T : FrameworkElement
        {
            FrameworkElement child = null;
            for (Int32 i = 0; i < VisualTreeHelper.GetChildrenCount(referenceElement); i++)
            {
                child = VisualTreeHelper.GetChild(referenceElement, i) as FrameworkElement;
                if (child != null && child.Name == name && child.GetType() == typeof(T))
                {
                    break;
                }

                else if (child != null)
                {
                    child = GetFrameworkElementByName<T>(child, name);
                }
            }
            return child as T;
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
    
    /*public class ContentTriggerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            object[] values= new object[2];
            values[0] = value;
            return values;
        }

    }*/

    
        
}