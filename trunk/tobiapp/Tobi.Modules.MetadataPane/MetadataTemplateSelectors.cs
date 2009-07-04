using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using urakawa.metadata.daisy;


namespace Tobi.Modules.MetadataPane
{
    public class ContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }

        private MetadataPaneView m_View;
        public ContentTemplateSelector(MetadataPaneView view)
        {
            if (view == null) return;
            m_View = view;
            DefaultTemplate = (DataTemplate)m_View.list.Resources["ContentTemplate"];
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return DefaultTemplate;
        }
    }

    public class NameTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OptionalTemplate { get; set; }
        public DataTemplate RecommendedTemplate { get; set; }
        public DataTemplate RequiredTemplate { get; set; }

        private MetadataPaneView m_View;
        public NameTemplateSelector(MetadataPaneView view)
        {
            if (view == null) return;

            m_View = view;
            OptionalTemplate = (DataTemplate)m_View.list.Resources["OptionalName"];
            RecommendedTemplate = (DataTemplate)m_View.list.Resources["OptionalName"];
            RequiredTemplate = (DataTemplate)m_View.list.Resources["RequiredName"];
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {

            NotifyingMetadataItem metadata = (NotifyingMetadataItem)item;
            int index = SupportedMetadata_Z39862005.MetadataList.FindIndex(0, s => s.Name == metadata.Name);

            if (index != -1)
            {
                MetadataDefinition definition = SupportedMetadata_Z39862005.MetadataList[index];

                if (definition.Occurrence == MetadataOccurrence.Required)
                    return RequiredTemplate;
                else if (definition.Occurrence == MetadataOccurrence.Recommended)
                    return RecommendedTemplate;
            }
            //if the occurrence is optional, or if we don't recognize this metadata
            return OptionalTemplate;
        }
    }

}
