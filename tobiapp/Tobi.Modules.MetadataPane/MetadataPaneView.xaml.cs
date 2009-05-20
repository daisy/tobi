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
        }

        #endregion Construction
    }
}

//if this class is in the Tobi.Modules.MetadataPane namespace, the XAML doesn't "see" it
//there must be an easy fix, but for today ... 
namespace Frustration
{
    public class MetadataTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OptionalStringTemplate { get; set; }
        public DataTemplate OptionalDateTemplate { get; set; }
        public DataTemplate RequiredStringTemplate { get; set; }
        public DataTemplate RequiredDateTemplate { get; set; }
        public DataTemplate ReadOnlyTemplate { get; set;}
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            System.Diagnostics.Debug.Assert(item is Metadata);

            Metadata metadata = (Metadata)item;
            
            //TODO: move this to a more sensible place where it only gets called once per Tobi instance
            //it contains all the metadata supported by Tobi
            List<Tobi.Modules.MetadataPane.SupportedMetadataItem> list = new List<SupportedMetadataItem>();
            Tobi.Modules.MetadataPane.CreateSupportedMetadataList createSupportedMetadataList = 
                new Tobi.Modules.MetadataPane.CreateSupportedMetadataList(list);

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
                    if (metaitem.IsRequired)
                        return RequiredDateTemplate;
                    else
                        return OptionalDateTemplate;
                }

                else if (metaitem.FieldType == SupportedMetadataFieldType.ShortString || 
                    metaitem.FieldType == SupportedMetadataFieldType.LongString)
                {
                    if (metaitem.IsRequired)
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
}