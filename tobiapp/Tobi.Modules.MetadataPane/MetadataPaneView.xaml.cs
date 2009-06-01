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
            if (thumb == null)
            {
                return;
            }

            var header = thumb.TemplatedParent as GridViewColumnHeader;
            if (header == null)
            {
                return;
            }

            var view = list.View as GridView;
            if (view == null)
            {
                return;
            }

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

        private void Add_Metadata_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("Dear user:  sorry!");
        }

        private void Fake_Button_Click(object sender, RoutedEventArgs e)
        {
            //mess with the data in the data model and test that the changes were reflected
        }
    }
}

//if this class is in the Tobi.Modules.MetadataPane namespace, the XAML doesn't "see" it
//there must be an easy fix, but for today ... 
namespace Frustration
{
    public class MetadataContentTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OptionalStringTemplate { get; set; }
        public DataTemplate OptionalDateTemplate { get; set; }
        public DataTemplate RequiredStringTemplate { get; set; }
        public DataTemplate RequiredDateTemplate { get; set; }
        public DataTemplate ReadOnlyTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Metadata metadata = (Metadata)item;

            List<Tobi.Modules.MetadataPane.SupportedMetadataItem> list =
                Tobi.Modules.MetadataPane.SupportedMetadataList.MetadataList;
            int index = list.FindIndex(0, s => s.Name == metadata.Name);
            if (index != -1)
            {
                Tobi.Modules.MetadataPane.SupportedMetadataItem metaitem = list[index];
                //TODO: this assumes that when a field is readonly, we will just display it as a default (short) string
                //this is probably an ok assumption for now, but we'll want to change it later.
                if (metaitem.IsReadOnly)
                    return ReadOnlyTemplate;

                if (metaitem.FieldType == SupportedMetadataFieldType.Date)
                {
                    if (metaitem.Occurence == MetadataOccurence.Required)
                        return RequiredDateTemplate;
                    else
                        return OptionalDateTemplate;
                }

                else if (metaitem.FieldType == SupportedMetadataFieldType.ShortString ||
                    metaitem.FieldType == SupportedMetadataFieldType.LongString)
                {
                    if (metaitem.Occurence == MetadataOccurence.Required)
                        return RequiredStringTemplate;
                    else
                        return OptionalStringTemplate;
                }

                else
                {
                    return DefaultTemplate;
                }
            }
            else
            {
                return DefaultTemplate;
            }
        }
    }

    public class MetadataNameTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OptionalTemplate { get; set; }
        public DataTemplate RecommendedTemplate { get; set; }
        public DataTemplate RequiredTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Metadata metadata = (Metadata)item;

            List<Tobi.Modules.MetadataPane.SupportedMetadataItem> list =
                Tobi.Modules.MetadataPane.SupportedMetadataList.MetadataList;
            int index = list.FindIndex(0, s => s.Name == metadata.Name);
            if (index != -1)
            {
                Tobi.Modules.MetadataPane.SupportedMetadataItem metaitem = list[index];

                if (metaitem.Occurence == MetadataOccurence.Required)
                    return RequiredTemplate;
                else if (metaitem.Occurence == MetadataOccurence.Recommended)
                    return RecommendedTemplate;
            }
            return OptionalTemplate;
        }
    }
}