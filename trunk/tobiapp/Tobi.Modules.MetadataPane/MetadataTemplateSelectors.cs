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
        public DataTemplate DatePickerTemplate { get; set; }
        
        public ContentTemplateSelector()
        {   
        }

        public override DataTemplate SelectTemplate(object obj, DependencyObject container)
        {
            return DefaultTemplate;

            NotifyingMetadataItem item = (NotifyingMetadataItem) obj;
            if (item.Definition.DataType == MetadataDataType.Date)
                return DatePickerTemplate;
            else
                return DefaultTemplate;
        }
    }

}
